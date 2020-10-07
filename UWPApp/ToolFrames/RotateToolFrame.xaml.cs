using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using UWPApp.FileIO;
using ToolBoxPDF.Core.PageRangePackage;
using ToolBoxPDF.Core.IO;

namespace UWPApp.ToolFrames
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RotateToolFrame : Page
    {
        private static readonly string PAGE_TITLE = "Rotate pages";

        private ToolPage rootPage;

        /// <summary>
        /// Contains one unprotected file
        /// </summary>
        private ObservableCollection<InternalFile> loadedFileList;
        private InternalFile loadedFile;
        private PdfDocument renderDocument;
        private uint renderedPageNumber = 0;

        /// <summary>
        /// Saves the rotation transformation of each page. 
        /// Pagenumber are accessed by index. Please note that the n-th page refers to the index n-1, since page numbers start at 1 and indices at 0
        /// </summary>
        private List<PageOrientation> rotations;

        public RotateToolFrame()
        {
            this.InitializeComponent();
            loadedFileList = new ObservableCollection<InternalFile>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            rootPage = ToolPage.Current;
            rootPage.SetPageTitle(PAGE_TITLE);
        }

        private async void FileAddButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker fileOpenPicker = new FileOpenPicker();

            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(App.RECENT_FILE_DIRECTORY_TOKEN))
            {
                fileOpenPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            }
            fileOpenPicker.FileTypeFilter.Add(".pdf");
            StorageFile selectedFile = await fileOpenPicker.PickSingleFileAsync(); ;

            if (selectedFile != null)
            {
                StorageFolder parentfolder = await selectedFile.GetParentAsync();
                if (parentfolder != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(App.RECENT_FILE_DIRECTORY_TOKEN, parentfolder);
                }

                InternalFile internalFile = await InternalFile.LoadInternalFileAsync(selectedFile);
                if (internalFile.Decrypted)
                {
                    loadedFile = internalFile;
                    TotalPageNumber.Text = loadedFile.PageCount.ToString();
                    SetDefaultOrientation();

                    loadedFileList = new ObservableCollection<InternalFile>();
                    loadedFileList.Add(loadedFile);
                    loadedFilesView.ItemsSource = loadedFileList;

                    // load the file separately for rendering
                    try
                    {
                        renderDocument = await PdfDocument.LoadFromFileAsync(loadedFile.File);
                        renderedPageNumber = 1;
                        SetCurrentPageLabel();
                        await RenderPageAsync(renderedPageNumber);
                    }
                    catch (Exception)
                    {
                        rootPage.NotifyUser("Document is not a valid PDF.", NotifyType.ErrorMessage);
                    }
                }
                else
                {
                    ToolPage.Current.NotifyUser("The selected file cannot be loaded since it is password-protected.", NotifyType.ErrorMessage);
                }
            }

            if (loadedFileList.Count > 0)
            {
                RotationPanel.Visibility = Visibility.Visible;
            }
            else
            {
                RotationPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF-Document", new List<String>() { ".pdf" });
            savePicker.SuggestedFileName = loadedFileList[0].FileName.Replace(".pdf", "-rotated.pdf");
            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(App.RECENT_FILE_DIRECTORY_TOKEN))
            {
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            }
            StorageFile savedFile = await savePicker.PickSaveFileAsync();
            RotateAndSaveAsync(rotations, savedFile);
        }

        /// <summary>
        /// Rotates the pages and saves the result as a new document to the given StorageFile
        /// </summary>
        private async void RotateAndSaveAsync(List<PageOrientation> orientations, StorageFile outputFile)
        {
            if (outputFile != null)
            {
                Task<Stream> outputStreamTask = outputFile.OpenStreamForWriteAsync();
                Stream outputStream = await outputStreamTask;
                if (outputStream != null)
                {
                    PdfAssembler pdfAssembler = new PdfAssembler(outputStream);
                    for (int i = 0; i < loadedFile.PageCount; i++)
                    {
                        int pageNumber = i + 1;
                        ExportTask task = new ExportTask(PageRange.FromPattern(loadedFile.Document, pageNumber.ToString()), new PageRotation(orientations[i]));
                        pdfAssembler.AppendTask(task);
                    }
                    pdfAssembler.ExportFile();
                }
                else
                {
                    ToolPage.Current.NotifyUser("Error occured while while exporting the merged file. Try again.", NotifyType.ErrorMessage);
                }
            }
            else
            {
                ToolPage.Current.NotifyUser("No output file has been selected.", NotifyType.ErrorMessage);
            }
        }

        private async Task RenderPageAsync(uint pageNumber)
        {
            // Convert from 1-based page number to 0-based page index.
            uint pageIndex = pageNumber - 1;

            using (PdfPage page = renderDocument.GetPage(pageIndex))
            {
                var options1 = new PdfPageRenderOptions();
                options1.BackgroundColor = Windows.UI.Colors.Transparent;
                uint actualWidth = (uint)(PreviewArea.ActualWidth - PreviewBorder.Margin.Left - PreviewBorder.Margin.Right - 4);
                ScaledRectangle displayedPageDimension;

                if (rotations[(int)pageIndex].DegAngle % 180 == 0)
                {
                    // Raw page orientation matches displayed page orientation
                    displayedPageDimension = new ScaledRectangle(page.Size.Height, page.Size.Width);
                }
                else
                {
                    // Raw page orientation does not match the displayed page orientation
                    displayedPageDimension = new ScaledRectangle(page.Size.Width, page.Size.Height);
                }

                uint stretchedHeight = (uint)displayedPageDimension.GetScaledHeight(actualWidth);
                var maxHeight = Preview.MaxHeight;
                if (stretchedHeight > maxHeight)
                {
                    options1.DestinationHeight = (uint)(maxHeight);
                    options1.DestinationWidth = (uint)(displayedPageDimension.GetScaledWidth(maxHeight));
                }
                else
                {
                    options1.DestinationHeight = stretchedHeight;
                    options1.DestinationWidth = actualWidth;
                }

                // update decent border around the previewed page
                PreviewBorder.Height = options1.DestinationHeight;
                PreviewBorder.Width = options1.DestinationWidth;

                var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream, options1);

                BitmapImage src = new BitmapImage();
                await src.SetSourceAsync(stream);
                Preview.Source = src;

                RotateTransform rotationTransform = new RotateTransform()
                {
                    Angle = rotations[(int)pageIndex].DegAngle,
                    CenterX = (PreviewBorder.Width-4) / 2,
                    CenterY = (PreviewBorder.Height-4) / 2
                };

                ScaleTransform scaleTransform;
                if (rotations[(int)pageIndex].DegAngle % 180 == 0)
                {
                    scaleTransform = new ScaleTransform()
                    {
                        ScaleX = 1,
                        ScaleY = 1,
                    };
                }
                else
                {
                    scaleTransform = new ScaleTransform()
                    {
                        CenterX = (PreviewBorder.Width - 4) / 2,
                        CenterY = (PreviewBorder.Height - 4) / 2,
                        ScaleX = displayedPageDimension.AspectRatio,
                        ScaleY = 1 / displayedPageDimension.AspectRatio,
                    };
                }

                TransformGroup renderTransform = new TransformGroup();
                renderTransform.Children.Add(rotationTransform);
                renderTransform.Children.Add(scaleTransform);

                Preview.RenderTransform = renderTransform;                
            }
        }

        private async void Border_SizeChangedAsync(object sender, SizeChangedEventArgs e)
        {
            if (renderDocument != null)
            {
                await RenderPageAsync(renderedPageNumber);
            }
        }

        private async void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (renderedPageNumber > 1)
            {
                renderedPageNumber--;
                await RenderPageAsync(renderedPageNumber);
                // update tick icon and pagenumber
                SetCurrentPageLabel();
            }

        }

        private async void NextPage_Click(object sender, RoutedEventArgs e)
        {
            if (renderedPageNumber < loadedFile.PageCount)
            {
                renderedPageNumber++;
                await RenderPageAsync(renderedPageNumber);
                // update tick icon and pagenumber
                SetCurrentPageLabel();
            }
        }

        private void SetCurrentPageLabel()
        {
            ToolPage.Current.NotifyUser(String.Empty, NotifyType.StatusMessage);
            CurrentPageNumber.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            CurrentPageNumber.Text = renderedPageNumber.ToString();
        }

        private async void CurrentPageNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox inputBox = sender as TextBox;
            uint pageNumber;
            if (!uint.TryParse(inputBox.Text, out pageNumber) || (pageNumber < 1) || (pageNumber > loadedFile.PageCount))
            {
                inputBox.Foreground = new SolidColorBrush(Windows.UI.Colors.Red); ;
            }
            else
            {
                inputBox.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
                renderedPageNumber = pageNumber;
                await RenderPageAsync(renderedPageNumber);
            }
        }

        private class ScaledRectangle
        {
            public double Height { get; }
            public double Width { get; }

            /// <summary>
            /// Aspect Ratio of the rectangle: Width / Height
            /// </summary>
            public double AspectRatio { get; }

            public ScaledRectangle(double height, double width)
            {
                Height = height;
                Width = width;
                AspectRatio = width / height;
            }

            public double GetScaledWidth(double height)
            {
                return height / Height * Width;
            }

            public double GetScaledHeight(double width)
            {
                return width / Width * Height;
            }
        }

        private async void RotateButton_Click(object sender, RoutedEventArgs e)
        {
            int pageIndex = (int)renderedPageNumber - 1;
            rotations[pageIndex]++;
            await RenderPageAsync(renderedPageNumber);
        }

        /// <summary>
        /// Sets the default page orientation for all pages of the loaded file.
        /// @requires: loadedFile != null (file must be loaded)
        /// </summary>
        private void SetDefaultOrientation()
        {
            rotations = new List<PageOrientation>();
            for (int i = 0; i < loadedFile.PageCount; i++)
            {
                rotations.Add(PageOrientation.NoRotation());
            }
        }
    }
}

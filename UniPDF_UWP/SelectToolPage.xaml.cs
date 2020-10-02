using PdfManipulator.PdfIOUtilities;
using PdfManipulator.PageRangePackage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using UniPDF_UWP.FileManagement;
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
using Windows.UI.Xaml.Controls.Primitives;

namespace UniPDF_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectToolPage : Page
    {
        private static readonly string PAGE_TITLE = "Extract selected pages";

        private ToolPage rootPage;

        /// <summary>
        /// Contains one unprotected file
        /// </summary>
        private ObservableCollection<InternalFile> loadedFileList;
        private InternalFile loadedFile;
        private PdfDocument renderDocument;
        private uint renderedPageNumber = 0;

        /// <summary>
        /// PageRange instance for the loaded file: represents the ticked files
        /// </summary>
        private PageRange selectedPageRange;

        public SelectToolPage()
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
                    selectedPageRange = new PageRange(loadedFile.Document);

                    loadedFileList = new ObservableCollection<InternalFile>();
                    loadedFileList.Add(loadedFile);
                    loadedFilesView.ItemsSource = loadedFileList;
                    PageRangeInput.Text = String.Empty;

                    // load the file separately for rendering
                    try
                    {
                        renderDocument = await PdfDocument.LoadFromFileAsync(loadedFile.File);
                        renderedPageNumber = 1;                        
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
                SelectPanel.Visibility = Visibility.Visible;
            }
            else
            {
                SelectPanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            PageRange outputPageRange;
            if (SelectionOptions.SelectedIndex == 0)
            {
                // extract option
                outputPageRange = selectedPageRange;
            }
            else
            {
                // remove option
                outputPageRange = selectedPageRange.Invert();
            }
            var savePicker = new FileSavePicker();
            savePicker.FileTypeChoices.Add("PDF-Document", new List<String>() { ".pdf" });
            savePicker.SuggestedFileName = loadedFileList[0].FileName.Replace(".pdf", "-extracted.pdf");
            if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(App.RECENT_FILE_DIRECTORY_TOKEN))
            {
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            }
            StorageFile savedFile = await savePicker.PickSaveFileAsync();
            ExtractAndSaveAsync(outputPageRange, savedFile);
        }

        /// <summary>
        /// Saves the pages from the PageRange instance as a new document to the given StorageFile
        /// </summary>
        private async void ExtractAndSaveAsync(PageRange outputPageRange, StorageFile outputFile)
        {
            if (outputFile != null)
            {
                Task<Stream> outputStreamTask = outputFile.OpenStreamForWriteAsync();
                Stream outputStream = await outputStreamTask;
                if (outputStream != null)
                {
                    PdfAssembler pdfAssembler = new PdfAssembler(outputStream);
                    ExportTask task = new ExportTask(outputPageRange);
                    pdfAssembler.AppendTask(task);
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

        private void SelectionOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ControlSaveButtonState();
        }

        private void PageRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                selectedPageRange = PageRange.FromPattern(loadedFileList[0].Document, PageRangeInput.Text);
                PageRangeInvalidLabel.Visibility = Visibility.Collapsed;
            }
            catch (Exception)
            {
                PageRangeInvalidLabel.Visibility = Visibility.Visible;
            }
            ControlSaveButtonState();
            SetToggleButtonState();
        }

        private void ControlSaveButtonState()
        {
            if (selectedPageRange != null)
            {
                if (SelectionOptions.SelectedIndex == 0)
                {
                    // extract option
                    if (selectedPageRange.IsEmpty())
                    {
                        SaveButton.IsEnabled = false;
                    }
                    else
                    {
                        SaveButton.IsEnabled = true;
                    }
                }
                else
                {
                    // remove option
                    if (selectedPageRange.IsEntire())
                    {
                        SaveButton.IsEnabled = false;
                    }
                    else
                    {
                        SaveButton.IsEnabled = true;
                    }
                }
            }
        }

        private async Task RenderPageAsync(uint pageNumber)
        {
            //uint pageNumber;
            //if (!uint.TryParse(PageNumberBox.Text, out pageNumber) || (pageNumber < 1) || (pageNumber > pdfDocument.PageCount))
            //{
            //    rootPage.NotifyUser("Invalid page number.", NotifyType.ErrorMessage);
            //    return;
            //}

            // Convert from 1-based page number to 0-based page index.
            uint pageIndex = pageNumber - 1;

            using (PdfPage page = renderDocument.GetPage(pageIndex))
            {
                ScaledRectangle pageDimension = new ScaledRectangle(page.Size.Height, page.Size.Width);

                uint actualWidth = (uint)(PreviewArea.ActualWidth - PreviewBorder.Margin.Left - PreviewBorder.Margin.Right - 4);
                uint strechedHeight = (uint)pageDimension.GetScaledHeight(actualWidth);

                var options1 = new PdfPageRenderOptions();

                if (strechedHeight > Preview.MaxHeight)
                {
                    options1.DestinationHeight = (uint)(Preview.MaxHeight);
                    options1.DestinationWidth = (uint)(pageDimension.GetScaledWidth(Preview.MaxHeight));
                    //ToolPage.Current.NotifyUser(page.Size.Height.ToString() + " " + page.Size.Width.ToString() + " " + Preview.MaxHeight.ToString() + " " + options1.DestinationWidth.ToString(), NotifyType.StatusMessage);
                }
                else
                {
                    options1.DestinationHeight = strechedHeight;
                    options1.DestinationWidth = actualWidth;
                    ToolPage.Current.NotifyUser(page.Size.Height.ToString() + " " + page.Size.Width.ToString() + " " + options1.DestinationHeight.ToString() + " " + options1.DestinationWidth, NotifyType.StatusMessage);
                }

                // update decent border around the previewed page
                PreviewBorder.Height = options1.DestinationHeight;
                PreviewBorder.Width = options1.DestinationWidth;


                var stream = new InMemoryRandomAccessStream();
                await page.RenderToStreamAsync(stream, options1);

                BitmapImage src = new BitmapImage();
                Preview.Source = src;

                await src.SetSourceAsync(stream);
            }
        }

        private class ScaledRectangle
        {
            public double Height { get; }
            public double Width { get; }

            public ScaledRectangle(double height, double width)
            {
                Height = height;
                Width = width;
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
                SetToggleButtonState();
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
                SetToggleButtonState();
                SetCurrentPageLabel();
            }
        }

        private void SetToggleButtonState()
        {
            if (selectedPageRange.Contains((int)renderedPageNumber))
            {
                CurrentPageSelectButton.IsChecked = true;
            }
            else
            {
                CurrentPageSelectButton.IsChecked = false;
            }
        }

        private void SetCurrentPageLabel()
        {
            CurrentPageNumber.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            CurrentPageNumber.Text =renderedPageNumber.ToString();
        }

        private void CurrentPageSelectButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;
            if (button.IsChecked ?? false)
            {
                selectedPageRange.Add((int) renderedPageNumber);
            }
            else
            {
                selectedPageRange.Remove((int)renderedPageNumber);
            }
            PageRangeInput.Text = selectedPageRange.ToString();
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
                SetToggleButtonState();
                await RenderPageAsync(renderedPageNumber);
            }
        }
    }
}

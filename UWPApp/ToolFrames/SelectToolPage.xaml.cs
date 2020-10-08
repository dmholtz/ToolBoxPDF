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
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls.Primitives;
using UWPApp.FileIO;
using ToolBoxPDF.Core.PageRangePackage;
using ToolBoxPDF.Core.IO;
using UWPApp.UIElementFrames;

namespace UWPApp.ToolFrames
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectToolPage : Page
    {
        private static readonly string PAGE_TITLE = "Extract selected pages";

        private ToolPage rootPage;

        private RenderedPagePreview renderedPagePreview;

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
                        SetCurrentPageLabel();
                        RenderedPagePreviewFrame.Navigate(typeof(RenderedPagePreview), renderDocument);
                        renderedPagePreview = RenderedPagePreview.Current;
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
            PageRange outputPageRange = GetOutputPageRange();

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
            if (selectedPageRange != null)
            {
                ControlSaveButtonState();
                ShowSummary();
            }
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
            ShowSummary();
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

        private async void Border_SizeChangedAsync(object sender, SizeChangedEventArgs e)
        {
            if (renderDocument != null)
            {
                await renderedPagePreview.RenderPageAsync(renderedPageNumber);
            }
        }

        private async void PreviousPage_Click(object sender, RoutedEventArgs e)
        {
            if (renderedPageNumber > 1)
            {
                renderedPageNumber--;
                await renderedPagePreview.RenderPageAsync(renderedPageNumber);
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
                await renderedPagePreview.RenderPageAsync(renderedPageNumber);
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
            ToolPage.Current.NotifyUser(String.Empty, NotifyType.StatusMessage);
            CurrentPageNumber.Foreground = new SolidColorBrush(Windows.UI.Colors.Black);
            CurrentPageNumber.Text = renderedPageNumber.ToString();
        }

        private void CurrentPageSelectButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleButton button = sender as ToggleButton;
            if (button.IsChecked ?? false)
            {
                selectedPageRange.Add((int)renderedPageNumber);
            }
            else
            {
                selectedPageRange.Remove((int)renderedPageNumber);
            }
            PageRangeInput.Text = selectedPageRange.ToString();
            ShowSummary();
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
                await renderedPagePreview.RenderPageAsync(renderedPageNumber);
            }
        }

        private PageRange GetOutputPageRange()
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
            return outputPageRange;
        }

        private void ShowSummary()
        {
            PageRange outputPageRange = GetOutputPageRange();
            int numberOfPages = outputPageRange.GetNumberOfPages();
            if (numberOfPages == 0)
            {
                SummaryText.Text = "No pages are extracted.";
            }
            else if (numberOfPages == 1)
            {
                SummaryText.Text = "1 page is extracted.";
            }
            else
            {
                SummaryText.Text = numberOfPages.ToString()+ " pages are extracted.";
            }
            ToolPage.Current.NotifyUser(String.Empty, NotifyType.StatusMessage);
        }
    }
}

using PdfManipulator.PdfIOUtilities;
using PdfManipulator.PageRangePackage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UniPDF_UWP.FileManagement;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

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
                    loadedFileList = new ObservableCollection<InternalFile>();
                    loadedFileList.Add(internalFile);
                    loadedFilesView.ItemsSource = loadedFileList;
                    PageRangeInput.Text = String.Empty;
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
            catch(Exception)
            {
                PageRangeInvalidLabel.Visibility = Visibility.Visible;
                selectedPageRange = null;
            }
            ControlSaveButtonState();           
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
    }
}

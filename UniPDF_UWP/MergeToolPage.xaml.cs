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
    public sealed partial class MergeToolPage : Page
    {
        private static readonly string PAGE_TITLE = "Merge PDF documents";

        private ToolPage rootPage;

        private ObservableCollection<InternalFile> loadedFilesList;

        public MergeToolPage()
        {
            this.InitializeComponent();
            loadedFilesList = new ObservableCollection<InternalFile>();
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
            IReadOnlyList<StorageFile> selectedFiles = await fileOpenPicker.PickMultipleFilesAsync();

            int encryptedFilesCount = 0;

            foreach (var storageFile in selectedFiles)
            {
                InternalFile internalFile = await InternalFile.LoadInternalFileAsync(storageFile);
                if (internalFile.Decrypted)
                {
                    loadedFilesList.Add(internalFile);
                }
                else
                {
                    encryptedFilesCount++;
                }
            }

            if (selectedFiles.Count > 0)
            {
                StorageFolder parentfolder = await selectedFiles[0].GetParentAsync();
                if (parentfolder != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(App.RECENT_FILE_DIRECTORY_TOKEN, parentfolder);
                }
            }

            if (encryptedFilesCount > 0)
            {
                ToolPage.Current.NotifyUser("Unable to load " + encryptedFilesCount + " encrypted file(s).", NotifyType.ErrorMessage);
            }
            else
            {
                ToolPage.Current.NotifyUser("Opened: " + loadedFilesList.Count.ToString(), NotifyType.StatusMessage);
            }


            loadedFilesView.ItemsSource = loadedFilesList;
            DisplaySummary();
        }

        public class InternalFileCollection
        {
            public ObservableCollection<InternalFile> InternFile { get; }

            public InternalFileCollection()
            {
                InternFile = new ObservableCollection<InternalFile>();
            }
        }

        private void loadedFilesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loadedFilesView.SelectedItems.Count > 0)
            {
                FileRemoveButton.Visibility = Visibility.Visible;
            }
            else
            {
                FileRemoveButton.Visibility = Visibility.Collapsed;
            }
            ToolPage.Current.NotifyUser(String.Empty, NotifyType.StatusMessage);
        }

        private void DisplaySummary()
        {
            int pageCount = 0;
            FileSize totalSize = FileSize.ZeroBytes();
            foreach (var obj in loadedFilesList)
            {
                var file = (InternalFile)obj;
                totalSize += file.Size;
                pageCount += file.PageCount;
            }
            if (pageCount == 1)
            {
                SummaryPages.Text = pageCount.ToString() + " page";
            }
            else
            {
                SummaryPages.Text = pageCount.ToString() + " pages";
            }
            if (loadedFilesList.Count == 0)
            {
                SummaryFiles.Text = "no files";
            }
            else if (loadedFilesList.Count == 1)
            {
                SummaryFiles.Text = "1 file";
            }
            else
            {
                SummaryFiles.Text = loadedFilesList.Count.ToString() + " file";
            }
            SummarySize.Text = totalSize.ToString();
        }

        private void FileRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Generate an immutable list of items to be removed
            IList<object> selectedItems = loadedFilesView.SelectedItems.ToImmutableList();
            // Deselect all items in the listview
            loadedFilesView.SelectedItem = null;
            foreach (var selectedItem in selectedItems)
            {
                InternalFile selectedFile = (InternalFile)selectedItem;
                loadedFilesList.Remove(selectedFile);
            }
            loadedFilesView.ItemsSource = loadedFilesList;
            DisplaySummary();
        }

        private void loadedFilesView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {

            StringBuilder s = new StringBuilder();
            foreach (var item in loadedFilesView.Items)
            {
                var file = (InternalFile)item;
                s.Append(file.FileName + " ");
            }

            ToolPage.Current.NotifyUser(s.ToString(), NotifyType.StatusMessage);
        }

        private async void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            if (loadedFilesList.Count > 0)
            {
                var savePicker = new FileSavePicker();
                savePicker.FileTypeChoices.Add("PDF-Document", new List<String>() { ".pdf" });
                savePicker.SuggestedFileName = "MergedPdfDocuments";
                if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(App.RECENT_FILE_DIRECTORY_TOKEN))
                {
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                }
                StorageFile savedFile = await savePicker.PickSaveFileAsync();

                if (savedFile != null)
                {
                    Task<Stream> outputStreamTask = savedFile.OpenStreamForWriteAsync();
                    Stream outputStream = await outputStreamTask;
                    if (outputStream != null)
                    {
                        PdfAssembler pdfAssembler = new PdfAssembler(outputStream);
                        foreach (var obj in loadedFilesList)
                        {
                            var file = (InternalFile)obj;
                            PageRange pageRange = PageRange.EntireDocument(file.Document);
                            ExportTask task = new ExportTask(pageRange);
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
            else
            {
                ToolPage.Current.NotifyUser("No files loaded.", NotifyType.ErrorMessage);
            }         
        }
    }
}

using ToolBoxPDF.Core.IO;
using ToolBoxPDF.Core.PageRangePackage;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UWPApp.FileIO;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UWPApp.ToolFrames
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EncryptToolPage : Page
    {
        private static readonly string PAGE_TITLE = "Encryption Tool: Protect PDF with password";

        private ToolPage rootPage;

        /// <summary>
        /// Contains only unprotected files which need to be decrypted
        /// </summary>
        private ObservableCollection<InternalFile> loadedFilesList;

        public EncryptToolPage()
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

            if (selectedFiles.Count > 0)
            {
                StorageFolder parentfolder = await selectedFiles[0].GetParentAsync();
                if (parentfolder != null)
                {
                    StorageApplicationPermissions.FutureAccessList.AddOrReplace(App.RECENT_FILE_DIRECTORY_TOKEN, parentfolder);
                }
            }

            List<InternalFile> protectedFiles = new List<InternalFile>();

            foreach (var storageFile in selectedFiles)
            {
                InternalFile internalFile = await InternalFile.LoadInternalFileAsync(storageFile);

                if (internalFile.Decrypted)
                {
                    loadedFilesList.Add(internalFile);
                }
                else
                {
                    protectedFiles.Add(internalFile);
                }
            }

            if (protectedFiles.Count > 0)
            {
                StringBuilder notification = new StringBuilder("The following are ignored because they are already password-protected:\n");
                foreach (var file in protectedFiles)
                {
                    notification.Append(file.FileName + "\n");
                }
                ToolPage.Current.NotifyUser(notification.ToString(), NotifyType.ErrorMessage);
            }
            loadedFilesView.ItemsSource = loadedFilesList;
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
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (OutDirectoryOption.IsOn && loadedFilesList.Count > 1)
            {
                // save all files to the same folder
                SaveFilesAsBatch();
            }
            else
            {
                // save all files to distinct folders
                SaveFilesSeparately();
            }
        }

        private async void SaveFilesSeparately()
        {
            IList<InternalFile> loadedFileIterationList = loadedFilesList.ToImmutableList<InternalFile>();
            foreach (var file in loadedFileIterationList)
            {
                var savePicker = new FileSavePicker();
                savePicker.FileTypeChoices.Add("PDF-Document", new List<String>() { ".pdf" });
                savePicker.SuggestedFileName = file.FileName.Replace(".pdf", "-encrypted.pdf");
                if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(App.RECENT_FILE_DIRECTORY_TOKEN))
                {
                    savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                }
                StorageFile savedFile = await savePicker.PickSaveFileAsync();
                EncryptAndSaveAsync(file, savedFile);
            }
        }       

        private async void SaveFilesAsBatch()
        {
            IList<InternalFile> loadedFileIterationList = loadedFilesList.ToImmutableList<InternalFile>();
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add(".pdf");
            StorageFolder parentfolder = await folderPicker.PickSingleFolderAsync();

            if (parentfolder != null)
            {
                foreach (var file in loadedFileIterationList)
                {
                    string fileName = file.FileName.Replace(".pdf", "-encrypted.pdf");
                    var savedFile = await parentfolder.CreateFileAsync(fileName,  CreationCollisionOption.GenerateUniqueName);                   
                    EncryptAndSaveAsync(file, savedFile);
                }
            }
            else
            {
                ToolPage.Current.NotifyUser("No output folder has been selected.", NotifyType.ErrorMessage);
            }
        }

        /// <summary>
        /// Encrypts ands saves a file.
        /// @Requires: PasswordInput.Password != null && !String.Empty().Equals(PasswordInput.Password)
        /// </summary>
        private async void EncryptAndSaveAsync(InternalFile source, StorageFile outputFile)
        {
            if (outputFile != null)
            {
                Task<Stream> outputStreamTask = outputFile.OpenStreamForWriteAsync();
                Stream outputStream = await outputStreamTask;
                if (outputStream != null)
                {
                    string password = PasswordInput.Password;

                    PdfAssembler pdfAssembler = new PdfAssembler(outputStream);
                    PageRange pageRange = PageRange.EntireDocument(source.Document);
                    ExportTask task = new ExportTask(pageRange);
                    pdfAssembler.AppendTask(task);
                    pdfAssembler.ExportFileEncrypted(password);
                    loadedFilesList.Remove(source);
                    ToolPage.Current.NotifyUser(String.Empty, NotifyType.StatusMessage);
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

        private void PasswordRepeatedInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordInput.Password.Equals(PasswordRepeatedInput.Password))
            {
                PasswordMismatchLabel.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = true;
            }
            else
            {
                PasswordMismatchLabel.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = false;
            }
        }

        private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (PasswordInput.Password.Equals(PasswordRepeatedInput.Password))
            {
                PasswordMismatchLabel.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = true;
            }
            else
            {
                PasswordMismatchLabel.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = false;
            }
        }
    }
}

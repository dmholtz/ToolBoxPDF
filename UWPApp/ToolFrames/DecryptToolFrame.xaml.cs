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
    public sealed partial class DecryptToolPage : Page
    {
        private static readonly string PAGE_TITLE = "Remove Password";

        private ToolPage rootPage;

        /// <summary>
        /// Contains only password-protected files that have not yet been decrypted
        /// </summary>
        private ObservableCollection<InternalFile> loadedFilesList;

        public DecryptToolPage()
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
            
            List<InternalFile> unprotectedFiles = new List<InternalFile>();

            foreach (var storageFile in selectedFiles)
            {
                InternalFile internalFile = await InternalFile.LoadInternalFileAsync(storageFile);

                if (internalFile.Decrypted)
                {
                    unprotectedFiles.Add(internalFile);
                }
                else
                {
                    loadedFilesList.Add(internalFile);
                }               
            }

            if(unprotectedFiles.Count > 0)
            {
                StringBuilder notification = new StringBuilder("The following are ignored because they are not password-protected:\n");
                foreach (var file in unprotectedFiles)
                {
                    notification.Append(file.FileName + "\n");
                }
                ToolPage.Current.NotifyUser(notification.ToString(), NotifyType.ErrorMessage);
            }

            if(loadedFilesList.Count>0)
            {
                ExportPanel.Visibility = Visibility.Visible;
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

            if (loadedFilesList.Count == 0)
            {
                ExportPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void AgreeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            FunctionPanel.Visibility = Visibility.Collapsed;
            ExportPanel.Visibility = Visibility.Collapsed;
        }

        private void AgreeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            FunctionPanel.Visibility = Visibility.Visible;
            if (loadedFilesList.Count > 0)
            {
                ExportPanel.Visibility = Visibility.Visible;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (OutDirectoryOption.IsOn)
            {
                // save all files to the same folder
                SaveFilesAutomatically();
            }
            else
            {
                // save all files to distinct folders
                SaveFilesManually();
            }
        }

        private async void SaveFilesManually()
        {
            IList<InternalFile> loadedFileIterationList = loadedFilesList.ToImmutableList<InternalFile>();
            int passwordMismatchCount = 0;
            foreach (var file in loadedFileIterationList)
            {
                Task<bool> decryptAttempt = file.TryDecryptingAsync(PasswordUserInput.Password);
                if (await decryptAttempt)
                {
                    var savePicker = new FileSavePicker();
                    savePicker.FileTypeChoices.Add("PDF-Document", new List<String>() { ".pdf" });
                    savePicker.SuggestedFileName = file.FileName;
                    if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(App.RECENT_FILE_DIRECTORY_TOKEN))
                    {
                        savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                    }
                    StorageFile savedFile = await savePicker.PickSaveFileAsync();

                    SaveDecryptedFileAsync(file, savedFile);
                }
                else
                {
                    passwordMismatchCount++;
                }
            }
            if (passwordMismatchCount == loadedFilesList.Count)
            {
                PasswordIncorrectLabel.Visibility = Visibility.Visible;
            }
        }

        private async void SaveFilesAutomatically()
        {
            IList<InternalFile> loadedFileIterationList = loadedFilesList.ToImmutableList<InternalFile>();
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add(".pdf");
            StorageFolder parentfolder = await folderPicker.PickSingleFolderAsync();

            if (parentfolder != null)
            {
                int passwordMismatchCount = 0;
                foreach (var file in loadedFileIterationList)
                {
                    Task<bool> decryptAttempt = file.TryDecryptingAsync(PasswordUserInput.Password);
                    if (await decryptAttempt)
                    {
                        bool replaceOption = ReplaceOption.IsChecked == null ? false : (bool)ReplaceOption.IsChecked;
                        CreationCollisionOption collisionOption = replaceOption ? CreationCollisionOption.ReplaceExisting : CreationCollisionOption.GenerateUniqueName;
                        var savedFile = await parentfolder.CreateFileAsync(file.FileName, collisionOption);
                        SaveDecryptedFileAsync(file, savedFile);
                    }
                    else
                    {
                        passwordMismatchCount++;
                    }
                }
                if (passwordMismatchCount == loadedFilesList.Count)
                {
                    PasswordIncorrectLabel.Visibility = Visibility.Visible;
                }
            }
            else
            {
                ToolPage.Current.NotifyUser("No output folder has been selected.", NotifyType.ErrorMessage);
            }            
        }

        private void InputPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ToolPage.Current.NotifyUser(String.Empty, NotifyType.ErrorMessage);
            PasswordIncorrectLabel.Visibility = Visibility.Collapsed;
        }

        private async void SaveDecryptedFileAsync(InternalFile source, StorageFile outputFile)
        {
            if (outputFile != null)
            {
                Task<Stream> outputStreamTask = outputFile.OpenStreamForWriteAsync();
                Stream outputStream = await outputStreamTask;
                if (outputStream != null)
                {
                    PdfAssembler pdfAssembler = new PdfAssembler(outputStream);
                    PageRange pageRange = PageRange.EntireDocument(source.Document);
                    ExportTask task = new ExportTask(pageRange);
                    pdfAssembler.AppendTask(task);
                    pdfAssembler.ExportFile();
                    loadedFilesList.Remove(source);
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

        private void OutDirectoryOption_Toggled(object sender, RoutedEventArgs e)
        {
            if (OutDirectoryOption.IsOn)
            {
                ResolveNameConflictPanel.Visibility = Visibility.Visible;
                AutoRenameOption.IsChecked = true;
            }
            else
            {
                ResolveNameConflictPanel.Visibility = Visibility.Collapsed; ;
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (ReplaceOption.IsChecked != null && (bool) ReplaceOption.IsChecked)
            {
                CautionReplaceFile.Visibility = Visibility.Visible;
            }
            else
            {
                CautionReplaceFile.Visibility = Visibility.Collapsed;
            }
        }
    }
}

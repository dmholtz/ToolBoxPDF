using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
                try
                {
                    InternalFile internalFile = await InternalFile.LoadInternalFileAsync(storageFile);
                    loadedFilesList.Add(internalFile);
                }
                catch
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
                ToolPage.Current.NotifyUser("Opened: "+loadedFilesList.Count.ToString(), NotifyType.StatusMessage);
            }

            
            loadedFilesView.ItemsSource = loadedFilesList;
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
            IList<object> selectedItems = e.AddedItems;
            if (selectedItems.Count > 0)
            {
                FileRemoveButton.Visibility = Visibility.Visible;      
            }
            else
            {
                FileRemoveButton.Visibility = Visibility.Collapsed;
            }
        }

        private void FileRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Generate an immutable list of items to be removed
            IList<object> selectedItems = loadedFilesView.SelectedItems.ToImmutableList();
            // Deselect all items in the listview
            loadedFilesView.SelectedItem = null;
            foreach(var selectedItem in selectedItems)
            {
                InternalFile selectedFile = (InternalFile)selectedItem;
                loadedFilesList.Remove(selectedFile);
            }
            loadedFilesView.ItemsSource = loadedFilesList;
        }
    }
}

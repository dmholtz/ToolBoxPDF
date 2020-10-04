using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniPDF_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FutureFeaturesFrame : Page
    {

        private static readonly string PAGE_TITLE = "Back to the Future";
        private ToolPage rootPage;

        public FutureFeaturesFrame()
        {
            this.InitializeComponent();
            LoadMarkdownFile();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            rootPage = ToolPage.Current;
            rootPage.SetPageTitle(PAGE_TITLE);
        }

        /// <summary>
        /// Loads and displays the markdownfile
        /// </summary>
        private async void LoadMarkdownFile()
        {
            var uri = new Uri("ms-appx:///MarkdownAssets/UpcomingFeatures.md", UriKind.RelativeOrAbsolute);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var lines = await FileIO.ReadTextAsync(file);
            MarkdownContent.Text = lines;
        }

        private async void MarkdownContent_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }
        }
    }    
}

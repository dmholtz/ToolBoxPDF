using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace UniPDF_UWP
{
    /// <summary>
    /// Scenario: About Page
    /// </summary>
    public sealed partial class ScenarioAboutPage: Page
    {
        public ScenarioAboutPage()
        {
            this.InitializeComponent();
            LoadMarkdownFile();         
        }

        /// <summary>
        /// Loads and displays the markdownfile
        /// </summary>
        private async void LoadMarkdownFile()
        {
            var uri = new Uri("ms-appx:///MarkdownAssets/AboutPage.md", UriKind.RelativeOrAbsolute);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);          
            var lines = await FileIO.ReadTextAsync(file);
            MarkdownContent.Text = lines;
        }

        private async void MarkdownContent_LinkClicked(object sender, Microsoft.Toolkit.Uwp.UI.Controls.LinkClickedEventArgs e)
        {
            if (Uri.TryCreate(e.Link, UriKind.Absolute, out Uri link))
            {
                await Launcher.LaunchUriAsync(link);
            }            
        }
    }
}

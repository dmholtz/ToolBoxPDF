using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniPDF_UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ToolPage : Page
    {
        public static ToolPage Current;

        public ToolPage()
        {
            this.InitializeComponent();

            // Clear the status block when navigating scenarios.
            NotifyUser(String.Empty, NotifyType.StatusMessage);
            Current = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame.CanGoBack)
            {
                // If there are pages in the in-app backstack
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if there are no pages in the in-app back stack
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }

            try
            {
                Tool selectedTool = (Tool)e.Parameter;
                switch(selectedTool)
                {
                    case Tool.Merge:
                        ToolFrame.Navigate(typeof(MergeToolPage));
                        break;
                    case Tool.Encrypt:
                        ToolFrame.Navigate(typeof(EncryptToolPage));
                        break;
                    case Tool.Decrypt:
                        ToolFrame.Navigate(typeof(DecryptToolPage));
                        break;
                    default:
                        NotifyUser("Navigation Error occurred: Unable to navigate to the desired tool frame.", NotifyType.ErrorMessage);
                        break;
                }
            }
            catch
            {
                NotifyUser("Navigation Error occurred: Unable to navigate to the desired tool frame.", NotifyType.ErrorMessage);
            }
        }

        public void SetPageTitle(string title)
        {
            ToolPageTitle.Text = title ?? throw new ArgumentException("Tool Page Title may not be a null-string");
        }

        /// <summary>
        /// Displays messages to the user
        /// </summary>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;

            // Collapse the StatusBlock whenever it has no text -> more space for other UI Elements
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }
        }
    }
}

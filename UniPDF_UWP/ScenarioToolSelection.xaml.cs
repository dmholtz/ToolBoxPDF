using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Controls;

namespace UniPDF_UWP
{
    /// <summary>
    /// Scenario: Tool Selection
    /// Needs to embedded into the MainPage's scenario frame
    /// </summary>
    public sealed partial class ScenarioToolSelection : Page
    {
        public ScenarioToolSelection()
        {
            this.InitializeComponent();
            showToolbox();
            
        }

        private void showToolbox()
        {
            List<ToolDefCollection> toolsSource = new List<ToolDefCollection>();

            ToolDefCollection tool = new ToolDefCollection();
            tool.ToolDefs.Add(new ToolDefinitionWrapper("A", "LimeGreen", "Assets\\split.png"));
            toolsSource.Add(tool);

            tool = new ToolDefCollection();
            tool.ToolDefs.Add(new ToolDefinitionWrapper("B", "Crimson", "Assets\\split.png"));
            toolsSource.Add(tool);

            tool = new ToolDefCollection();
            tool.ToolDefs.Add(new ToolDefinitionWrapper("C", "Midnightblue", "Assets\\split.png"));
            toolsSource.Add(tool);

            tool = new ToolDefCollection();
            tool.ToolDefs.Add(new ToolDefinitionWrapper("D", "Teal", "Assets\\split.png"));
            toolsSource.Add(tool);

            tool = new ToolDefCollection();
            tool.ToolDefs.Add(new ToolDefinitionWrapper("E", "OrangeRed", "Assets\\split.png"));
            toolsSource.Add(tool);

            tool = new ToolDefCollection();
            tool.ToolDefs.Add(new ToolDefinitionWrapper("F", "Orchid", "Assets\\split.png"));
            toolsSource.Add(tool);

            toolboxItems.Source = toolsSource;
        }

        private void Toolbox_ItemClick(object sender, ItemClickEventArgs e)
        {
            String msg = sender.ToString();
            string msg2 = ((ToolDefinitionWrapper)e.ClickedItem).ToolName;
            MainPage.Current.NotifyUser("Item has been clicked by " + msg+ " "+ msg2, NotifyType.StatusMessage);
        }

        public class ToolDefinitionWrapper
        {
            public string ToolName { get; }
            public string TileColor { get; }
            public string ImagePath { get; }

            public ToolDefinitionWrapper(string toolName, string tileColor, string imagePath)
            {
                ToolName = toolName;
                TileColor = tileColor;
                ImagePath = imagePath;
            }
        }

        public class ToolDefCollection
        {
            public ObservableCollection<ToolDefinitionWrapper> ToolDefs { get; }

            public ToolDefCollection()
            {
                ToolDefs = new ObservableCollection<ToolDefinitionWrapper>();
            }
        }
    }
}

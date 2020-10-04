using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UWPApp
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

        private const int MAX_NUMBER_OF_FEATURES = 10;  
        private ISet<ToolDefinitionWrapper> tools;

        private void initializeTools()
        {
            tools = new HashSet<ToolDefinitionWrapper>(MAX_NUMBER_OF_FEATURES);
            tools.Add(new ToolDefinitionWrapper(Tool.Merge, "Merge PDFs", "Crimson", "Assets\\merge2.png")); // #0077b3
            tools.Add(new ToolDefinitionWrapper(Tool.Select, "Select pages", "Crimson", "Assets\\select1.png"));
            tools.Add(new ToolDefinitionWrapper(Tool.Encrypt, "Encryption Tool", "Crimson", "Assets\\encrypt.png"));        
            tools.Add(new ToolDefinitionWrapper(Tool.Decrypt, "Remove Password", "Crimson", "Assets\\decrypt.png"));
            //tools.Add(new ToolDefinitionWrapper(Tool.Split, "Split PDF", "Crimson", "Assets\\split.png"));       
            tools.Add(new ToolDefinitionWrapper(Tool.FutureFeatures, "Upcoming Features", "Crimson", "Assets\\upcoming.png"));
        }

        /// <summary>
        /// Displays the Toolbox with tool names, colors and icons.
        /// </summary>
        private void showToolbox()
        {
            initializeTools();
            List<ToolDefCollection> toolsSource = new List<ToolDefCollection>();

            foreach(var toolDefinition in tools)
            {
                var toolDefCollection = new ToolDefCollection();
                toolDefCollection.ToolDefs.Add(toolDefinition);
                toolsSource.Add(toolDefCollection);
            }

            toolboxItems.Source = toolsSource;
        }

        private void Toolbox_ItemClick(object sender, ItemClickEventArgs e)
        {
            Tool clickedTool = ((ToolDefinitionWrapper)e.ClickedItem).ToolIdentifier;

            Frame rootFrame = Window.Current.Content as Frame;

            rootFrame.Navigate(typeof(ToolPage), clickedTool);
        }

        public class ToolDefinitionWrapper
        {
            public Tool ToolIdentifier { get; }
            public string ToolName { get; }
            public string TileColor { get; }
            public string ImagePath { get; }

            public ToolDefinitionWrapper(Tool identifier, string toolName, string tileColor, string imagePath)
            {
                ToolIdentifier = identifier;
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

    public enum Tool
    {
        Merge,
        Split,
        Select,
        FutureFeatures,
        Decrypt,
        Encrypt
    }
}

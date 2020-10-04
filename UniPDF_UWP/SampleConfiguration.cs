using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace UniPDF_UWP
{
    public sealed partial class MainPage : Page
    {
        public const string APP_NAME = "ToolBox PDF";

        List<Scenario> scenarios = new List<Scenario>()
        {
            new Scenario() {Title =  "ToolBox", ClassType = typeof(ScenarioToolSelection) },
            new Scenario() {Title = "About this App", ClassType = typeof(ScenarioAboutPage)},
        };
    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }
}

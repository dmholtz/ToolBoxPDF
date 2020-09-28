using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;

namespace UniPDF_UWP
{
    public sealed partial class MainPage : Page
    {
        public const string APP_NAME = "UniversalPDF";

        List<Scenario> scenarios = new List<Scenario>()
        {
            new Scenario() {Title =  "Tools", ClassType = typeof(ScenarioToolSelection) },
            new Scenario() {Title = "Legal Notes", ClassType = typeof(ScenarioLegalNotes)},
        };
    }

    public class Scenario
    {
        public string Title { get; set; }
        public Type ClassType { get; set; }
    }
}

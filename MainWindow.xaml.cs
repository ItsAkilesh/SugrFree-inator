using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SugrFree
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            nvSample.SelectedItem = nvSample.MenuItems[0]; // Select the first item by default
            contentFrame.Navigate(typeof(SugrFree.Pages.HomePage)); // Navigate to HomePage by default
            nvSample.SelectionChanged += NvSample_SelectionChanged;
        }

        private void NvSample_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                contentFrame.Navigate(typeof(SugrFree.Pages.SettingsPage));
            }
            else
            {
                // Get the selected NavigationViewItem
                var selectedItem = args.SelectedItem as NavigationViewItem;

                // Navigate to the corresponding page based on the selected NavigationViewItem's Tag
                if (selectedItem != null)
                {
                    switch (selectedItem.Tag.ToString())
                    {
                        case "HomePage":
                            contentFrame.Navigate(typeof(SugrFree.Pages.HomePage));
                            break;
                        case "SugrFreePage":
                            contentFrame.Navigate(typeof(SugrFree.Pages.SugrFreePage));
                            break;
                            // Add cases for other pages if needed
                    }
                }
            }
        }

    }

}

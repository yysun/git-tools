using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace F1SYS.VsGitToolsPackage
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    public partial class MyControl : UserControl
    {
        // private SccProviderService service;
        //private GitFileStatusTracker tracker;
        private MyToolWindow toolWindow;
        private IVsTextView textView;
        private string[] diffLines;

        private GridViewColumnHeader _currentSortedColumn;
        private ListSortDirection _lastSortDirection;

        public MyControl(MyToolWindow toolWindow)
        {
            InitializeComponent();
            this.toolWindow = toolWindow;
            //this.service = BasicSccProvider.GetServiceEx<SccProviderService>();
        }

        private void listView1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void listView1_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void listView1_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void listView1_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void listView1_Click(object sender, RoutedEventArgs e)
        {

        }

        private void listView1_KeyUp(object sender, KeyEventArgs e)
        {

        }

        private void checkBoxAllStaged_Click(object sender, RoutedEventArgs e)
        {

        }

        private void checkBoxSelected_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuStage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuUnstage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuCompare_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuUndo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuDeleteFile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuIgnore_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuIgnoreFile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuIgnoreFilePath_Click(object sender, RoutedEventArgs e)
        {

        }

        private void menuIgnoreFileExt_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DiffEditor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

    }

    public static class ExtHelper
    {
        public static TChild FindVisualChild<TChild>(this DependencyObject obj, string name = null) where TChild : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is TChild && (name == null || ((Control)child).Name == name))
                {
                    return (TChild)child;
                }
                else
                {
                    TChild childOfChild = FindVisualChild<TChild>(child, name);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}
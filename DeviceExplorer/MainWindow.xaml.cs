using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DeviceExplorer.Model;
using DeviceExplorer.Utilities;

namespace DeviceExplorer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DeviceManagerItem = new DeviceManagerItem();

            TV.ItemsSource = new TreeItem[] { DeviceManagerItem };
        }

        public DeviceManagerItem DeviceManagerItem { get; }

        protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
        {
            var item = (e.OriginalSource as DependencyObject).GetVisualSelfOrParent<TreeViewItem>();
            if (item != null)
            {
                item.Focus();
                e.Handled = true;
                return;
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewHiddenItems_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var item = TV.GetSelectedDataContext<TreeItem>();
            if (item != null)
            {
                TV.ContextMenu.DataContext = item;
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void About_Click(object sender, RoutedEventArgs e) => MessageBox.Show(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title + " - " + (IntPtr.Size == 4 ? "32" : "64") + "-bit" + Environment.NewLine + "Copyright (C) 2021-" + DateTime.Now.Year + " Simon Mourier. All rights reserved.", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title, MessageBoxButton.OK, MessageBoxImage.Information);
        private void View_Refresh(object sender, RoutedEventArgs e)
        {

        }

        private void TV_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is TreeItem item)
            {
                DG.ItemsSource = item.Properties;//.Cast<DeviceItem.DeviceProperty>();
            }
        }

        private void ExpandAll_Click(object sender, RoutedEventArgs e)
        {
            var item = TV.GetSelectedDataContext<TreeItem>();
            if (item != null)
            {
                item.ExpandAll();
            }
        }

        private void CollapseAll_Click(object sender, RoutedEventArgs e)
        {
            var item = TV.GetSelectedDataContext<TreeItem>();
            if (item != null)
            {
                item.CollapseAll();
            }
        }
    }
}

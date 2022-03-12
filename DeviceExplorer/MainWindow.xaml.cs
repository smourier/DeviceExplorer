using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Enumeration;

namespace DeviceExplorer
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DeviceInterfaceWatcher = DeviceInformation.CreateWatcher(string.Empty, Array.Empty<string>(), DeviceInformationKind.DeviceInterface);
            DeviceInterfaceWatcher.Added += OnDeviceAdded;
            DeviceInterfaceWatcher.Removed += OnDeviceRemoved;
            DeviceInterfaceWatcher.Updated += OnDeviceUpdated;
            DeviceInterfaceWatcher.Start();
        }

        public DeviceWatcher DeviceInterfaceWatcher { get; }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewHiddenItems_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {

        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();
        private void About_Click(object sender, RoutedEventArgs e) => MessageBox.Show(Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title + " - " + (IntPtr.Size == 4 ? "32" : "64") + "-bit" + Environment.NewLine + "Copyright (C) 2021-" + DateTime.Now.Year + " Simon Mourier. All rights reserved.", Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title, MessageBoxButton.OK, MessageBoxImage.Information);
        private void View_Refresh(object sender, RoutedEventArgs e)
        {

        }

        private void TV_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }
    }
}

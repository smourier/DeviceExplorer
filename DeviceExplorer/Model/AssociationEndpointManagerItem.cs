using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using DeviceExplorer.Utilities;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class AssociationEndpointManagerItem : TreeItem
    {
        public AssociationEndpointManagerItem()
            : base(null)
        {
            var requestedProperties = new string[]
            {
                "System.Devices.GlyphIcon",
                "System.Devices.Aep.AepId",
                "System.Devices.Aep.CanPair",
                "System.Devices.Aep.Category",
                "System.Devices.Aep.ContainerId",
                "System.Devices.Aep.DeviceAddress",
                "System.Devices.Aep.IsConnected",
                "System.Devices.Aep.IsPaired",
                "System.Devices.Aep.IsPresent",
                "System.Devices.Aep.Manufacturer",
                "System.Devices.Aep.ModelId",
                "System.Devices.Aep.ModelName",
                "System.Devices.Aep.ProtocolId",
                "System.Devices.Aep.SignalStrength",

                // these are specific to bluetooth
                "System.Devices.Aep.Bluetooth.LastSeenTime",
                "System.Devices.Aep.Bluetooth.IssueInquiry",
                "System.Devices.Aep.Bluetooth.Le.ActiveScanning",
                "System.Devices.Aep.Bluetooth.Le.AddressType",
                "System.Devices.Aep.Bluetooth.Le.Advertisement",
                "System.Devices.Aep.Bluetooth.Le.Appearance",
                "System.Devices.Aep.Bluetooth.Le.Appearance.Category",
                "System.Devices.Aep.Bluetooth.Le.Appearance.Subcategory",
                "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                "System.Devices.Aep.Bluetooth.Le.ScanInterval",
                "System.Devices.Aep.Bluetooth.Le.ScanResponse",
                "System.Devices.Aep.Bluetooth.Le.ScanWindow",
                "System.Devices.Aep.Bluetooth.Le.IsConnectable",
            };

            Watcher = DeviceInformation.CreateWatcher(null, requestedProperties, DeviceInformationKind.AssociationEndpoint);
            Watcher.Added += OnDeviceAdded;
            Watcher.Removed += OnDeviceRemoved;
            Watcher.Updated += OnDeviceUpdated;
            Watcher.Start();

            Name = "Assocation Endpoints";
            IsExpanded = true;
            using var ico = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            Image = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        public DeviceWatcher Watcher { get; }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate device)
        {
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate device)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                foreach (var protocolItem in Children)
                {
                    var item = protocolItem.Children.OfType<AssociationEndpointItem>().FirstOrDefault(ep => ep.Id == device.Id);
                    if (item == null)
                        continue;

                    protocolItem.Children.Remove(item);
                }
            });
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
        {
            if (!device.Properties.TryGetValue<Guid>("System.Devices.Aep.ProtocolId", out var protocolId) || protocolId == Guid.Empty)
                return;

            App.Current?.Dispatcher?.Invoke(() =>
            {
                var protocolItem = Children.OfType<AssociationEndpointProtocolItem>().FirstOrDefault(i => i.Id == protocolId);
                if (protocolItem == null)
                {
                    protocolItem = new AssociationEndpointProtocolItem(this, protocolId);
                    Children.Add(protocolItem);
                }

                var item = protocolItem.Children.OfType<AssociationEndpointItem>().FirstOrDefault(ep => ep.Id == device.Id);
                if (item == null)
                {
                    item = new AssociationEndpointItem(protocolItem, device);
                    protocolItem.Children.Add(item);
                }
            });
        }
    }
}

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
                "System.Devices.Aep.Category",
                "System.Devices.Aep.ContainerId",
                "System.Devices.Aep.DeviceAddress",
                "System.Devices.Aep.IsConnected",
                "System.Devices.Aep.IsPaired",
                "System.Devices.Aep.IsPresent",
                "System.Devices.Aep.ProtocolId",
                "System.Devices.Aep.Bluetooth.Le.IsConnectable",
                "System.Devices.Aep.SignalStrength",
                "System.Devices.Aep.Bluetooth.LastSeenTime",
                "System.Devices.Aep.Bluetooth.Le.IsConnectable",
            };

            //requestedProperties = new string[]
            //{
            //    "System.ItemNameDisplay",
            //    "System.Devices.AepContainer.Categories",
            //    "System.Devices.AepContainer.Children",
            //    "System.Devices.AepContainer.CanPair",
            //    "System.Devices.AepContainer.ContainerId",
            //    "System.Devices.AepContainer.IsPaired",
            //    "System.Devices.AepContainer.IsPresent",
            //    "System.Devices.AepContainer.Manufacturer",
            //    "System.Devices.AepContainer.ModelIds",
            //    "System.Devices.AepContainer.ModelName",
            //    "System.Devices.AepContainer.ProtocolIds",
            //    "System.Devices.AepContainer.SupportedUriSchemes",
            //    "System.Devices.AepContainer.SupportsAudio",
            //    "System.Devices.AepContainer.SupportsImages",
            //    "System.Devices.AepContainer.SupportsVideo",
            //};

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

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            //App.Current.Dispatcher.Invoke(() =>
            //{
            //    var item = Children.Cast<AssociationEndpointProtocolItem>().FirstOrDefault(i => i.Id == update.Id);
            //    if (item != null)
            //    {
            //        Children.Remove(item);
            //    }
            //});
        }

        private void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
        {
            if (!device.Properties.TryGetValue<Guid>("System.Devices.Aep.ProtocolId", out var protocolId) || protocolId == Guid.Empty)
                return;

            App.Current.Dispatcher.Invoke(() =>
            {
                var item = Children.Cast<AssociationEndpointProtocolItem>().FirstOrDefault(i => i.Id == protocolId);
                if (item == null)
                {
                    item = new AssociationEndpointProtocolItem(this, protocolId);
                    Children.Add(item);
                }
            });
        }
    }
}

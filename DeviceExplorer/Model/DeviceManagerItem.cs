using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class DeviceManagerItem : TreeItem
    {
        internal readonly ConcurrentDictionary<string, DeviceInterfaceItem> _deviceInterfaces = new();

        public DeviceManagerItem()
            : base(null)
        {
            Watcher = DeviceInformation.CreateWatcher(string.Empty, Array.Empty<string>(), DeviceInformationKind.DeviceInterface);
            Watcher.Added += OnDeviceAdded;
            Watcher.Removed += OnDeviceRemoved;
            Watcher.Updated += OnDeviceUpdated;
            Watcher.Start();

            Name = "Device Manager";
            IsExpanded = true;
            using var ico = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            Image = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        public DeviceWatcher Watcher { get; }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            if (!_deviceInterfaces.TryGetValue(update.Id, out var deviceInterfaceItem))
                return;

            App.Current.Dispatcher.Invoke(() =>
            {
                deviceInterfaceItem.Parent.UpdateDeviceInterface(update);
            });
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate update)
        {
            if (!_deviceInterfaces.TryGetValue(update.Id, out var deviceInterfaceItem))
                return;

            // TODO
        }

        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
        {
            var information = await GetInformationAsync(device).ConfigureAwait(false);
            var guid = information.Properties["System.Devices.ClassGuid"];
            if (guid == null)
                return;

            var classGuid = (Guid)guid;
            App.Current.Dispatcher.Invoke(() =>
            {
                var classItem = Children.Cast<DeviceClassItem>().FirstOrDefault(i => i.ClassGuid == classGuid);
                if (classItem == null)
                {
                    classItem = new DeviceClassItem(this, classGuid);
                    Children.Add(classItem);
                }

                classItem.AddDevice(information, device);
            });
        }

        private static async Task<DeviceInformation> GetInformationAsync(DeviceInformation info) => await DeviceInformation.CreateFromIdAsync((string)info.Properties["System.Devices.DeviceInstanceId"], new[] { "System.Devices.ClassGuid" }, DeviceInformationKind.Device);
    }
}

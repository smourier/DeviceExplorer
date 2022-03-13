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
        internal readonly ConcurrentDictionary<string, DeviceInterfaceItem> _deviceInterfaces = new ConcurrentDictionary<string, DeviceInterfaceItem>();
        private readonly ConcurrentDictionary<Guid, DeviceClassItem> _classItems = new ConcurrentDictionary<Guid, DeviceClassItem>();

        public DeviceManagerItem()
            : base(null, true)
        {
            Watcher = DeviceInformation.CreateWatcher(string.Empty, Array.Empty<string>(), DeviceInformationKind.DeviceInterface);
            Watcher.Added += OnDeviceAdded;
            Watcher.Removed += OnDeviceRemoved;
            Watcher.Updated += OnDeviceUpdated;
            Watcher.Start();

            Name = "Device Manager";
            //IsExpanded = true;
            using var ico = Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule.FileName);
            Image = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        public DeviceWatcher Watcher { get; }

        protected override void LoadChildren()
        {
            Children.Clear();
            foreach (var item in _classItems.OrderBy(i => i.Value.Name))
            {
                Children.Add(item.Value);
            }
        }

        private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate device)
        {
        }

        private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate device)
        {
        }

        private async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation device)
        {
            var information = await GetInformationAsync(device).ConfigureAwait(false);
            var classGuid = (Guid)information.Properties["System.Devices.ClassGuid"];

            if (!_classItems.TryGetValue(classGuid, out var classItem))
            {
                classItem = new DeviceClassItem(this, classGuid);
                _classItems[classGuid] = classItem;
            }

            classItem.AddDevice(information, device);
        }

        private static async Task<DeviceInformation> GetInformationAsync(DeviceInformation info) => await DeviceInformation.CreateFromIdAsync((string)info.Properties["System.Devices.DeviceInstanceId"], new[] { "System.Devices.ClassGuid" }, DeviceInformationKind.Device);
    }
}

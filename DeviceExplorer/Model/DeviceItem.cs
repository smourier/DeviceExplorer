using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class DeviceItem : TreeItem
    {
        private readonly ConcurrentDictionary<string, DeviceInterfaceItem> _items = new ConcurrentDictionary<string, DeviceInterfaceItem>();
        private readonly Lazy<BitmapSource> _icon;

        public DeviceItem(DeviceClassItem parent, DeviceInformation device)
            : base(parent, true)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            Device = device;
            Name = device.Name;

            _icon = new Lazy<BitmapSource>(() =>
            {
                if (_items.Count == 0)
                    return null;

                if (!_items.FirstOrDefault().Value.DeviceInterface.Properties.TryGetValue("System.Devices.Icon", out var value) || value is not string location)
                    return null;

                var index = DeviceClassItem.ParseIconLocation(location, out var path);
                if (path == null)
                    return null;

                return DeviceClassItem.LoadIcon(path, index);
            }, false);
        }

        public DeviceInformation Device { get; }
        public new DeviceClassItem Parent => (DeviceClassItem)base.Parent;
        public DeviceManagerItem Root => Parent.Parent;
        public override ImageSource Image => _icon.Value;

        protected override void LoadChildren()
        {
            Children.Clear();
            foreach (var item in _items.OrderBy(i => i.Value.Name))
            {
                Children.Add(item.Value);
            }
        }

        internal void AddDeviceInterface(DeviceInformation deviceInterface)
        {
            var interfaceItem = new DeviceInterfaceItem(this, deviceInterface);
            _items[deviceInterface.Id] = interfaceItem;

            Root._deviceInterfaces[deviceInterface.Id] = interfaceItem;
        }
    }
}

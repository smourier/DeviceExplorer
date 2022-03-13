using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class DeviceInterfaceItem : TreeItem
    {
        private readonly Lazy<BitmapSource> _icon;

        public DeviceInterfaceItem(DeviceItem parent, DeviceInformation deviceInterface)
            : base(parent, false)
        {
            if (deviceInterface == null)
                throw new ArgumentNullException(nameof(deviceInterface));

            DeviceInterface = deviceInterface;
            Name = deviceInterface.Name;
            IsHidden = !deviceInterface.IsEnabled;

            _icon = new Lazy<BitmapSource>(() =>
            {
                if (!DeviceInterface.Properties.TryGetValue("System.Devices.Icon", out var value) || value is not string location)
                    return null;

                var index = DeviceClassItem.ParseIconLocation(location, out var path);
                if (path == null)
                    return null;

                return DeviceClassItem.LoadIcon(path, index);
            }, false);
        }

        public DeviceInformation DeviceInterface { get; }
        public new DeviceItem Parent => (DeviceItem)base.Parent;
        public override ImageSource Image => _icon.Value;

        protected override void LoadChildren() { }
    }
}

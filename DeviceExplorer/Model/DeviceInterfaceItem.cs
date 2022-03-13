using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeviceExplorer.Utilities;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class DeviceInterfaceItem : TreeItem
    {
        private readonly Lazy<BitmapSource> _icon;
        private readonly SortableObservableCollection<InterfaceProperty> _properties = new SortableObservableCollection<InterfaceProperty>
        {
            SortingSelector = o => o.Name
        };

        public DeviceInterfaceItem(DeviceItem parent, DeviceInformation deviceInterface)
            : base(parent)
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

            _properties.Add(new InterfaceProperty("Id")
            {
                Value = deviceInterface.Id
            });

            foreach (var prop in deviceInterface.Properties)
            {
                var p = new InterfaceProperty(prop.Key)
                {
                    Value = prop.Value
                };
                _properties.Add(p);
            }
        }

        private class InterfaceProperty : Property
        {
            public InterfaceProperty(string name)
                : base(name)
            {
            }

            public object Value { get; set; }
        }

        public DeviceInformation DeviceInterface { get; }
        public new DeviceItem Parent => (DeviceItem)base.Parent;
        public override ImageSource Image => _icon.Value;
        public override IEnumerable<Property> Properties => _properties;
    }
}

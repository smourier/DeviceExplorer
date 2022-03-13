using System;
using System.Collections.Generic;
using System.Linq;
using DeviceExplorer.Utilities;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class DeviceItem : TreeItem
    {
        private readonly SortableObservableCollection<DeviceProperty> _properties = new SortableObservableCollection<DeviceProperty>
        {
            SortingSelector = o => o.Name
        };
        
        public DeviceItem(DeviceClassItem parent, DeviceInformation device)
            : base(parent)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));

            Device = device;
            Name = device.Name;

            _properties.Add(new DeviceProperty("Id")
            {
                Value = device.Id
            });

            foreach (var prop in device.Properties)
            {
                var p = new DeviceProperty(prop.Key)
                {
                    Value = prop.Value
                };
                _properties.Add(p);
            }
        }

        private class DeviceProperty : Property
        {
            public DeviceProperty(string name)
                : base(name)
            {
            }

            public object Value { get; set; }
        }

        public DeviceInformation Device { get; }
        public new DeviceClassItem Parent => (DeviceClassItem)base.Parent;
        public DeviceManagerItem Root => Parent.Parent;
        public override IEnumerable<Property> Properties => _properties;

        internal void AddDeviceInterface(DeviceInformation deviceInterface)
        {
            var interfaceItem = Children.Cast<DeviceInterfaceItem>().FirstOrDefault(d => d.DeviceInterface.Id == deviceInterface.Id);
            if (interfaceItem == null)
            {
                interfaceItem = new DeviceInterfaceItem(this, deviceInterface);
                Children.Add(interfaceItem);
                if (Image == null)
                {
                    Image = interfaceItem.Image;
                }
            }

            Root._deviceInterfaces[deviceInterface.Id] = interfaceItem;
        }

        internal void UpdateDeviceInterface(DeviceInformationUpdate deviceInterface)
        {
            var interfaceItem = Children.Cast<DeviceInterfaceItem>().FirstOrDefault(d => d.DeviceInterface.Id == deviceInterface.Id);
            if (interfaceItem == null)
                return;

            if (!deviceInterface.Properties.TryGetValue("System.Devices.InterfaceEnabled", out var obj) || obj is not bool enabled)
                return;

            interfaceItem.IsHidden = !enabled;
        }
    }
}

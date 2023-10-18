using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeviceExplorer.Resources.Hid;
using DeviceExplorer.Utilities;
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;

namespace DeviceExplorer.Model
{
    public class DeviceInterfaceItem : TreeItem
    {
        private readonly Lazy<BitmapSource> _icon;
        private readonly SortableObservableCollection<ValueProperty> _properties = new()
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

            _properties.Add(new ValueProperty("Id")
            {
                Value = deviceInterface.Id
            });

            foreach (var prop in deviceInterface.Properties)
            {
                var p = new ValueProperty(prop.Key)
                {
                    Value = prop.Value
                };
                _properties.Add(p);

                if (p.Name == "System.Devices.DeviceInstanceId" && p.Value is string s && s.StartsWith(@"HID\"))
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var dev = await HidDevice.FromIdAsync(deviceInterface.Id, Windows.Storage.FileAccessMode.Read);
                            if (dev != null)
                            {
                                var page = (HID_USAGE_PAGE)dev.UsagePage;
                                _properties.Add(new ValueProperty("HID.UsagePage") { Value = $"{page} {dev.UsagePage.ToHex()}" });
                                _properties.Add(new ValueProperty("HID.ProductId") { Value = dev.ProductId.ToHex() });

                                var type = page.GetUsageType();
                                if (type == null)
                                {
                                    _properties.Add(new ValueProperty("HID.UsageId") { Value = dev.UsageId.ToHex() });
                                }
                                else
                                {
                                    if (Enum.TryParse(type, dev.UsageId.ToString(), out var e))
                                    {
                                        _properties.Add(new ValueProperty("HID.UsageId") { Value = $"{e} {dev.UsageId.ToHex()}" });
                                    }
                                }

                                _properties.Add(new ValueProperty("HID.Version") { Value = dev.Version });
                                var vendor = Usb.GetVendorName(dev.VendorId);
                                if (vendor != null)
                                {
                                    _properties.Add(new ValueProperty("HID.VendorId") { Value = $"'{vendor}' {dev.VendorId.ToHex()}" });
                                }
                                else
                                {
                                    _properties.Add(new ValueProperty("HID.VendorId") { Value = dev.VendorId.ToHex() });
                                }
                            }
                        }
                        catch
                        {
                            // continue
                        }
                    });
                }
            }
        }

        public DeviceInformation DeviceInterface { get; }
        public new DeviceItem Parent => (DeviceItem)base.Parent;
        public override ImageSource Image => _icon.Value;
        public override IEnumerable<Property> Properties => _properties;
    }
}

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeviceExplorer.Utilities;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class AssociationEndpointItem : TreeItem
    {
        private readonly Lazy<BitmapSource> _icon;
        private readonly SortableObservableCollection<ValueProperty> _properties = new()
        {
            SortingSelector = o => o.Name
        };

        public AssociationEndpointItem(AssociationEndpointManagerItem parent, DeviceInformation info)
            : base(parent)
        {
            Name = info.Name.Nullify() ?? info.Id;
            Name = info.Id;
            Id = info.Id;

            _icon = new Lazy<BitmapSource>(() =>
            {
                if (!info.Properties.TryGetValue<string>("System.Devices.GlyphIcon", out var location) &&
                    !info.Properties.TryGetValue<string>("System.Devices.Icon", out location))
                    return null;

                var index = DeviceClassItem.ParseIconLocation(location, out var path);
                if (path == null)
                    return null;

                return DeviceClassItem.LoadIcon(path, index);
            }, false);

            foreach (var prop in info.Properties)
            {
                var p = new ValueProperty(prop.Key)
                {
                    Value = prop.Value
                };
                _properties.Add(p);
            }
        }

        public string Id { get; }
        public override ImageSource Image => _icon.Value;
        public override IEnumerable<Property> Properties => _properties;
    }
}

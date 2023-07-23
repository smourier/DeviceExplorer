using System;
using System.Collections.Generic;
using System.Linq;
using DeviceExplorer.Utilities;
using Windows.Devices.Bluetooth.Advertisement;

namespace DeviceExplorer.Model
{
    public class BluetoothLEAdvertisementItem : TreeItem
    {
        private readonly SortableObservableCollection<ValueProperty> _properties = new()
        {
            SortingSelector = o => o.Name
        };

        public BluetoothLEAdvertisementItem(BluetoothLEAdvertisementsItem advertisements, BluetoothLEAdvertisementReceivedEventArgs advertisement, string key)
            : base(advertisements)
        {
            Key = key;

            var name = advertisement.AdvertisementType.ToString();
            var company = string.Join("| ", advertisement.Advertisement.ManufacturerData.Select(m => Bluetooth.GetCompanyName(m.CompanyId))).Nullify();
            if (company != null)
            {
                name += " " + company;
            }
            Name = name;
            Image = DeviceClassItem.LoadIcon(@"%windir%\system32\DDORes.dll", -2381);

            Update(advertisement);
        }

        public string Key { get; }
        public virtual DateTime LastReceived { get => DictionaryObjectGetPropertyValue<DateTime>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual int ReceivedCount { get => DictionaryObjectGetPropertyValue<int>(); set => DictionaryObjectSetPropertyValue(value); }

        public void Update(BluetoothLEAdvertisementReceivedEventArgs advertisement)
        {
            LastReceived = DateTime.Now;
            ReceivedCount++;

            _properties.Clear();
            _properties.Add(new ValueProperty(nameof(advertisement.IsAnonymous)) { Value = advertisement.IsAnonymous });
            _properties.Add(new ValueProperty(nameof(advertisement.IsConnectable)) { Value = advertisement.IsConnectable });
            _properties.Add(new ValueProperty(nameof(advertisement.IsDirected)) { Value = advertisement.IsDirected });
            _properties.Add(new ValueProperty(nameof(advertisement.IsScannable)) { Value = advertisement.IsScannable });
            _properties.Add(new ValueProperty(nameof(advertisement.IsScanResponse)) { Value = advertisement.IsScanResponse });
            _properties.Add(new ValueProperty(nameof(advertisement.AdvertisementType)) { Value = advertisement.AdvertisementType });
            _properties.Add(new ValueProperty(nameof(advertisement.BluetoothAddress)) { Value = Bluetooth.GetMacAddress(advertisement.BluetoothAddress) });
            _properties.Add(new ValueProperty(nameof(advertisement.BluetoothAddressType)) { Value = advertisement.BluetoothAddressType });
            _properties.Add(new ValueProperty(nameof(advertisement.RawSignalStrengthInDBm)) { Value = advertisement.RawSignalStrengthInDBm });
            _properties.Add(new ValueProperty(nameof(advertisement.TransmitPowerLevelInDBm)) { Value = advertisement.TransmitPowerLevelInDBm });

            _properties.Add(new ValueProperty(nameof(advertisement.Advertisement.LocalName)) { Value = advertisement.Advertisement.LocalName });
            _properties.Add(new ValueProperty(nameof(advertisement.Advertisement.Flags)) { Value = advertisement.Advertisement.Flags });
            _properties.Add(new ValueProperty(nameof(advertisement.Advertisement.ServiceUuids)) { Value = string.Join("|", advertisement.Advertisement.ServiceUuids.Select(u => Bluetooth.GetService(u))) });

            var i = 0;
            foreach (var data in advertisement.Advertisement.ManufacturerData)
            {
                var companyName = Bluetooth.GetCompanyName(data.CompanyId);
                var bytes = data.Data.AsBytes();
                var dump = bytes.ToHexaDump();
                _properties.Add(new ValueProperty("Data " + i + Environment.NewLine + companyName) { Value = dump });
                i++;
            }

            i = 0;
            foreach (var section in advertisement.Advertisement.DataSections)
            {
                var dataType = Bluetooth.GetDataType(section.DataType);
                var bytes = section.Data.AsBytes();
                var dump = bytes.ToHexaDump();
                _properties.Add(new ValueProperty("DataSection " + i + Environment.NewLine + dataType) { Value = dump });
                i++;
            }

            _properties.Add(new ValueProperty(nameof(LastReceived)) { Value = LastReceived });
            _properties.Add(new ValueProperty(nameof(ReceivedCount)) { Value = ReceivedCount });
        }

        public override IEnumerable<Property> Properties => _properties;

        internal static string GetKey(BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // come up with a key that represents a type of ad (so we remove variable properties such as time, decibels, etc.)
            var dic = new Dictionary<string, object>
            {
                ["a"] = args.IsAnonymous,
                ["c"] = args.IsConnectable,
                ["d"] = args.IsDirected,
                ["s"] = args.IsScannable,
                ["r"] = args.IsScanResponse,
                ["t"] = args.AdvertisementType,
                ["b"] = args.BluetoothAddress,
                ["z"] = args.BluetoothAddressType,
            };

            dic["n"] = args.Advertisement.LocalName;
            dic["f"] = args.Advertisement.Flags.GetValueOrDefault();
            dic["u"] = string.Join("|", args.Advertisement.ServiceUuids);

            var i = 0;
            foreach (var data in args.Advertisement.ManufacturerData)
            {
                dic["o" + i + "," + nameof(data.CompanyId)] = data.CompanyId;
                var bytes = data.Data.AsBytes();
                dic["o" + i + "," + nameof(data.Data)] = string.Join(string.Empty, bytes.Select(b => b.ToString("X2")));
                i++;
            }

            i = 0;
            foreach (var section in args.Advertisement.DataSections)
            {
                dic["x" + i + "," + nameof(section.DataType)] = section.DataType;
                var bytes = section.Data.AsBytes();
                dic["x" + i + "," + nameof(section.Data)] = string.Join(string.Empty, bytes.Select(b => b.ToString("X2")));
                i++;
            }

            return string.Join("|", dic.Where(kv => kv.Value != null && !string.Empty.Equals(kv.Value)).Select(kv => kv.Key + ":" + kv.Value));
        }
    }
}

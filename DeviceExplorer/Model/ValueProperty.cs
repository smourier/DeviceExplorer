using System;
using System.Globalization;
using DeviceExplorer.Utilities;

namespace DeviceExplorer.Model
{
    public class ValueProperty : Property
    {
        private object _value;

        public ValueProperty(string name)
            : base(name)
        {
        }

        // just for the order by property grid
        public new string Name => base.Name;

        public virtual object Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;

                if (value is string[] strings)
                {
                    _value = string.Join("| ", strings);
                    return;
                }

                if (value is Guid[] guids)
                {
                    _value = string.Join("| ", guids);
                    return;
                }

                if (value is byte[] bytes)
                {
                    _value = Conversions.ToHexaDump(bytes, count: 256);
                    return;
                }
                _value = value;

                Hint = Guids.GetName(_value);

                // check usb & pci vendor
                if (Hint == null && _value is string str)
                {
                    var hidTok = @"HID\VID_";
                    var usbTok = @"USB\VID_";
                    var pos = str.IndexOf(usbTok, StringComparison.OrdinalIgnoreCase);
                    if (pos < 0)
                    {
                        pos = str.IndexOf(hidTok, StringComparison.OrdinalIgnoreCase);
                    }

                    if (pos >= 0 && (pos + usbTok.Length + 4) <= str.Length)
                    {
                        var hex = str.Substring(pos + usbTok.Length, 4);
                        if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var us) &&
                            Usb.VendorsIds.TryGetValue(us, out var name))
                        {
                            Hint = name;
                        }
                    }

                    var pciTok = @"PCI\VEN_";
                    pos = str.IndexOf(pciTok, StringComparison.OrdinalIgnoreCase);
                    if (pos >= 0 && (pos + pciTok.Length + 4) <= str.Length)
                    {
                        var hex = str.Substring(pos + pciTok.Length, 4);
                        if (ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var us) &&
                            Pci.VendorsIds.TryGetValue(us, out var name))
                        {
                            Hint = name;
                        }
                    }
                }
            }
        }

        public string Hint { get; private set; }

        public override string ToString() => Name + "\t" + Value;
    }
}

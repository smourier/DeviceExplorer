using System;
using System.Collections.Generic;
using DeviceExplorer.Utilities;
using Microsoft.Win32;

namespace DeviceExplorer.Model
{
    public class AssociationEndpointProtocolItem : TreeItem
    {
        private static readonly Lazy<Dictionary<Guid, string>> _namesFromRegistry = new Lazy<Dictionary<Guid, string>>(GetNamesFromRegistry);

        private readonly SortableObservableCollection<ProtocolProperty> _properties = new()
        {
            SortingSelector = o => o.Name
        };

        public AssociationEndpointProtocolItem(AssociationEndpointManagerItem parent, Guid protocolId)
            : base(parent)
        {
            Id = protocolId;
            if (protocolId == Guids.ProtocolId_UPNP)
            {
                Name = "UPnP (including DIAL and DLNA)";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_USB);
            }
            else if (protocolId == Guids.ProtocolId_WSD)
            {
                Name = "Web services on devices (WSD)";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_NETSERVICE);
            }
            else if (protocolId == Guids.ProtocolId_WiFiDirect)
            {
                Name = "Wi-Fi Direct";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_NETCLIENT);
            }
            else if (protocolId == Guids.ProtocolId_DNS_SD)
            {
                Name = "DNS service discovery (DNS-SD)";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_NETSERVICE);
            }
            else if (protocolId == Guids.ProtocolId_POS)
            {
                Name = "Point of service";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_NETSERVICE);
            }
            else if (protocolId == Guids.ProtocolId_NetworkPrinters)
            {
                Name = "Network printers (active directory printers)";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_PRINTER);
            }
            else if (protocolId == Guids.ProtocolId_WNC)
            {
                Name = "Windows connect now (WNC)";
            }
            else if (protocolId == Guids.ProtocolId_WiGigDocks)
            {
                Name = "WiGig docks";
            }
            else if (protocolId == Guids.ProtocolId_WifiprovisioningForHPPrinters)
            {
                Name = "Wi-Fi provisioning for HP printers";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_PRINTER);
            }
            else if (protocolId == Guids.ProtocolId_Bluetooth)
            {
                Name = "Bluetooth";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_BLUETOOTH);
            }
            else if (protocolId == Guids.ProtocolId_BluetoothLE)
            {
                Name = "Bluetooth LE";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_BLUETOOTH);
            }
            else if (protocolId == Guids.ProtocolId_NetworkCamera)
            {
                Name = "Network Camera";
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_CAMERA);
            }
            else
            {
                if (_namesFromRegistry.Value.TryGetValue(Id, out var name))
                {
                    Name = name;
                }
                else
                {
                    Name = Id.ToString();
                }
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_UNKNOWN);
            }

            if (Image == null)
            {
                Image = DeviceClassItem.GetClassIcon(Guids.GUID_DEVCLASS_EXTENSION);
            }

            var prop = new ProtocolProperty(Name)
            {
                ProtocolId = protocolId.ToString("B")
            };

            if (Guids.Names.TryGetValue(protocolId, out var guidName))
            {
                prop.GuidName = guidName;
            }

            _properties.Add(prop);
        }

        public Guid Id { get; set; }
        public override IEnumerable<Property> Properties => _properties;

        private sealed class ProtocolProperty : Property
        {
            public ProtocolProperty(string name)
                : base(name)
            {
            }

            public string ProtocolId { get; set; }
            public string GuidName { get; set; }
        }

        private static Dictionary<Guid, string> GetNamesFromRegistry()
        {
            var dic = new Dictionary<Guid, string>();
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Device Association Framework\InboxProviders", false))
            {
                if (key != null)
                {
                    foreach (var name in key.GetSubKeyNames())
                    {
                        using var subKey = key.OpenSubKey(name, false);
                        if (subKey != null)
                        {
                            if (Conversions.TryChangeType<Guid>(subKey.GetValue("ProtocolId"), out var guid) && guid != Guid.Empty)
                            {
                                dic[guid] = name;
                            }
                        }
                    }
                }
            }
            return dic;
        }
    }
}

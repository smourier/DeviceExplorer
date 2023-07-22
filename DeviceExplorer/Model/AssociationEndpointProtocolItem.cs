using System;
using System.Collections.Generic;
using DeviceExplorer.Utilities;
using Microsoft.Win32;

namespace DeviceExplorer.Model
{
    public class AssociationEndpointProtocolItem : TreeItem
    {
        // https://learn.microsoft.com/en-us/windows/uwp/devices-sensors/enumerate-devices-over-a-network#enumerating-devices-over-networked-or-wireless-protocols
        public static readonly Guid ProtocolID_UPNP = new("0e261de4-12f0-46e6-91ba-428607ccef64");
        public static readonly Guid ProtocolId_WSD = new("782232aa-a2f9-4993-971b-aedc551346b0");
        public static readonly Guid ProtocolID_WiFiDirect = new("0407d24e-53de-4c9a-9ba1-9ced54641188");
        public static readonly Guid ProtocolID_DNS_SD = new("4526e8c1-8aac-4153-9b16-55e86ada0e54");
        public static readonly Guid ProtocolId_POS = new("d4bf61b3-442e-4ada-882d-fa7B70c832d9");
        public static readonly Guid ProtocolId_NetworkPrinters = new("37aba761-2124-454c-8d82-c42962c2de2b");
        public static readonly Guid ProtocolId_WNC = new("4c1b1ef8-2f62-4b9f-9bc5-b21ab636138f");
        public static readonly Guid ProtocolId_WiGigDocks = new("a277f3a5-8764-4f88-8045-4c5e962640b1");
        public static readonly Guid ProtocolId_WifiprovisioningForHPPrinters = new("c85ef710-f344-4792-bb6d-85a4346f1e69");
        public static readonly Guid ProtocolId_Bluetooth = new("e0cbf06c-cd8b-4647-bb8a-263b43f0f974"); // GUID_DEVCLASS_BLUETOOTH
        public static readonly Guid ProtocolId_BluetoothLE = new("bb7bb05e-5972-42b5-94fc-76eaa7084d49");
        public static readonly Guid ProtocolId_NetworkCamera = new("b8238652-b500-41eb-b4f3-4234f7f5ae99"); // KSCATEGORY_NETWORK_CAMERA

        private static readonly Lazy<Dictionary<Guid, string>> _namesFromRegistry = new Lazy<Dictionary<Guid, string>>(GetNamesFromRegistry);

        public AssociationEndpointProtocolItem(AssociationEndpointManagerItem parent, Guid protocolId)
            : base(parent)
        {
            Id = protocolId;
            if (protocolId == ProtocolID_UPNP)
            {
                Name = "UPnP (including DIAL and DLNA)";
            }
            else if (protocolId == ProtocolId_WSD)
            {
                Name = "Web services on devices (WSD)";
            }
            else if (protocolId == ProtocolID_WiFiDirect)
            {
                Name = "Wi-Fi Direct";
            }
            else if (protocolId == ProtocolID_DNS_SD)
            {
                Name = "DNS service discovery (DNS-SD)";
            }
            else if (protocolId == ProtocolId_POS)
            {
                Name = "Point of service";
            }
            else if (protocolId == ProtocolId_NetworkPrinters)
            {
                Name = "Network printers (active directory printers)";
            }
            else if (protocolId == ProtocolId_WNC)
            {
                Name = "Windows connect now (WNC)";
            }
            else if (protocolId == ProtocolId_WiGigDocks)
            {
                Name = "WiGig docks";
            }
            else if (protocolId == ProtocolId_WifiprovisioningForHPPrinters)
            {
                Name = "Wi-Fi provisioning for HP printers";
            }
            else if (protocolId == ProtocolId_Bluetooth)
            {
                Name = "Bluetooth";
            }
            else if (protocolId == ProtocolId_BluetoothLE)
            {
                Name = "Bluetooth LE";
            }
            else if (protocolId == ProtocolId_NetworkCamera)
            {
                Name = "Network Camera";
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
            }
        }

        public Guid Id { get; set; }

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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DeviceExplorer.Utilities;
using Windows.Devices.Enumeration;

namespace DeviceExplorer.Model
{
    public class DeviceClassItem : TreeItem
    {
        private static readonly Lazy<IReadOnlyDictionary<Guid, string>> _guidsNames = new(GetGuidsNames);
        public static IReadOnlyDictionary<Guid, string> GuidsNames => _guidsNames.Value;

        private readonly Lazy<BitmapSource> _icon;
        private readonly SortableObservableCollection<ClassProperty> _properties = new()
        {
            SortingSelector = o => o.Name
        };

        public DeviceClassItem(DeviceManagerItem parent, Guid classGuid)
            : base(parent)
        {
            ClassGuid = classGuid;
            var sb = new StringBuilder(1024);
            var size = sb.Capacity;
            _ = SetupDiGetClassDescription(classGuid, sb, sb.Capacity, ref size);
            Name = sb.ToString();

            _icon = new Lazy<BitmapSource>(() =>
             {
                 if (!SetupDiLoadClassIcon(ClassGuid, out var icon, IntPtr.Zero))
                     return null;

                 using (var ico = Icon.FromHandle(icon))
                 {
                     var img = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                     DestroyIcon(icon);
                     return img;
                 }
             }, false);

            var prop = new ClassProperty(Name)
            {
                ClassGuid = classGuid.ToString("B")
            };

            if (GuidsNames.TryGetValue(classGuid, out var guidName))
            {
                prop.GuidName = guidName;
            }

            _properties.Add(prop);
        }

        private sealed class ClassProperty : Property
        {
            public ClassProperty(string name)
                : base(name)
            {
            }

            public string ClassGuid { get; set; }
            public string GuidName { get; set; }
        }

        public Guid ClassGuid { get; }
        public new DeviceManagerItem Parent => (DeviceManagerItem)base.Parent;
        public override ImageSource Image => _icon.Value;
        public override IEnumerable<Property> Properties => _properties;

        internal void AddDevice(DeviceInformation device, DeviceInformation deviceInterface)
        {
            var deviceItem = Children.Cast<DeviceItem>().FirstOrDefault(d => d.Device.Id == device.Id);
            if (deviceItem == null)
            {
                deviceItem = new DeviceItem(this, device);
                Children.Add(deviceItem);
            }

            deviceItem.AddDeviceInterface(deviceInterface);
        }

        internal static int ParseIconLocation(string location, out string path)
        {
            path = new string(location);
            var index = PathParseIconLocation(path);
            var pos = path.LastIndexOf('\0');
            if (pos >= 0)
            {
                path = path.Substring(0, pos);
            }
            return index;
        }

        internal static string NormalizeIconPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            if (path.Contains('%'))
                return Environment.ExpandEnvironmentVariables(path);

            return path;
        }

        internal static BitmapSource LoadIcon(string iconFilePath, int index, int size = 32)
        {
            var ext = Path.GetExtension(iconFilePath);
            if (ext.EqualsIgnoreCase(".ico"))
            {
                try
                {
                    using var icon = Icon.ExtractAssociatedIcon(iconFilePath);
                    return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                }
                catch
                {
                    return null;
                }
            }

            var h = LoadLibraryEx(NormalizeIconPath(iconFilePath), IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE | LOAD_LIBRARY_AS_IMAGE_RESOURCE);
            if (h == IntPtr.Zero)
                return null;

            var hicon = IntPtr.Zero;
            try
            {
                hicon = LoadImage(h, (IntPtr)(-index), IMAGE_ICON, size, size, 0);
                if (hicon == IntPtr.Zero)
                    return null;

                return Imaging.CreateBitmapSourceFromHIcon(hicon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                if (hicon != IntPtr.Zero)
                {
                    DestroyIcon(hicon);
                }
                FreeLibrary(h);
            }
        }

        private const int LOAD_LIBRARY_AS_DATAFILE = 0x2;
        private const int LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x20;
        private const int IMAGE_ICON = 1;

        [DllImport("setupapi", CharSet = CharSet.Unicode)]
        private static extern int SetupDiGetClassDescription([MarshalAs(UnmanagedType.LPStruct)] Guid ClassGuid, StringBuilder ClassDescription, int ClassDescriptionSize, ref int RequiredSize);

        [DllImport("setupapi")]
        private static extern bool SetupDiLoadClassIcon([MarshalAs(UnmanagedType.LPStruct)] Guid ClassGuid, out IntPtr LargeIcon, IntPtr MiniIconIndex);

        [DllImport("user32")]
        internal static extern bool DestroyIcon(IntPtr handle);

        [DllImport("shlwapi", CharSet = CharSet.Unicode)]
        private static extern int PathParseIconLocation(string pszIconFile);

        [DllImport("user32", CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadImage(IntPtr hInst, IntPtr name, int type, int cx, int cy, int fuLoad);

        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, int dwFlags);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private static IReadOnlyDictionary<Guid, string> GetGuidsNames()
        {
            var dic = new ConcurrentDictionary<Guid, string>();
            foreach (var field in typeof(DeviceClassItem).GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.FieldType == typeof(Guid) && f.Name.StartsWith("GUID_DEVCLASS_")))
            {
                var guid = (Guid)field.GetValue(null);
                dic[guid] = field.Name;
            }
            return dic;
        }

        public static readonly Guid GUID_DEVCLASS_1394 = new("6bdd1fc1-810f-11d0-bec7-08002be2092f");
        public static readonly Guid GUID_DEVCLASS_1394DEBUG = new("66f250d6-7801-4a64-b139-eea80a450b24");
        public static readonly Guid GUID_DEVCLASS_61883 = new("7ebefbc0-3200-11d2-b4c2-00a0c9697d07");
        public static readonly Guid GUID_DEVCLASS_ADAPTER = new("4d36e964-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_APMSUPPORT = new("d45b1c18-c8fa-11d1-9f77-0000f805f530");
        public static readonly Guid GUID_DEVCLASS_AVC = new("c06ff265-ae09-48f0-812c-16753d7cba83");
        public static readonly Guid GUID_DEVCLASS_BATTERY = new("72631e54-78a4-11d0-bcf7-00aa00b7b32a");
        public static readonly Guid GUID_DEVCLASS_BIOMETRIC = new("53d29ef7-377c-4d14-864b-eb3a85769359");
        public static readonly Guid GUID_DEVCLASS_BLUETOOTH = new("e0cbf06c-cd8b-4647-bb8a-263b43f0f974");
        public static readonly Guid GUID_DEVCLASS_CAMERA = new("ca3e7ab9-b4c3-4ae6-8251-579ef933890f");
        public static readonly Guid GUID_DEVCLASS_CDROM = new("4d36e965-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_COMPUTEACCELERATOR = new("f01a9d53-3ff6-48d2-9f97-c8a7004be10c");
        public static readonly Guid GUID_DEVCLASS_COMPUTER = new("4d36e966-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_DECODER = new("6bdd1fc2-810f-11d0-bec7-08002be2092f");
        public static readonly Guid GUID_DEVCLASS_DISKDRIVE = new("4d36e967-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_DISPLAY = new("4d36e968-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_DOT4 = new("48721b56-6795-11d2-b1a8-0080c72e74a2");
        public static readonly Guid GUID_DEVCLASS_DOT4PRINT = new("49ce6ac8-6f86-11d2-b1e5-0080c72e74a2");
        public static readonly Guid GUID_DEVCLASS_EHSTORAGESILO = new("9da2b80f-f89f-4a49-a5c2-511b085b9e8a");
        public static readonly Guid GUID_DEVCLASS_ENUM1394 = new("c459df55-db08-11d1-b009-00a0c9081ff6");
        public static readonly Guid GUID_DEVCLASS_EXTENSION = new("e2f84ce7-8efa-411c-aa69-97454ca4cb57");
        public static readonly Guid GUID_DEVCLASS_FDC = new("4d36e969-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_FIRMWARE = new("f2e7dd72-6468-4e36-b6f1-6488f42c1b52");
        public static readonly Guid GUID_DEVCLASS_FLOPPYDISK = new("4d36e980-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_GPS = new("6bdd1fc3-810f-11d0-bec7-08002be2092f");
        public static readonly Guid GUID_DEVCLASS_HDC = new("4d36e96a-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_HIDCLASS = new("745a17a0-74d3-11d0-b6fe-00a0c90f57da");
        public static readonly Guid GUID_DEVCLASS_HOLOGRAPHIC = new("d612553d-06b1-49ca-8938-e39ef80eb16f");
        public static readonly Guid GUID_DEVCLASS_IMAGE = new("6bdd1fc6-810f-11d0-bec7-08002be2092f");
        public static readonly Guid GUID_DEVCLASS_INFINIBAND = new("30ef7132-d858-4a0c-ac24-b9028a5cca3f");
        public static readonly Guid GUID_DEVCLASS_INFRARED = new("6bdd1fc5-810f-11d0-bec7-08002be2092f");
        public static readonly Guid GUID_DEVCLASS_KEYBOARD = new("4d36e96b-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_LEGACYDRIVER = new("8ecc055d-047f-11d1-a537-0000f8753ed1");
        public static readonly Guid GUID_DEVCLASS_MEDIA = new("4d36e96c-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_MEDIUM_CHANGER = new("ce5939ae-ebde-11d0-b181-0000f8753ec4");
        public static readonly Guid GUID_DEVCLASS_MEMORY = new("5099944a-f6b9-4057-a056-8c550228544c");
        public static readonly Guid GUID_DEVCLASS_MODEM = new("4d36e96d-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_MONITOR = new("4d36e96e-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_MOUSE = new("4d36e96f-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_MTD = new("4d36e970-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_MULTIFUNCTION = new("4d36e971-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_MULTIPORTSERIAL = new("50906cb8-ba12-11d1-bf5d-0000f805f530");
        public static readonly Guid GUID_DEVCLASS_NET = new("4d36e972-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_NETCLIENT = new("4d36e973-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_NETDRIVER = new("87ef9ad1-8f70-49ee-b215-ab1fcadcbe3c");
        public static readonly Guid GUID_DEVCLASS_NETSERVICE = new("4d36e974-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_NETTRANS = new("4d36e975-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_NETUIO = new("78912bc1-cb8e-4b28-a329-f322ebadbe0f");
        public static readonly Guid GUID_DEVCLASS_NODRIVER = new("4d36e976-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_PCMCIA = new("4d36e977-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_PNPPRINTERS = new("4658ee7e-f050-11d1-b6bd-00c04fa372a7");
        public static readonly Guid GUID_DEVCLASS_PORTS = new("4d36e978-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_PRINTER = new("4d36e979-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_PRINTERUPGRADE = new("4d36e97a-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_PRINTQUEUE = new("1ed2bbf9-11f0-4084-b21f-ad83a8e6dcdc");
        public static readonly Guid GUID_DEVCLASS_PROCESSOR = new("50127dc3-0f36-415e-a6cc-4cb3be910b65");
        public static readonly Guid GUID_DEVCLASS_SBP2 = new("d48179be-ec20-11d1-b6b8-00c04fa372a7");
        public static readonly Guid GUID_DEVCLASS_SCMDISK = new("53966cb1-4d46-4166-bf23-c522403cd495");
        public static readonly Guid GUID_DEVCLASS_SCMVOLUME = new("53ccb149-e543-4c84-b6e0-bce4f6b7e806");
        public static readonly Guid GUID_DEVCLASS_SCSIADAPTER = new("4d36e97b-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_SECURITYACCELERATOR = new("268c95a1-edfe-11d3-95c3-0010dc4050a5");
        public static readonly Guid GUID_DEVCLASS_SENSOR = new("5175d334-c371-4806-b3ba-71fd53c9258d");
        public static readonly Guid GUID_DEVCLASS_SIDESHOW = new("997b5d8d-c442-4f2e-baf3-9c8e671e9e21");
        public static readonly Guid GUID_DEVCLASS_SMARTCARDREADER = new("50dd5230-ba8a-11d1-bf5d-0000f805f530");
        public static readonly Guid GUID_DEVCLASS_SMRDISK = new("53487c23-680f-4585-acc3-1f10d6777e82");
        public static readonly Guid GUID_DEVCLASS_SMRVOLUME = new("53b3cf03-8f5a-4788-91b6-d19ed9fcccbf");
        public static readonly Guid GUID_DEVCLASS_SOFTWARECOMPONENT = new("5c4c3332-344d-483c-8739-259e934c9cc8");
        public static readonly Guid GUID_DEVCLASS_SOUND = new("4d36e97c-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_SYSTEM = new("4d36e97d-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_TAPEDRIVE = new("6d807884-7d21-11cf-801c-08002be10318");
        public static readonly Guid GUID_DEVCLASS_UNKNOWN = new("4d36e97e-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVCLASS_UCM = new("e6f1aa1c-7f3b-4473-b2e8-c97d8ac71d53");
        public static readonly Guid GUID_DEVCLASS_USB = new("36fc9e60-c465-11cf-8056-444553540000");
        public static readonly Guid GUID_DEVCLASS_VOLUME = new("71a27cdd-812a-11d0-bec7-08002be2092f");
        public static readonly Guid GUID_DEVCLASS_VOLUMESNAPSHOT = new("533c5b84-ec70-11d2-9505-00c04f79deaf");
        public static readonly Guid GUID_DEVCLASS_WCEUSBS = new("25dbce51-6c8f-4a72-8a6d-b54c2b4fc835");
        public static readonly Guid GUID_DEVCLASS_WPD = new("eec5ad98-8080-425f-922a-dabf3de3f69a");

        public static readonly Guid GUID_DEVINTERFACE_A2DP_SIDEBAND_AUDIO = new("f3b1362f-c9f4-4dd1-9d55-e02038a129fb");
        public static readonly Guid GUID_DEVINTERFACE_ASP_INFRA_DEVICE = new("ff823995-7a72-4c80-8757-c67ee13d1a49");
        public static readonly Guid GUID_DEVINTERFACE_BIOMETRIC_READER = new("e2b5183a-99ea-4cc3-ad6b-80ca8d715b80");
        public static readonly Guid GUID_DEVINTERFACE_BLUETOOTH_HFP_SCO_HCIBYPASS = new("be446647-f655-4919-8bd0-125ba5d4ce65");
        public static readonly Guid GUID_DEVINTERFACE_BRIGHTNESS = new("fde5bba4-b3f9-46fb-bdaa-0728ce3100b4");
        public static readonly Guid GUID_DEVINTERFACE_BRIGHTNESS_2 = new("148a3c98-0ecd-465a-b634-b05f195f7739");
        public static readonly Guid GUID_DEVINTERFACE_BRIGHTNESS_3 = new("197a4a6e-0391-4322-96ea-c2760f881d3a");
        public static readonly Guid GUID_DEVINTERFACE_CDCHANGER = new("53f56312-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_CDROM = new("53f56308-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_CHARGING_ARBITRATION = new("ec0a1cc9-4294-43fb-bf37-b850ce95f337");
        public static readonly Guid GUID_DEVINTERFACE_COMPORT = new("86e0d1e0-8089-11d0-9ce4-08003e301f73");
        public static readonly Guid GUID_DEVINTERFACE_CONFIGURABLE_USBFN_CHARGER = new("7158c35c-c1bc-4d90-acb1-8020bd0e19ca");
        public static readonly Guid GUID_DEVINTERFACE_CONFIGURABLE_WIRELESS_CHARGER = new("3612b1c8-3633-47d3-8af5-00a4dfa04793");
        public static readonly Guid GUID_DEVINTERFACE_DIRECTLY_ASSIGNABLE_DEVICE = new("0db3e0f9-3536-4213-9572-ad77e224be27");
        public static readonly Guid GUID_DEVINTERFACE_DISK = new("53f56307-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_DISPLAY_ADAPTER = new("5b45201d-f2f2-4f3b-85bb-30ff1f953599");
        public static readonly Guid GUID_DEVINTERFACE_DMP = new("25b4e268-2a05-496e-803b-266837fbda4b");
        public static readonly Guid GUID_DEVINTERFACE_DMR = new("d0875fb4-2196-4c7a-a63d-e416addd60a1");
        public static readonly Guid GUID_DEVINTERFACE_DMS = new("c96037ae-a558-4470-b432-115a31b85553");
        public static readonly Guid GUID_DEVINTERFACE_EMMC_PARTITION_ACCESS_GPP = new("2e0e2e39-1f19-4595-a906-887882e73903");
        public static readonly Guid GUID_DEVINTERFACE_EMMC_PARTITION_ACCESS_RPMB = new("27447c21-bcc3-4d07-a05b-a3395bb4eee7");
        public static readonly Guid GUID_DEVINTERFACE_ENHANCED_STORAGE_SILO = new("3897f6a4-fd35-4bc8-a0b7-5dbba36adafa");
        public static readonly Guid GUID_DEVINTERFACE_FLOPPY = new("53f56311-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_GNSS = new("3336e5e4-018a-4669-84c5-bd05f3bd368b");
        public static readonly Guid GUID_DEVINTERFACE_GRAPHICSPOWER = new("ea5c6870-e93c-4588-bef1-fec42fc9429a");
        public static readonly Guid GUID_DEVINTERFACE_HID = new("4d1e55b2-f16f-11cf-88cb-001111000030");
        public static readonly Guid GUID_DEVINTERFACE_HIDDEN_VOLUME = new("7f108a28-9833-4b3b-b780-2c6b5fa5c062");
        public static readonly Guid GUID_DEVINTERFACE_HOLOGRAPHIC_DISPLAY = new("deac60ab-66e2-42a4-ad9b-557ee33ae2d5");
        public static readonly Guid GUID_DEVINTERFACE_HPMI = new("dedae202-1d20-4c40-a6f3-1897e319d54f");
        public static readonly Guid GUID_DEVINTERFACE_I2C = new("2564aa4f-dddb-4495-b497-6ad4a84163d7");
        public static readonly Guid GUID_DEVINTERFACE_IMAGE = new("6bdd1fc6-810f-11d0-bec7-08002be2092f");
        public static readonly Guid GUID_DEVINTERFACE_IPPUSB_PRINT = new("f2f40381-f46d-4e51-bce7-62de6cf2d098");
        public static readonly Guid GUID_DEVINTERFACE_KEYBOARD = new("884b96c3-56ef-11d1-bc8c-00a0c91405dd");
        public static readonly Guid GUID_DEVINTERFACE_LAMP = new("6c11e9e3-8238-4f0a-0a19-aaec26ca5e98");
        public static readonly Guid GUID_DEVINTERFACE_MEDIUMCHANGER = new("53f56310-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_MIRACAST_DISPLAY = new("af03f190-22af-48cb-94bb-b78e76a25107");
        public static readonly Guid GUID_DEVINTERFACE_MIRACAST_DISPLAY_ARRIVAL = new("64f1f453-d465-4097-b8f8-cdff171fc335");
        public static readonly Guid GUID_DEVINTERFACE_MODEM = new("2c7089aa-2e0e-11d1-b114-00c04fc2aae4");
        public static readonly Guid GUID_DEVINTERFACE_MONITOR = new("e6f07b5f-ee97-4a90-b076-33f57bf4eaa7");
        public static readonly Guid GUID_DEVINTERFACE_MOUSE = new("378de44c-56ef-11d1-bc8c-00a0c91405dd");
        public static readonly Guid GUID_DEVINTERFACE_NET = new("cac88484-7515-4c03-82e6-71a87abac361");
        public static readonly Guid GUID_DEVINTERFACE_NETUIO = new("08336f60-0679-4c6c-85d2-ae7ced65fff7");
        public static readonly Guid GUID_DEVINTERFACE_NFCDTA = new("7fd3f30b-5e49-4be1-b3aa-af06260d236a");
        public static readonly Guid GUID_DEVINTERFACE_NFCSE = new("8dc7c854-f5e5-4bed-815d-0c85ad047725");
        public static readonly Guid GUID_DEVINTERFACE_NFP = new("fb3842cd-9e2a-4f83-8fcc-4b0761139ae9");
        public static readonly Guid GUID_DEVINTERFACE_OPM = new("bf4672de-6b4e-4be4-a325-68a91ea49c09");
        public static readonly Guid GUID_DEVINTERFACE_OPM_2 = new("7f098726-2ebb-4ff3-a27f-1046b95dc517");
        public static readonly Guid GUID_DEVINTERFACE_OPM_2_JTP = new("e929eea4-b9f1-407b-aab9-ab08bb44fbf4");
        public static readonly Guid GUID_DEVINTERFACE_OPM_3 = new("693a2cb1-8c8d-4ab6-9555-4b85ef2c7c6b");
        public static readonly Guid GUID_DEVINTERFACE_PARALLEL = new("97f76ef0-f883-11d0-af1f-0000f800845c");
        public static readonly Guid GUID_DEVINTERFACE_PARCLASS = new("811fc6a5-f728-11d0-a537-0000f8753ed1");
        public static readonly Guid GUID_DEVINTERFACE_PARTITION = new("53f5630a-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_POS_CASHDRAWER = new("772e18f2-8925-4229-a5ac-6453cb482fda");
        public static readonly Guid GUID_DEVINTERFACE_POS_LINEDISPLAY = new("4fc9541c-0fe6-4480-a4f6-9495a0d17cd2");
        public static readonly Guid GUID_DEVINTERFACE_POS_MSR = new("2a9fe532-0cdc-44f9-9827-76192f2ca2fb");
        public static readonly Guid GUID_DEVINTERFACE_POS_PRINTER = new("c7bc9b22-21f0-4f0d-9bb6-66c229b8cd33");
        public static readonly Guid GUID_DEVINTERFACE_POS_SCANNER = new("c243ffbd-3afc-45e9-b3d3-2ba18bc7ebc5");
        public static readonly Guid GUID_DEVINTERFACE_PWM_CONTROLLER = new("60824b4c-eed1-4c9c-b49c-1b961461a819");
        public static readonly Guid GUID_DEVINTERFACE_PWM_CONTROLLER_WSZ = new("{60824B4C-EED1-4C9C-B49C-1B961461A819}");
        public static readonly Guid GUID_DEVINTERFACE_SCM_PHYSICAL_DEVICE = new("4283609d-4dc2-43be-bbb4-4f15dfce2c61");
        public static readonly Guid GUID_DEVINTERFACE_SENSOR = new("ba1bb692-9b7a-4833-9a1e-525ed134e7e2");
        public static readonly Guid GUID_DEVINTERFACE_SERENUM_BUS_ENUMERATOR = new("4d36e978-e325-11ce-bfc1-08002be10318");
        public static readonly Guid GUID_DEVINTERFACE_SERVICE_VOLUME = new("6ead3d82-25ec-46bc-b7fd-c1f0df8f5037");
        public static readonly Guid GUID_DEVINTERFACE_SES = new("1790c9ec-47d5-4df3-b5af-9adf3cf23e48");
        public static readonly Guid GUID_DEVINTERFACE_SIDESHOW = new("152e5811-feb9-4b00-90f4-d32947ae1681");
        public static readonly Guid GUID_DEVINTERFACE_SMARTCARD_READER = new("50dd5230-ba8a-11d1-bf5d-0000f805f530");
        public static readonly Guid GUID_DEVINTERFACE_STORAGEPORT = new("2accfe60-c130-11d2-b082-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_SURFACE_VIRTUAL_DRIVE = new("2e34d650-5819-42ca-84ae-d30803bae505");
        public static readonly Guid GUID_DEVINTERFACE_TAPE = new("53f5630b-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_THERMAL_COOLING = new("dbe4373d-3c81-40cb-ace4-e0e5d05f0c9f");
        public static readonly Guid GUID_DEVINTERFACE_THERMAL_MANAGER = new("927ec093-69a4-4bc0-bd02-711664714463");
        public static readonly Guid GUID_DEVINTERFACE_UNIFIED_ACCESS_RPMB = new("27447c21-bcc3-4d07-a05b-a3395bb4eee7");
        public static readonly Guid GUID_DEVINTERFACE_USB_BILLBOARD = new("5e9adaef-f879-473f-b807-4e5ea77d1b1c");
        public static readonly Guid GUID_DEVINTERFACE_USB_DEVICE = new("a5dcbf10-6530-11d2-901f-00c04fb951ed");
        public static readonly Guid GUID_DEVINTERFACE_USB_HOST_CONTROLLER = new("3abf6f2d-71c4-462a-8a92-1e6861e6af27");
        public static readonly Guid GUID_DEVINTERFACE_USB_HUB = new("f18a0e88-c30c-11d0-8815-00a0c906bed8");
        public static readonly Guid GUID_DEVINTERFACE_USB_SIDEBAND_AUDIO_HS_HCIBYPASS = new("02baa4b5-33b5-4d97-ae4f-e86dde17536f");
        public static readonly Guid GUID_DEVINTERFACE_USBPRINT = new("28d78fad-5a12-11d1-ae5b-0000f803a8c2");
        public static readonly Guid GUID_DEVINTERFACE_VIDEO_OUTPUT_ARRIVAL = new("1ad9e4f0-f88d-4360-bab9-4c2d55e564cd");
        public static readonly Guid GUID_DEVINTERFACE_VIRTUALIZABLE_DEVICE = new("a13a7a93-11f0-4bd2-a9f5-6b5c5b88527d");
        public static readonly Guid GUID_DEVINTERFACE_VM_GENCOUNTER = new("3ff2c92b-6598-4e60-8e1c-0ccf4927e319");
        public static readonly Guid GUID_DEVINTERFACE_VMLUN = new("6f416619-9f29-42a5-b20b-37e219ca02b0");
        public static readonly Guid GUID_DEVINTERFACE_VOLUME = new("53f5630d-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_VPCI = new("57863182-c948-4692-97e3-34b57662a3e0");
        public static readonly Guid GUID_DEVINTERFACE_WDDM3_ON_VB = new("e922004d-eb9c-4de1-9224-a9ceaa959bce");
        public static readonly Guid GUID_DEVINTERFACE_WIFIDIRECT_DEVICE = new("439b20af-8955-405b-99f0-a62af0c68d43");
        public static readonly Guid GUID_DEVINTERFACE_WPD = new("6ac27878-a6fa-4155-ba85-f98f491d4f33");
        public static readonly Guid GUID_DEVINTERFACE_WPD_DRIVER_PREPARED = new("10497b1b-ba51-44e5-8318-a65c837b6661");
        public static readonly Guid GUID_DEVINTERFACE_WPD_PRIVATE = new("ba0c718f-4ded-49b7-bdd3-fabe28661211");
        public static readonly Guid GUID_DEVINTERFACE_WPD_SERVICE = new("9ef44f80-3d64-4246-a6aa-206f328d1edc");
        public static readonly Guid GUID_DEVINTERFACE_WRITEONCEDISK = new("53f5630c-b6bf-11d0-94f2-00a0c91efb8b");
        public static readonly Guid GUID_DEVINTERFACE_WWAN_CONTROLLER = new("669159fd-e3c0-45cb-bc5f-95995bcd06cd");
        public static readonly Guid GUID_DEVINTERFACE_ZNSDISK = new("b87941c5-ffdb-43c7-b6b1-20b632f0b109");
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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

            _icon = new Lazy<BitmapSource>(() => GetClassIcon(ClassGuid), false);
            var prop = new ClassProperty(Name)
            {
                ClassGuid = classGuid.ToString("B")
            };

            if (Guids.Names.TryGetValue(classGuid, out var guidName))
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

        internal static BitmapSource GetClassIcon(Guid classGuid)
        {
            if (!SetupDiLoadClassIcon(classGuid, out var icon, IntPtr.Zero))
                return null;

            using var ico = Icon.FromHandle(icon);
            var img = Imaging.CreateBitmapSourceFromHIcon(ico.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DestroyIcon(icon);
            return img;
        }

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
    }
}

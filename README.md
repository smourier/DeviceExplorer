# DeviceExplorer
A tool that displays the hardware attached to a Windows computer, similar to Windows' Device Manager:

![Device Explorer](Images/DeviceExplorer.png?raw=true)

It also shows Association EndPoints dynamically (bluetooth endpoints,  UPnP, etc.):

![Device Explorer BLE](Images/DeviceExplorerBLE.png?raw=true)

It also shows Bluetooth LE advertisements dynamically:

![Device Explorer BLE Adds](Images/DeviceExplorerBLEAdds.png?raw=true)

Written with C# WPF and .NET 6, using WinRT's `Windows.Devices.Enumeration` Namespace.

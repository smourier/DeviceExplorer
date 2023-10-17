using System.Linq;
using DeviceExplorer.Utilities;
using Windows.Devices.Bluetooth.Advertisement;

namespace DeviceExplorer.Model
{
    public class BluetoothLEAdvertisementManager
    {
        // https://www.bluetooth.com/specifications/assigned-numbers/
        public BluetoothLEAdvertisementManager(AssociationEndpointManagerItem endpointManager)
        {
            EndPointManager = endpointManager;
            Watcher = new BluetoothLEAdvertisementWatcher();
            Watcher.Received += OnWatcherReceived;
            try
            {
                Watcher.Start();
            }
            catch
            {
                // don't care, for some reason it's not supported
            }
        }

        public AssociationEndpointManagerItem EndPointManager { get; }
        public BluetoothLEAdvertisementWatcher Watcher { get; }

        private void OnWatcherReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            App.Current?.Dispatcher?.Invoke(() =>
            {
                var address = Bluetooth.GetMacAddress(args.BluetoothAddress);
                var key = BluetoothLEAdvertisementItem.GetKey(args);

                var protocolItem = EndPointManager.Children.OfType<AssociationEndpointProtocolItem>().FirstOrDefault(i => i.Id == Guids.ProtocolId_BluetoothLE);
                if (protocolItem == null)
                    return;

                var endpointItem = protocolItem.Children.OfType<AssociationEndpointItem>().FirstOrDefault(ep => ep.Address.EqualsIgnoreCase(address));
                if (endpointItem == null)
                    return;

                var adsItem = endpointItem.Children.OfType<BluetoothLEAdvertisementsItem>().FirstOrDefault();
                if (adsItem == null)
                {
                    adsItem = new BluetoothLEAdvertisementsItem(endpointItem);
                    endpointItem.Children.Add(adsItem);
                }

                var adItem = adsItem.Children.OfType<BluetoothLEAdvertisementItem>().FirstOrDefault(ad => ad.Key.EqualsIgnoreCase(key));
                if (adItem == null)
                {
                    adItem = new BluetoothLEAdvertisementItem(adsItem, args, key);
                    adsItem.Children.Add(adItem);
                }
                adItem.Update(args);
            });
        }
    }
}

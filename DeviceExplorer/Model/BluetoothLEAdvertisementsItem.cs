namespace DeviceExplorer.Model
{
    public class BluetoothLEAdvertisementsItem : TreeItem
    {
        public BluetoothLEAdvertisementsItem(AssociationEndpointItem endpoint)
            : base(endpoint)
        {
            Name = "Advertisements";
            Image = DeviceClassItem.LoadIcon(@"%windir%\system32\shell32.dll", -4);
        }
    }
}

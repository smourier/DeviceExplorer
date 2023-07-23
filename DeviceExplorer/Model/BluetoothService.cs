using System;

namespace DeviceExplorer.Model
{
    public class BluetoothService
    {
        private ushort _uuid;
        private Guid _guid;

        public virtual string Name { get; set; }
        public virtual string Id { get; set; }

        public virtual ushort Uuid
        {
            get => _uuid;
            set
            {
                if (_uuid == value)
                    return;

                _uuid = value;
                Guid = new Guid("0000" + _uuid.ToString("X4") + "-0000-1000-8000-00805f9b34fb");
            }
        }

        public virtual Guid Guid
        {
            get => _guid;
            set
            {
                if (_guid == value)
                    return;

                _guid = value;
            }
        }

        public override string ToString() => Uuid + ":" + Name;
    }
}

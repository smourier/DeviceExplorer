using System;
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

                GuidName = Guids.GetName(_value);
            }
        }

        public string GuidName { get; private set; }

        public override string ToString() => Name + "\t" + Value;
    }
}

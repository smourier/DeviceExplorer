using System;

namespace DeviceExplorer.Model
{
    public abstract class Property : IComparable, IComparable<Property>
    {
        protected Property(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Name = name;
        }

        public string Name { get; }

        public override string ToString() => Name;

        int IComparable.CompareTo(object obj) => CompareTo(obj as Property);
        public int CompareTo(Property other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            return Name.CompareTo(other.Name);
        }
    }
}

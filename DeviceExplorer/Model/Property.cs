using System;

namespace DeviceExplorer.Model
{
    public class Property : IComparable, IComparable<Property>
    {
        public Property(string name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

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

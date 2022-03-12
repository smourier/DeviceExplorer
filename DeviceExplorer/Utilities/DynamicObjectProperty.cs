using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DeviceExplorer.Utilities
{
    public class DynamicObjectProperty : PropertyDescriptor
    {
        private Type _type;
        private bool _isReadOnly;
        private object _defaultValue;

        public DynamicObjectProperty(PropertyDescriptor descriptor)
            : base(descriptor)
        {
            var atts = new List<Attribute>();
            foreach (Attribute att in descriptor.Attributes)
            {
                atts.Add(att);
            }
            Construct(descriptor.Name, descriptor.PropertyType, atts);
        }

        public DynamicObjectProperty(string name, Type type, IEnumerable<Attribute> attributes)
            : base(name, GetAttributes(attributes))
        {
            Construct(name, type, attributes);
        }

        public virtual object DefaultValue { get => _defaultValue; set => _defaultValue = Conversions.ChangeType(value, _type, defaultValue: null); }
        public int SortOrder { get; set; }
        public virtual bool HasDefaultValue { get; set; }
        public override bool IsReadOnly => _isReadOnly;
        public override Type PropertyType => _type;
        public override Type ComponentType => typeof(DynamicObject);

        protected virtual void Construct(string name, Type type, IEnumerable<Attribute> attributes)
        {
            _type = type;

            var ro = Attributes.GetAttribute<ReadOnlyAttribute>();
            if (ro != null)
            {
                _isReadOnly = ro.IsReadOnly;
            }

            var dv = Attributes.GetAttribute<DefaultValueAttribute>();
            if (dv != null)
            {
                HasDefaultValue = true;
                _defaultValue = Conversions.ChangeType(dv.Value, _type, defaultValue: null);
            }
            else
            {
                _defaultValue = Conversions.ChangeType(null, _type, defaultValue: null);
            }
        }

        private static Attribute[] GetAttributes(IEnumerable<Attribute> attributes)
        {
            var list = attributes == null ? new List<Attribute>() : new List<Attribute>(attributes);
            return list.ToArray();
        }

        public override object GetValue(object component)
        {
            if (component is DynamicObject obj)
                return obj.GetPropertyValue(Name, _defaultValue);

            throw new ArgumentException(null, nameof(component));
        }

        public override string ToString() => Name + " (" + _type.FullName + ")";
        public override bool ShouldSerializeValue(object component) => false;
        public override bool CanResetValue(object component) => HasDefaultValue;
        public override void ResetValue(object component)
        {
            if (HasDefaultValue)
            {
                SetValue(component, _defaultValue);
            }
        }

        public override void SetValue(object component, object value)
        {
            if (component is DynamicObject obj)
            {
                obj.SetPropertyValue(Name, value);
                return;
            }

            throw new ArgumentException(null, nameof(component));
        }
    }
}
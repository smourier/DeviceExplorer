using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace DeviceExplorer.Utilities
{
    public class PropertyGridProperty : DictionaryObject, IComparable, IComparable<PropertyGridProperty>
    {
        public event EventHandler<PropertyGridEventArgs> Event;
        private string _defaultEditorResourceKey;
        private object _clonedValue;
        private bool _valueCloned;

        public PropertyGridProperty(PropertyGridDataProvider dataProvider)
        {
            if (dataProvider == null)
                throw new ArgumentNullException(nameof(dataProvider));

            DataProvider = dataProvider;
            PropertyType = typeof(object);
            Attributes = dataProvider.CreateDynamicObject();
            TypeAttributes = dataProvider.CreateDynamicObject();
        }

        public override string ToString() => Name;

        public virtual void CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (Value is IPropertyGridCommandHandler handler)
            {
                handler.CanExecute(this, sender, e);
            }
        }

        public virtual void Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (Value is IPropertyGridCommandHandler handler)
            {
                handler.Executed(this, sender, e);
            }
        }

        public virtual void OnEvent(object sender, PropertyGridEventArgs e) => Event?.Invoke(sender, e);

        public void UpdateCellBindings(Action<BindingExpression> action) => UpdateCellBindings(null, null, action);
        public void UpdateCellBindings(string childName, Action<BindingExpression> action) => UpdateCellBindings(childName, null, action);
        public virtual void UpdateCellBindings(string childName, Func<Binding, bool> where, Action<BindingExpression> action) => DataProvider.Grid.UpdateCellBindings(this, childName, where, action);

        public static bool IsEnumOrNullableEnum(Type type, out Type enumType, out bool nullable)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            nullable = false;
            if (type.IsEnum)
            {
                enumType = type;
                return true;
            }

            if (type.Name == typeof(Nullable<>).Name)
            {
                var args = type.GetGenericArguments();
                if (args.Length == 1 && args[0].IsEnum)
                {
                    enumType = args[0];
                    nullable = true;
                    return true;
                }
            }

            enumType = null;
            return false;
        }

        public static PropertyGridProperty FromEvent(RoutedEventArgs e)
        {
            if (e == null)
                return null;

            if (e.OriginalSource is not FrameworkElement fe)
                return null;

            return fe.DataContext as PropertyGridProperty;
        }

        public PropertyGridDataProvider DataProvider { get; private set; }
        public bool IsValid => !DictionaryObjectHasErrors;
        public virtual int SortOrder { get; set; }
        public virtual DynamicObject Attributes { get; private set; }
        public virtual DynamicObject TypeAttributes { get; private set; }
        public virtual PropertyGridOptionsAttribute Options { get; set; }
        public virtual object Tag { get; set; }

        public virtual Type PropertyType { get => DictionaryObjectGetPropertyValue<Type>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual string Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool IsError { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool IsEnum { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool IsFlagsEnum { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual string Category { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual string DisplayName { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual string Description { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool HasDefaultValue { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual PropertyDescriptor Descriptor { get => DictionaryObjectGetPropertyValue<PropertyDescriptor>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual TypeConverter Converter { get => DictionaryObjectGetPropertyValue<TypeConverter>(); set => DictionaryObjectSetPropertyValue(value); }

        public virtual object DefaultValue
        {
            get => DictionaryObjectGetPropertyValue<object>();
            set
            {
                if (DictionaryObjectSetPropertyValue(value))
                {
                    OnPropertyChanged(nameof(BooleanValue));
                    OnPropertyChanged(nameof(Value));
                    OnPropertyChanged(nameof(IsDefaultValue));
                }
            }
        }

        public virtual string DefaultEditorResourceKey
        {
            get
            {
                if (_defaultEditorResourceKey != null)
                    return _defaultEditorResourceKey;

                if (IsCollection)
                    return "CollectionEditorWindow";

                return "ObjectEditorWindow";
            }
            set => _defaultEditorResourceKey = value;
        }

        public virtual Type CollectionItemPropertyType
        {
            get
            {
                if (!IsCollection)
                    return null;

                return Extensions.GetElementType(PropertyType);
            }
        }

        public virtual bool IsDefaultValue
        {
            get
            {
                if (!HasDefaultValue)
                    return false;

                if (DefaultValue == null)
                    return Value == null;

                return DefaultValue.Equals(Value);
            }
        }

        public virtual bool IsReadOnly
        {
            get
            {
                var def = false;
                if (DataProvider != null && DataProvider.Grid != null && DataProvider.Grid.IsReadOnly)
                {
                    def = true;
                }

                return DictionaryObjectGetPropertyValue(def);
            }
            set
            {
                if (DictionaryObjectSetPropertyValue(value))
                {
                    OnPropertyChanged(nameof(IsReadWrite));
                }
            }
        }

        public bool IsReadWrite { get => !IsReadOnly; set => IsReadOnly = !value; }
        public virtual bool IsCollectionItemValueType => CollectionItemPropertyType != null && CollectionItemPropertyType.IsValueType;
        public virtual bool IsValueType => PropertyType != null && PropertyType.IsValueType;

        public virtual int CollectionCount
        {
            get
            {
                if (Value is IEnumerable enumerable)
                    return enumerable.Cast<object>().Count();

                return 0;
            }
        }

        public virtual bool IsCollection
        {
            get
            {
                if (PropertyType == null)
                    return false;

                if (PropertyType == typeof(string))
                    return false;

                return typeof(IEnumerable).IsAssignableFrom(PropertyType);
            }
        }
        
        public bool? BooleanValue
        {
            get
            {
                if (Value == null)
                    return null;

                var def = HasDefaultValue && Conversions.ChangeType(DefaultValue, false);
                return Conversions.ChangeType(Value, def);
            }
            set
            {
                if (value == null)
                {
                    Value = null;
                    return;
                }

                Value = value.Value;
            }
        }

        public virtual string TextValue
        {
            get
            {
                if (Converter != null && Converter.CanConvertTo(typeof(string)))
                    return (string)Converter.ConvertTo(Value, typeof(string));

                return Conversions.ChangeType<string>(Value);
            }
            set
            {
                if (Converter != null)
                {
                    if (Converter.CanConvertFrom(typeof(string)))
                    {
                        Value = Converter.ConvertFrom(value);
                        return;
                    }

                    if (Descriptor != null && Converter.CanConvertTo(Descriptor.PropertyType))
                    {
                        Value = Converter.ConvertTo(value, Descriptor.PropertyType);
                        return;
                    }
                }

                if (Descriptor != null)
                {
                    if (Conversions.TryChangeType(value, Descriptor.PropertyType, out var v))
                    {
                        Value = v;
                        return;
                    }
                }
                Value = value;
            }
        }

        public virtual void OnValueChanged()
        {
            OnPropertyChanged(nameof(TextValue));
            OnPropertyChanged(nameof(BooleanValue));
            OnPropertyChanged(nameof(IsCollection));
            OnPropertyChanged(nameof(CollectionCount));
            OnPropertyChanged(nameof(IsDefaultValue));
        }

        public virtual void SetValue(object value, DictionaryObjectPropertySetOptions options)
        {
            var set = DictionaryObjectSetPropertyValue(value, options, nameof(Value));
            if (set || options.HasFlag(DictionaryObjectPropertySetOptions.ForceRaiseOnPropertyChanged))
            {
                OnValueChanged();
            }
        }

        public void ResetClonedValue()
        {
            _valueCloned = false;
        }

        public virtual void CloneValue(bool refresh)
        {
            if (_valueCloned && !refresh)
                return;

            _clonedValue = Value is ICloneable c ? c.Clone() : Value;
            _valueCloned = true;
        }

        public virtual object ClonedValue
        {
            get
            {
                CloneValue(false);
                return _clonedValue;
            }
        }

        public virtual object Value
        {
            get => DictionaryObjectGetPropertyValue(DefaultValue);
            set
            {
                if (!TryChangeType(value, PropertyType, CultureInfo.CurrentCulture, out var changedValue))
                    throw new ArgumentException("Cannot convert value {" + value + "} to type '" + PropertyType.FullName + "'.");

                if (Descriptor != null)
                {
                    try
                    {
                        Descriptor.SetValue(DataProvider.Data, changedValue);
                        var finalValue = Descriptor.GetValue(DataProvider.Data);
                        SetValue(finalValue, DictionaryObjectPropertySetOptions.TrackChanges);
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Cannot set value {" + value + "} to object.", e);
                    }
                }
            }
        }

        protected virtual bool TryChangeType(object value, Type type, IFormatProvider provider, out object changedValue)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return Conversions.TryChangeType(value, type, provider, out changedValue);
        }

        public virtual void RaiseOnPropertyChanged(string name) => OnPropertyChanged(name);

        public virtual void OnDescribed()
        {
        }

        public virtual void RefreshValueFromDescriptor(DictionaryObjectPropertySetOptions options)
        {
            if (Descriptor == null)
                return;

            try
            {
                var value = Descriptor.GetValue(DataProvider.Data);
                SetValue(value, options);
            }
            catch (Exception e)
            {
                if (PropertyType == typeof(string))
                {
                    Value = e.GetAllMessages();
                }
                IsError = true;
            }
        }

        int IComparable.CompareTo(object obj) => CompareTo(obj as PropertyGridProperty);
        public virtual int CompareTo(PropertyGridProperty other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (SortOrder != 0)
                return SortOrder.CompareTo(other.SortOrder);

            if (other.SortOrder != 0)
                return -other.SortOrder.CompareTo(0);

            if (DisplayName == null)
                return 1;

            return string.Compare(DisplayName, other.DisplayName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
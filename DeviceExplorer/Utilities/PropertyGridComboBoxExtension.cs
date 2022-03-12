using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace DeviceExplorer.Utilities
{
    public class PropertyGridComboBoxExtension : MarkupExtension
    {
        private readonly Binding _binding;

        public PropertyGridComboBoxExtension(Binding binding)
        {
            _binding = binding; // may be null
            DefaultZeroName = "None";
        }

        public string DefaultZeroName { get; set; }

        public virtual PropertyGridItem CreateItem() => Activator.CreateInstance<PropertyGridItem>();

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (_binding == null)
                throw new InvalidOperationException();

            _binding.Converter = new Converter(this);
            return _binding.ProvideValue(serviceProvider);
        }

        private static int IndexOf(string[] names, string name)
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == null)
                    continue;

                if (string.Compare(names[i], name, StringComparison.OrdinalIgnoreCase) == 0)
                    return i;
            }
            return -1;
        }

        private static int IndexOf(object[] names, ulong value)
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == null)
                    continue;

                if (!ulong.TryParse(string.Format("{0}", names[i]), out ulong ul))
                    continue;

                if (ul == value)
                    return i;
            }
            return -1;
        }

        public static object EnumToObject(PropertyGridProperty property, object value)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (value != null && property.PropertyType.IsEnum)
                return Extensions.EnumToObject(property.PropertyType, value);

            if (value != null && value.GetType().IsEnum)
                return Extensions.EnumToObject(value.GetType(), value);

            if (property.PropertyType != typeof(string))
                return Conversions.ChangeType(value, property.PropertyType, defaultValue: null);

            var options = PropertyGridOptionsAttribute.FromProperty(property);
            if (options == null)
                return Conversions.ChangeType(value, property.PropertyType, defaultValue: null);

            return EnumToObject(options, property.PropertyType, value);
        }

        public static object EnumToObject(PropertyGridOptionsAttribute options, Type propertyType, object value)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (propertyType == null)
                throw new ArgumentNullException(nameof(propertyType));

            if (value != null && propertyType.IsEnum)
                return Extensions.EnumToObject(propertyType, value);

            if (value != null && value.GetType().IsEnum)
                return Extensions.EnumToObject(value.GetType(), value);

            if (propertyType != typeof(string))
                return Conversions.ChangeType(value, propertyType, defaultValue: null);

            if (options == null || options.EnumNames == null || options.EnumValues == null || options.EnumValues.Length != options.EnumNames.Length)
                return Conversions.ChangeType(value, propertyType, defaultValue: null);

            if (string.IsNullOrEmpty(string.Format("{0}", value).Nullify()))
                return string.Empty;

            var sb = new StringBuilder();
            var svalue = string.Format("{0}", value);
            if (!ulong.TryParse(svalue, out ulong ul))
            {
                var enums = ParseEnum(svalue);
                if (enums.Count == 0)
                    return string.Empty;

                var enumValues = options.EnumValues.Select(v => string.Format("{0}", v)).ToArray();
                foreach (var enumValue in enums)
                {
                    var index = IndexOf(enumValues, enumValue);
                    if (index < 0)
                    {
                        index = IndexOf(options.EnumNames, enumValue);
                    }

                    if (index >= 0)
                    {
                        if (sb.Length > 0 && options.EnumSeparator != null)
                        {
                            sb.Append(options.EnumSeparator);
                        }
                        sb.Append(options.EnumNames[index]);
                    }
                }
            }
            else // a string
            {
                var bitsCount = (ulong)GetEnumMaxPower(options) - 1; // skip first
                ulong b = 1;
                for (ulong bit = 1; bit < bitsCount; bit++) // signed, skip highest bit
                {
                    if ((ul & b) != 0)
                    {
                        var index = IndexOf(options.EnumValues, b);
                        if (index >= 0)
                        {
                            if (sb.Length > 0 && options.EnumSeparator != null)
                            {
                                sb.Append(options.EnumSeparator);
                            }
                            sb.Append(options.EnumNames[index]);
                        }
                    }
                    b *= 2;
                }
            }

            var s = sb.ToString();
            if (s.Length == 0)
            {
                var index = IndexOf(options.EnumValues, 0);
                if (index >= 0)
                {
                    s = options.EnumNames[index];
                }
            }

            return s;
        }

        private static List<string> ParseEnum(string text)
        {
            var enums = new List<string>();
            var split = text.Split(',', ';', '|', ' ');
            if (split.Length >= 0)
            {
                foreach (var sp in split)
                {
                    if (string.IsNullOrWhiteSpace(sp))
                        continue;

                    enums.Add(sp.Trim());
                }
            }
            return enums;
        }

        public static ulong EnumToUInt64(PropertyGridProperty property, object value)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (value == null)
                return 0;

            var type = value.GetType();
            if (type.IsEnum)
                return Extensions.EnumToUInt64(value);

            var typeCode = Convert.GetTypeCode(value);
            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return (ulong)Convert.ToInt64(value);

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Convert.ToUInt64(value);
            }

            var att = PropertyGridOptionsAttribute.FromProperty(property);
            if (att == null || att.EnumNames == null)
                return 0;

            var svalue = string.Format("{0}", value);
            if (ulong.TryParse(svalue, out ulong ul))
                return ul;

            var enums = ParseEnum(svalue);
            if (enums.Count == 0)
                return 0;

            foreach (var name in enums)
            {
                var index = IndexOf(att.EnumNames, name);
                if (index < 0)
                    continue;

                var ulvalue = Extensions.EnumToUInt64(att.EnumValues[index]);
                ul |= ulvalue;
            }
            return ul;
        }

        public static int GetEnumMaxPower(PropertyGridOptionsAttribute options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.EnumMaxPower <= 0 ? 32 : options.EnumMaxPower;
        }

        internal static bool TryGetDefaultValue(PropertyGridOptionsAttribute options, out string value)
        {
            value = null;
            if (options == null || !options.IsEnum && !options.IsFlagsEnum)
                return false;

            if (options.EnumNames != null && options.EnumNames.Length > 0)
            {
                value = options.EnumNames.First();
                return true;
            }
            return false;
        }

        public virtual IEnumerable BuildItems(PropertyGridProperty property, Type targetType, object parameter, CultureInfo culture)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var isEnumOrNullableEnum = PropertyGridProperty.IsEnumOrNullableEnum(property.PropertyType, out Type enumType, out bool nullable);

            PropertyGridItem zero = null;
            var att = PropertyGridOptionsAttribute.FromProperty(property);
            var items = new ObservableCollection<PropertyGridItem>();
            if (isEnumOrNullableEnum)
            {
                if (nullable)
                {
                    var item = CreateItem();
                    item.Property = property;
                    item.Name = null; // "<unset>";
                    item.Value = null;
                    item.IsUnset = true;
                    items.Add(item);
                }

                var names = Enum.GetNames(enumType);
                var values = Enum.GetValues(enumType);
                if (enumType.IsFlagsEnum())
                {
                    var uvalue = EnumToUInt64(property, property.Value);

                    for (var i = 0; i < names.Length; i++)
                    {
                        var nameValue = EnumToUInt64(property, values.GetValue(i));
                        if (!ShowEnumField(property, enumType, names[i], out string displayName))
                            continue;

                        var item = CreateItem();
                        item.Property = property;
                        item.Name = displayName;
                        item.Value = nameValue;
                        item.IsZero = nameValue == 0;

                        if (nameValue == 0)
                        {
                            zero = item;
                        }

                        // determine if this name is in fact a combination of other names
                        var bitsCount = (ulong)Extensions.GetEnumMaxPower(enumType) - 1; // skip first
                        ulong b = 1;
                        for (ulong bit = 1; bit < bitsCount; bit++) // signed, skip highest bit
                        {
                            b *= 2;
                        }

                        var isChecked = (uvalue & nameValue) != 0;
                        item.IsChecked = isChecked;
                        items.Add(item);
                    }

                    // determine if the lisbox is empty, which we don't want anyway
                    if (items.Count == 0)
                    {
                        var item = CreateItem();
                        item.Property = property;
                        item.Name = DefaultZeroName;
                        item.Value = 0;
                        item.IsZero = true;
                        items.Add(item);
                    }

                    if (uvalue == 0 && zero != null)
                    {
                        zero.IsChecked = true;
                    }
                }
                else
                {
                    for (var i = 0; i < names.Length; i++)
                    {
                        if (!ShowEnumField(property, enumType, names[i], out string displayName))
                            continue;

                        var item = CreateItem();
                        item.Property = property;
                        item.Name = displayName;
                        item.Value = values.GetValue(i);
                        item.IsZero = i == 0; // first one is default
                        items.Add(item);
                    }
                }
            }
            else
            {
                if (att != null && att.IsEnum)
                {
                    var manualFlags = false;
                    // either EnumList or EnumValues can be null but not both
                    // if not null, length must be the same
                    if (att.EnumNames == null || att.EnumNames.Length == 0)
                    {
                        if (att.EnumValues == null || att.EnumValues.Length == 0)
                            return items;

                        att.EnumNames = new string[att.EnumValues.Length];
                        for (var i = 0; i < att.EnumValues.Length; i++)
                        {
                            att.EnumNames[i] = string.Format("{0}", att.EnumValues[i]);
                        }
                    }
                    else
                    {
                        if (att.EnumValues == null || att.EnumValues.Length != att.EnumNames.Length)
                        {
                            att.EnumValues = new object[att.EnumNames.Length];
                            if (att.IsFlagsEnum)
                            {
                                ulong current = 1; // don't use zero when nothing is specified in flags mode
                                manualFlags = true;
                                for (var i = 0; i < att.EnumNames.Length; i++)
                                {
                                    att.EnumValues[i] = current;
                                    current *= 2;
                                }
                            }
                            else
                            {
                                for (var i = 0; i < att.EnumNames.Length; i++)
                                {
                                    att.EnumValues[i] = string.Format("{0}", att.EnumNames[i]);
                                }
                            }
                        }
                    }

                    // items value must of a compatible type with property.Value
                    object valueConverter(object v)
                    {
                        var propType = property.Value != null ? property.Value.GetType() : property.PropertyType;
                        if (v == null)
                        {
                            if (!propType.IsValueType)
                                return null;

                            return Activator.CreateInstance(propType);
                        }

                        var vType = v.GetType();
                        if (propType.IsAssignableFrom(vType))
                            return v;

                        return Conversions.ChangeType(v, propType, defaultValue: null);
                    }

                    if (att.IsFlagsEnum)
                    {
                        var uvalue = EnumToUInt64(property, property.Value);

                        for (var i = 0; i < att.EnumNames.Length; i++)
                        {
                            var nameValue = EnumToUInt64(property, att.EnumValues[i]);

                            var item = CreateItem();
                            item.Property = property;
                            item.Name = att.EnumNames[i];
                            item.Value = valueConverter(att.EnumValues[i]);
                            if (manualFlags)
                            {
                                item.IsZero = i == 0;
                            }
                            else
                            {
                                item.IsZero = nameValue == 0;
                            }

                            if (nameValue == 0)
                            {
                                zero = item;
                            }

                            // note: in this case, we don't support names as a combination of other names
                            var bitsCount = (ulong)GetEnumMaxPower(att) - 1; // skip first
                            ulong b = 1;
                            for (ulong bit = 1; bit < bitsCount; bit++) // signed, skip highest bit
                            {
                                b *= 2;
                            }

                            var isChecked = (uvalue & nameValue) != 0;
                            item.IsChecked = isChecked;
                            items.Add(item);
                        }

                        // determine if the list is empty, which we don't want anyway
                        if (items.Count == 0)
                        {
                            var item = CreateItem();
                            item.Property = property;
                            item.Name = DefaultZeroName;
                            item.Value = valueConverter(0);
                            item.IsZero = true;
                            items.Add(item);
                        }

                        if (uvalue == 0 && zero != null)
                        {
                            zero.IsChecked = true;
                        }
                    }
                    else
                    {
                        for (var i = 0; i < att.EnumNames.Length; i++)
                        {
                            var item = CreateItem();
                            item.Property = property;
                            item.Name = att.EnumNames[i];
                            item.Value = valueConverter(att.EnumValues[i]);
                            item.IsZero = i == 0; // first one is default
                            items.Add(item);
                        }
                    }
                }
            }

            var ctx = new Dictionary<string, object>
            {
                ["items"] = items
            };
            property.OnEvent(this, new PropertyGridEventArgs(property, ctx));
            return items;
        }

        protected virtual bool ShowEnumField(PropertyGridProperty property, Type type, string name, out string displayName)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var fi = type.GetField(name, BindingFlags.Static | BindingFlags.Public);
            displayName = fi.Name;
            var ba = fi.GetAttribute<BrowsableAttribute>();
            if (ba != null && !ba.Browsable)
                return false;

            var da = fi.GetAttribute<DescriptionAttribute>();
            if (da != null && !string.IsNullOrWhiteSpace(da.Description))
            {
                displayName = da.Description;
            }
            return true;
        }

        protected class Converter : IValueConverter
        {
            public Converter(PropertyGridComboBoxExtension extension)
            {
                Extension = extension;
            }

            public PropertyGridComboBoxExtension Extension { get; }

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is PropertyGridProperty property)
                    return Extension.BuildItems(property, targetType, parameter, culture);

                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
        }
    }
}
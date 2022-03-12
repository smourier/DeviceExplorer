using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DeviceExplorer.Utilities
{
    public static class Extensions
    {
        public static T GetSelectedDataContext<T>(this TreeView treeView)
        {
            if (treeView == null)
                return default;

            if (treeView.SelectedItem == null)
                return default;

            if (typeof(T).IsAssignableFrom(treeView.SelectedItem.GetType()))
                return (T)treeView.SelectedItem;

            object context = null;
            if (treeView.SelectedItem is FrameworkElement fe)
            {
                context = fe.DataContext;
            }

            if (context != null && typeof(T).IsAssignableFrom(context.GetType()))
                return (T)context;

            return default;
        }

        public static T GetVisualSelfOrParent<T>(this DependencyObject source) where T : DependencyObject
        {
            if (source == null)
                return default;

            if (source is T t)
                return t;

            if (source is not Visual && source is not Visual3D)
                return default;

            return VisualTreeHelper.GetParent(source).GetVisualSelfOrParent<T>();
        }

        public static T FindFocusableVisualChild<T>(this DependencyObject obj, string name) where T : FrameworkElement
        {
            foreach (var item in obj.EnumerateVisualChildren(true, true).OfType<T>())
            {
                if (item.Focusable && (item.Name == name || name == null))
                    return item;
            }
            return null;
        }

        public static IEnumerable<DependencyProperty> EnumerateMarkupDependencyProperties(object element)
        {
            if (element != null)
            {
                var markupObject = MarkupWriter.GetMarkupObjectFor(element);
                if (markupObject != null)
                {
                    foreach (var mp in markupObject.Properties)
                    {
                        if (mp.DependencyProperty != null)
                            yield return mp.DependencyProperty;
                    }
                }
            }
        }

        public static T FindVisualChild<T>(this DependencyObject obj, string name) where T : FrameworkElement
        {
            foreach (var item in obj.EnumerateVisualChildren(true, true).OfType<T>())
            {
                if (name == null)
                    return item;

                if (item.Name == name)
                    return item;
            }
            return null;
        }

        public static IEnumerable<DependencyObject> EnumerateVisualChildren(this DependencyObject obj, bool recursive = true, bool sameLevelFirst = true)
        {
            if (obj == null)
                yield break;

            if (sameLevelFirst)
            {
                var count = VisualTreeHelper.GetChildrenCount(obj);
                var list = new List<DependencyObject>(count);
                for (var i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(obj, i);
                    if (child == null)
                        continue;

                    yield return child;
                    if (recursive)
                    {
                        list.Add(child);
                    }
                }

                foreach (var child in list)
                {
                    foreach (var grandChild in child.EnumerateVisualChildren(recursive, true))
                    {
                        yield return grandChild;
                    }
                }
            }
            else
            {
                var count = VisualTreeHelper.GetChildrenCount(obj);
                for (var i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(obj, i);
                    if (child == null)
                        continue;

                    yield return child;
                    if (recursive)
                    {
                        foreach (var dp in child.EnumerateVisualChildren(true, false))
                        {
                            yield return dp;
                        }
                    }
                }
            }
        }

        public static IEnumerable<T> GetChildren<T>(this DependencyObject obj)
        {
            if (obj == null)
                yield break;

            foreach (var item in LogicalTreeHelper.GetChildren(obj))
            {
                if (item == null)
                    continue;

                if (item is T t)
                    yield return t;

                if (item is DependencyObject dep)
                {
                    foreach (var child in dep.GetChildren<T>())
                    {
                        yield return child;
                    }
                }
            }
        }

        public static T GetSelfOrParent<T>(this FrameworkElement source) where T : FrameworkElement
        {
            while (true)
            {
                if (source == null)
                    return default;

                if (source is T t)
                    return t;

                source = source.Parent as FrameworkElement;
            }
        }

        public static List<T> SplitToList<T>(this string text, params char[] separators)
        {
            var list = new List<T>();
            if (text != null)
            {
                foreach (var s in text.Split(separators))
                {
                    var item = Conversions.ChangeType<T>(s);
                    list.Add(item);
                }
            }
            return list;
        }

        public static bool IsNullable(this Type type)
        {
            if (type == null)
                return false;

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static Type GetElementType(Type collectionType)
        {
            if (collectionType == null)
                throw new ArgumentNullException(nameof(collectionType));

            foreach (var iface in collectionType.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    return iface.GetGenericArguments()[1];

                if (iface.GetGenericTypeDefinition() == typeof(IList<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }
            return typeof(object);
        }

        public static T GetAttribute<T>(this MemberDescriptor descriptor) where T : Attribute
        {
            if (descriptor == null)
                return null;

            return GetAttribute<T>(descriptor.Attributes);
        }

        public static T GetAttribute<T>(this AttributeCollection attributes) where T : Attribute
        {
            if (attributes == null)
                return null;

            foreach (var att in attributes)
            {
                if (typeof(T).IsAssignableFrom(att.GetType()))
                    return (T)att;
            }
            return null;
        }

        public static IEnumerable<T> GetAttributes<T>(this MemberInfo element) where T : Attribute => (IEnumerable<T>)Attribute.GetCustomAttributes(element, typeof(T));
        public static T GetAttribute<T>(this ICustomAttributeProvider provider) where T : Attribute
        {
            if (provider == null)
                return null;

            var o = provider.GetCustomAttributes(typeof(T), true);
            if (o == null || o.Length == 0)
                return null;

            return (T)o[0];
        }

        public static object EnumToObject(Type enumType, object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)
                throw new ArgumentException(null, nameof(enumType));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            Type underlyingType = Enum.GetUnderlyingType(enumType);
            if (underlyingType == typeof(long))
                return Enum.ToObject(enumType, Conversions.ChangeType<long>(value));

            if (underlyingType == typeof(ulong))
                return Enum.ToObject(enumType, Conversions.ChangeType<ulong>(value));

            if (underlyingType == typeof(int))
                return Enum.ToObject(enumType, Conversions.ChangeType<int>(value));

            if ((underlyingType == typeof(uint)))
                return Enum.ToObject(enumType, Conversions.ChangeType<uint>(value));

            if (underlyingType == typeof(short))
                return Enum.ToObject(enumType, Conversions.ChangeType<short>(value));

            if (underlyingType == typeof(ushort))
                return Enum.ToObject(enumType, Conversions.ChangeType<ushort>(value));

            if (underlyingType == typeof(byte))
                return Enum.ToObject(enumType, Conversions.ChangeType<byte>(value));

            if (underlyingType == typeof(sbyte))
                return Enum.ToObject(enumType, Conversions.ChangeType<sbyte>(value));

            throw new ArgumentException(null, nameof(enumType));
        }

        public static ulong EnumToUInt64(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var typeCode = Convert.GetTypeCode(value);
#pragma warning disable IDE0066 // Convert switch statement to expression
            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return (ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture);

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Convert.ToUInt64(value, CultureInfo.InvariantCulture);

                //case TypeCode.String:
                default:
                    return Conversions.ChangeType<ulong>(value);
            }
#pragma warning restore IDE0066 // Convert switch statement to expression
        }

        public static int GetEnumMaxPower(Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)
                throw new ArgumentException(null, nameof(enumType));

            return GetEnumUnderlyingTypeMaxPower(Enum.GetUnderlyingType(enumType));
        }

        public static int GetEnumUnderlyingTypeMaxPower(Type underlyingType)
        {
            if (underlyingType == null)
                throw new ArgumentNullException(nameof(underlyingType));

            if (underlyingType == typeof(long) || underlyingType == typeof(ulong))
                return 64;

            if (underlyingType == typeof(int) || underlyingType == typeof(uint))
                return 32;

            if (underlyingType == typeof(short) || underlyingType == typeof(ushort))
                return 16;

            if (underlyingType == typeof(byte) || underlyingType == typeof(sbyte))
                return 8;

            throw new ArgumentException(null, nameof(underlyingType));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DeviceExplorer.Utilities
{
    public static class Extensions
    {
        public static bool TryGetValue<T>(this IReadOnlyDictionary<string, object> dictionary, string key, out T value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (dictionary == null || !dictionary.TryGetValue(key, out object v))
            {
                value = default;
                return false;
            }

            return Conversions.TryChangeType(v, out value);
        }

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
    }
}

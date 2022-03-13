using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;

namespace DeviceExplorer.Utilities
{
    [ContentProperty("DataTemplate")]
    public class PropertyGridDataTemplate
    {
        private class NullableEnum
        {
        }

        public static readonly Type NullableEnumType = typeof(NullableEnum);

        private List<Type> _resolvedPropertyTypes;
        private List<Type> _resolvedCollectionItemPropertyTypes;

        public string PropertyType { get; set; }
        public string CollectionItemPropertyType { get; set; }
        public DataTemplate DataTemplate { get; set; }  // note: may be null
        public bool? IsCollection { get; set; }
        public bool? IsReadOnly { get; set; }
        public bool? IsError { get; set; }
        public bool? IsValid { get; set; }
        public bool? IsFlagsEnum { get; set; }
        public bool? IsCollectionItemValueType { get; set; }
        public bool? IsValueType { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }

        public virtual IList<Type> ResolvedPropertyTypes
        {
            get
            {
                if (_resolvedPropertyTypes == null)
                {
                    _resolvedPropertyTypes = new List<Type>();
                    var names = PropertyType.SplitToList<string>('|');
                    foreach (var name in names)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        Type type;
                        // a hack to handle nullable enum in a general way
                        if (name == "System.Nullable`1[System.Enum]")
                        {
                            type = NullableEnumType;
                        }
                        else
                        {
                            type = Type.GetType(name, true);
                        }

                        if (type != null)
                        {
                            _resolvedPropertyTypes.Add(type);
                        }
                    }
                }
                return _resolvedPropertyTypes;
            }
        }

        public virtual IList<Type> ResolvedCollectionItemPropertyTypes
        {
            get
            {
                if (_resolvedCollectionItemPropertyTypes == null)
                {
                    _resolvedCollectionItemPropertyTypes = new List<Type>();
                    var names = CollectionItemPropertyType.SplitToList<string>('|');
                    foreach (var name in names)
                    {
                        if (string.IsNullOrWhiteSpace(name))
                            continue;

                        var type = Type.GetType(name, true);
                        if (type != null)
                        {
                            _resolvedCollectionItemPropertyTypes.Add(type);
                        }
                    }
                }
                return _resolvedCollectionItemPropertyTypes;
            }
        }
    }
}
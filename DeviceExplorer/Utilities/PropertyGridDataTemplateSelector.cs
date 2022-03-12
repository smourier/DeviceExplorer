using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace DeviceExplorer.Utilities
{
    [ContentProperty("DataTemplates")]
    public class PropertyGridDataTemplateSelector : DataTemplateSelector
    {
        public PropertyGridDataTemplateSelector()
        {
            DataTemplates = new ObservableCollection<PropertyGridDataTemplate>();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public ObservableCollection<PropertyGridDataTemplate> DataTemplates { get; }
        public PropertyGrid PropertyGrid { get; private set; }

        protected virtual bool Filter(PropertyGridDataTemplate template, PropertyGridProperty property)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            if (property == null)
                throw new ArgumentNullException(nameof(property));

            // check various filters
            if (template.IsCollection.HasValue && template.IsCollection.Value != property.IsCollection)
                return true;

            if (template.IsCollectionItemValueType.HasValue && template.IsCollectionItemValueType.Value != property.IsCollectionItemValueType)
                return true;

            if (template.IsValueType.HasValue && template.IsValueType.Value != property.IsValueType)
                return true;

            if (template.IsReadOnly.HasValue && template.IsReadOnly.Value != property.IsReadOnly)
                return true;

            if (template.IsError.HasValue && template.IsError.Value != property.IsError)
                return true;

            if (template.IsValid.HasValue && template.IsValid.Value != property.IsValid)
                return true;

            if (template.IsFlagsEnum.HasValue && template.IsFlagsEnum.Value != property.IsFlagsEnum)
                return true;

            if (template.Category != null && !property.Category.EqualsIgnoreCase(template.Category))
                return true;

            if (template.Name != null && !property.Name.EqualsIgnoreCase(template.Name))
                return true;

            return false;
        }

        public virtual bool IsAssignableFrom(Type type, Type propertyType, PropertyGridDataTemplate template, PropertyGridProperty property)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (propertyType == null)
                throw new ArgumentNullException(nameof(propertyType));

            if (template == null)
                throw new ArgumentNullException(nameof(template));

            if (property == null)
                throw new ArgumentNullException(nameof(property));

            if (type.IsAssignableFrom(propertyType))
            {
                // bool? is assignable from bool, but we don't want that match
                if (!type.IsNullable() || propertyType.IsNullable())
                    return true;
            }

            // hack for nullable enums...
            if (type == PropertyGridDataTemplate.NullableEnumType)
            {
                PropertyGridProperty.IsEnumOrNullableEnum(propertyType, out _, out bool nullable);
                if (nullable)
                    return true;
            }

            var options = PropertyGridOptionsAttribute.FromProperty(property);
            if (options != null)
            {
                if ((type.IsEnum || type == typeof(Enum)) && options.IsEnum)
                {
                    if (!options.IsFlagsEnum)
                        return true;

                    if (type.IsFlagsEnum())
                        return true;

                    if (template.IsFlagsEnum.HasValue && template.IsFlagsEnum.Value)
                        return true;
                }
            }

            return false;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));

            if (item is not PropertyGridProperty property)
                return base.SelectTemplate(item, container);

            var propTemplate = PropertyGridOptionsAttribute.SelectTemplate(property, item, container);
            if (propTemplate != null)
                return propTemplate;

            if (PropertyGrid == null)
            {
                PropertyGrid = container.GetVisualSelfOrParent<PropertyGrid>();
            }

            if (PropertyGrid.ValueEditorTemplateSelector != null && PropertyGrid.ValueEditorTemplateSelector != this)
            {
                var template = PropertyGrid.ValueEditorTemplateSelector.SelectTemplate(item, container);
                if (template != null)
                    return template;
            }

            foreach (var template in DataTemplates)
            {
                if (Filter(template, property))
                    continue;

                if (template.IsCollection.HasValue && template.IsCollection.Value)
                {
                    if (string.IsNullOrWhiteSpace(template.CollectionItemPropertyType) && template.DataTemplate != null)
                        return template.DataTemplate;

                    if (property.CollectionItemPropertyType != null)
                    {
                        foreach (var type in template.ResolvedCollectionItemPropertyTypes)
                        {
                            if (IsAssignableFrom(type, property.CollectionItemPropertyType, template, property))
                                return template.DataTemplate;
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(template.PropertyType) && template.DataTemplate != null)
                        return template.DataTemplate;

                    foreach (var type in template.ResolvedPropertyTypes)
                    {
                        if (IsAssignableFrom(type, property.PropertyType, template, property))
                            return template.DataTemplate;
                    }
                }
            }
            return base.SelectTemplate(item, container);
        }
    }
}

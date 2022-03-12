using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DeviceExplorer.Utilities
{
    [AttributeUsage(AttributeTargets.All)]
    public class PropertyGridOptionsAttribute : Attribute
    {
        public PropertyGridOptionsAttribute()
        {
            EnumSeparator = ", ";
        }

        public string[] EnumNames { get; set; }
        public object[] EnumValues { get; set; }
        public bool IsEnum { get; set; }
        public bool IsFlagsEnum { get; set; }
        public int EnumMaxPower { get; set; }
        public bool CollectionEditorHasOnlyOneColumn { get; set; }
        public int SortOrder { get; set; }
        public string EditorDataTemplatePropertyPath { get; set; }
        public string EditorDataTemplateSelectorPropertyPath { get; set; }
        public Type EditorType { get; set; }
        public string EditorResourceKey { get; set; }
        public object EditorDataTemplateResourceKey { get; set; }
        public Type PropertyType { get; set; }
        public bool ForceReadWrite { get; set; }
        public bool HasDefaultValue { get; set; }
        public bool ForcePropertyChanged { get; set; }
        public object DefaultValue { get; set; }
        public string EnumSeparator { get; set; }

        public static DataTemplate SelectTemplate(PropertyGridProperty property, object item, DependencyObject container)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var att = FromProperty(property);
            if (att == null)
                return null;

            var rk = att.EditorDataTemplateResourceKey;
            if (rk != null)
            {
                if (Application.Current != null)
                {
                    var dt = (DataTemplate)Application.Current.TryFindResource(rk);
                    if (dt != null)
                        return dt;
                }

                if (container is FrameworkElement fe)
                {
                    var dt = (DataTemplate)fe.TryFindResource(rk);
                    if (dt != null)
                        return dt;
                }

                return null;
            }

            if (att.EditorType != null)
            {
                var editor = Activator.CreateInstance(att.EditorType);
                if (att.EditorDataTemplateSelectorPropertyPath != null)
                {
                    var dts = (DataTemplateSelector)DataBindingEvaluator.GetPropertyValue(editor, att.EditorDataTemplateSelectorPropertyPath);
                    return dts?.SelectTemplate(item, container);
                }

                if (att.EditorDataTemplatePropertyPath != null)
                    return (DataTemplate)DataBindingEvaluator.GetPropertyValue(editor, att.EditorDataTemplatePropertyPath);

                if (editor is ContentControl cc)
                {
                    if (cc.ContentTemplateSelector != null)
                    {
                        var template = cc.ContentTemplateSelector.SelectTemplate(item, container);
                        if (template != null)
                            return template;
                    }

                    return cc.ContentTemplate;
                }

                if (editor is ContentPresenter cp)
                {
                    if (cp.ContentTemplateSelector != null)
                    {
                        var template = cp.ContentTemplateSelector.SelectTemplate(item, container);
                        if (template != null)
                            return template;
                    }

                    return cp.ContentTemplate;
                }
            }
            return null;
        }

        public static PropertyGridOptionsAttribute FromProperty(PropertyGridProperty property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var att = property.Options;
            if (att != null)
                return att;

            if (property.Descriptor != null)
            {
                att = property.Descriptor.GetAttribute<PropertyGridOptionsAttribute>();
            }
            return att;
        }
    }
}
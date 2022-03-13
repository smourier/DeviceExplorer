using System.Collections.Generic;

namespace DeviceExplorer.Utilities
{
    public interface IPropertyGridObject
    {
        void FinalizeProperties(PropertyGridDataProvider dataProvider, IList<PropertyGridProperty> properties);
        bool TryShowEditor(PropertyGridProperty property, object editor, out bool? result);
        void EditorClosed(PropertyGridProperty property, object editor);
    }
}
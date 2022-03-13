using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using DeviceExplorer.Utilities;

namespace DeviceExplorer.Model
{
    public abstract class TreeItem : DictionaryObject, IComparable, IComparable<TreeItem>
    {
        protected TreeItem(TreeItem parent)
        {
            Parent = parent;
            Children = new SortableObservableCollection<TreeItem>
            {
                SortingSelector = o => o.Name
            };
        }

        public TreeItem Parent { get; }
        public SortableObservableCollection<TreeItem> Children { get; }
        public virtual string ErrorText { get => DictionaryObjectGetPropertyValue<string>() ?? ((IDataErrorInfo)this).Error; set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool IsSelected { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool IsHidden { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual ImageSource Image { get => DictionaryObjectGetPropertyValue<ImageSource>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual string Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
        public bool IsCollapsed => !IsExpanded;
        public virtual IEnumerable<Property> Properties => Enumerable.Empty<Property>();
        public virtual bool IsExpanded
        {
            get => DictionaryObjectGetPropertyValue<bool>();
            set
            {
                if (DictionaryObjectSetPropertyValue(value))
                {
                    if (IsExpanded && Parent != null)
                    {
                        Parent.IsExpanded = true;
                    }
                }
            }
        }

        public override string ToString() => Name;

        public virtual void CollapseAll()
        {
            IsExpanded = false;
            foreach (var obj in Children)
            {
                obj.CollapseAll();
            }
        }

        public virtual void ExpandAll()
        {
            IsExpanded = true;
            if (IsExpanded)
            {
                foreach (var obj in Children)
                {
                    obj.ExpandAll();
                }
            }
        }

        int IComparable.CompareTo(object obj) => CompareTo(obj as TreeItem);
        public virtual int CompareTo(TreeItem other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            var name = Name;
            if (name == null)
                return 1;

            var oname = other.Name;
            if (oname == null)
                return -1;

            return name.CompareTo(oname);
        }
    }
}

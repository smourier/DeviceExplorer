using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using DeviceExplorer.Utilities;

namespace DeviceExplorer.Model
{
    public abstract class TreeItem : DictionaryObject, IComparable, IComparable<TreeItem>
    {
        private static readonly TreeItem _lazy = new Lazy();
        private readonly bool _hasLazy;

        private class Lazy : TreeItem
        {
            public Lazy()
                : base(null, false)
            {
            }

            protected override void LoadChildren() => throw new NotSupportedException();
        }

        protected TreeItem(TreeItem parent, bool hasLazy)
        {
            Parent = parent;
            _hasLazy = hasLazy;
            Children = new ObservableCollection<TreeItem>();
            if (hasLazy)
            {
                Children.Add(_lazy);
            }
        }

        public TreeItem Parent { get; }
        public virtual object SelectableObject { get; }
        public bool HasLazyChild => Children.Count > 0 && Children[0] == _lazy;
        public ObservableCollection<TreeItem> Children { get; }
        public virtual string ErrorText { get => DictionaryObjectGetPropertyValue<string>() ?? ((IDataErrorInfo)this).Error; set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool IsSelected { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual bool IsHidden { get => DictionaryObjectGetPropertyValue<bool>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual ImageSource Image { get => DictionaryObjectGetPropertyValue<ImageSource>(); set => DictionaryObjectSetPropertyValue(value); }
        public virtual string Name { get => DictionaryObjectGetPropertyValue<string>(); set => DictionaryObjectSetPropertyValue(value); }
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

                    LazyLoadChildren();
                }
            }
        }

        protected abstract void LoadChildren();

        public override string ToString() => Name;

        public virtual void Reset()
        {
            IsExpanded = false;
            Children.Clear();
            if (_hasLazy)
            {
                Children.Add(_lazy);
            }
        }

        public virtual void LazyLoadChildren()
        {
            if (!HasLazyChild)
                return;

            Children.Remove(_lazy);
            try
            {
                LoadChildren();
            }
            catch (Exception e)
            {
                HandleLoadChildrenError(e);
            }
        }

        protected virtual void HandleLoadChildrenError(Exception e)
        {
            ErrorText = e.Message;
            Children.Add(_lazy);
            IsExpanded = false;
        }

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

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace DeviceExplorer.Utilities
{
    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        private Func<T, object> _sortingSelector;
        private bool _descending;

        public virtual Func<T, object> SortingSelector
        {
            get => _sortingSelector;
            set
            {
                if (_sortingSelector == value)
                    return;

                _sortingSelector = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(SortingSelector)));
                OnPropertyChanged(new PropertyChangedEventArgs("Items[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public virtual bool Descending
        {
            get => _descending;
            set
            {
                if (_descending == value)
                    return;

                _descending = value;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Descending)));
                OnPropertyChanged(new PropertyChangedEventArgs("Items[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            if (SortingSelector == null || e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
                return;

            var query = this.Select((item, index) => (Item: item, Index: index));
            query = Descending ? query.OrderByDescending(tuple => SortingSelector(tuple.Item)) : query.OrderBy(tuple => SortingSelector(tuple.Item));
            var map = query.Select((tuple, index) => (OldIndex: tuple.Index, NewIndex: index)).Where(o => o.OldIndex != o.NewIndex);
            using var enumerator = map.GetEnumerator();
            if (enumerator.MoveNext())
            {
                Move(enumerator.Current.OldIndex, enumerator.Current.NewIndex);
            }
        }
    }
}

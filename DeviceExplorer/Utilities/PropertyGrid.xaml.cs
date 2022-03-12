using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DeviceExplorer.Utilities
{
    public partial class PropertyGrid : UserControl
    {
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(PropertyGrid),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure, IsReadOnlyPropertyChanged));

        public static readonly DependencyProperty ReadOnlyBackgroundProperty =
            DependencyProperty.Register(nameof(ReadOnlyBackground), typeof(Brush), typeof(PropertyGrid),
            new FrameworkPropertyMetadata(Brushes.LightSteelBlue, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SelectedObjectProperty =
            DependencyProperty.Register(nameof(SelectedObject), typeof(object), typeof(PropertyGrid),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure, SelectedObjectPropertyChanged));

        public static readonly DependencyProperty ValueEditorTemplateSelectorProperty =
            DependencyProperty.Register(nameof(ValueEditorTemplateSelector), typeof(DataTemplateSelector), typeof(PropertyGrid), new FrameworkPropertyMetadata(null));

        public static readonly RoutedEvent BrowseEvent = EventManager.RegisterRoutedEvent(nameof(Browse), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PropertyGrid));

        public static RoutedCommand NewGuidCommand = new();
        public static RoutedCommand EmptyGuidCommand = new();
        public static RoutedCommand IncrementGuidCommand = new();
        public static RoutedCommand BrowseCommand = new();

        public event EventHandler<PropertyGridEventArgs> PropertyChanged;
        public event RoutedEventHandler Browse { add { AddHandler(BrowseEvent, value); } remove { RemoveHandler(BrowseEvent, value); } }

        public PropertyGrid()
        {
            DefaultCategoryName = CategoryAttribute.Default.Category;
            ChildEditorWindowOffset = 20;
            InitializeComponent();
            PropertiesSource = (CollectionViewSource)FindResource(nameof(PropertiesSource));
            CommandBindings.Add(new CommandBinding(NewGuidCommand, OnGuidCommandExecuted, OnGuidCommandCanExecute));
            CommandBindings.Add(new CommandBinding(EmptyGuidCommand, OnGuidCommandExecuted, OnGuidCommandCanExecute));
            CommandBindings.Add(new CommandBinding(IncrementGuidCommand, OnGuidCommandExecuted, OnGuidCommandCanExecute));
            CommandBindings.Add(new CommandBinding(BrowseCommand, OnBrowseCommandExecuted));
            DecamelizePropertiesDisplayNames = true;
        }

        public CollectionViewSource PropertiesSource { get; }
        public virtual string DefaultCategoryName { get; set; }
        public virtual double ChildEditorWindowOffset { get; set; }
        public virtual bool DecamelizePropertiesDisplayNames { get; set; }
        public DataGrid BaseGrid => PropertiesGrid;
        public Brush ReadOnlyBackground { get => (Brush)GetValue(ReadOnlyBackgroundProperty); set => SetValue(ReadOnlyBackgroundProperty, value); }
        public object SelectedObject { get => GetValue(SelectedObjectProperty); set => SetValue(SelectedObjectProperty, value); }
        public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }
        public DataTemplateSelector ValueEditorTemplateSelector { get => (DataTemplateSelector)GetValue(ValueEditorTemplateSelectorProperty); set => SetValue(ValueEditorTemplateSelectorProperty, value); }

        public bool GroupByCategory
        {
            get => PropertiesSource.GroupDescriptions.Count > 0;
            set
            {
                if (value)
                {
                    if (GroupByCategory)
                        return;

                    PropertiesSource.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
                }
                else
                {
                    if (!GroupByCategory)
                        return;

                    PropertiesSource.GroupDescriptions.Clear();
                }
            }
        }

        public virtual DataGridColumn GetValueColumn() => PropertiesGrid.Columns.OfType<DataGridTemplateColumn>().FirstOrDefault(c => c.CellTemplateSelector is PropertyGridDataTemplateSelector);

        public virtual FrameworkElement GetValueCellContent(object dataItem)
        {
            if (dataItem == null)
                throw new ArgumentNullException(nameof(dataItem));

            var col = GetValueColumn();
            if (col == null)
                return null;

            return col.GetCellContent(dataItem);
        }

        public virtual void UpdateCellBindings(object dataItem, string childName, Func<Binding, bool> where, Action<BindingExpression> action)
        {
            if (dataItem == null)
                throw new ArgumentNullException(nameof(dataItem));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var fe = GetValueCellContent(dataItem);
            if (fe == null)
                return;

            if (childName == null)
            {
                foreach (var child in fe.EnumerateVisualChildren(true).OfType<UIElement>())
                {
                    UpdateBindings(child, where, action);
                }
            }
            else
            {
                var child = fe.FindVisualChild<FrameworkElement>(childName);
                if (child != null)
                {
                    UpdateBindings(child, where, action);
                }
            }
        }

        public static void UpdateBindings(UIElement element, Func<Binding, bool> where, Action<BindingExpression> action)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (where == null)
            {
                where = b => true;
            }

            foreach (DependencyProperty prop in Extensions.EnumerateMarkupDependencyProperties(element))
            {
                BindingExpression expr = BindingOperations.GetBindingExpression(element, prop);
                if (expr != null && expr.ParentBinding != null && where(expr.ParentBinding))
                {
                    action(expr);
                }
            }
        }

        protected virtual Window GetEditor(PropertyGridProperty property, object parameter)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            string resourceKey = string.Format("{0}", parameter);
            if (string.IsNullOrWhiteSpace(resourceKey))
            {
                var att = PropertyGridOptionsAttribute.FromProperty(property);
                if (att != null)
                {
                    resourceKey = att.EditorResourceKey;
                }

                if (string.IsNullOrWhiteSpace(resourceKey))
                {
                    resourceKey = property.DefaultEditorResourceKey;
                    if (string.IsNullOrWhiteSpace(resourceKey))
                    {
                        resourceKey = "ObjectEditorWindow";
                    }
                }
            }

            var editor = TryFindResource(resourceKey) as Window;
            if (editor != null)
            {
                editor.Owner = this.GetVisualSelfOrParent<Window>();
                if (editor.Owner != null)
                {
                    var wo = PropertyGridWindowManager.GetOptions(editor);
                    if (wo.HasFlag(PropertyGridWindowOptions.UseDefinedSize))
                    {
                        if (double.IsNaN(editor.Left))
                        {
                            editor.Left = editor.Owner.Left + ChildEditorWindowOffset;
                        }

                        if (double.IsNaN(editor.Top))
                        {
                            editor.Top = editor.Owner.Top + ChildEditorWindowOffset;
                        }

                        if (double.IsNaN(editor.Width))
                        {
                            editor.Width = editor.Owner.Width;
                        }

                        if (double.IsNaN(editor.Height))
                        {
                            editor.Height = editor.Owner.Height;
                        }
                    }
                    else
                    {
                        editor.Left = editor.Owner.Left + ChildEditorWindowOffset;
                        editor.Top = editor.Owner.Top + ChildEditorWindowOffset;
                        editor.Width = editor.Owner.Width;
                        editor.Height = editor.Owner.Height;
                    }
                }
                editor.DataContext = property;
                if (LogicalTreeHelper.FindLogicalNode(editor, "EditorSelector") is Selector selector)
                {
                    selector.SelectedIndex = 0;
                }

                if (LogicalTreeHelper.FindLogicalNode(editor, "CollectionEditorListGrid") is Grid grid && grid.ColumnDefinitions.Count > 2)
                {
                    if (property.IsCollection && CollectionEditorHasOnlyOneColumn(property))
                    {
                        grid.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
                        grid.ColumnDefinitions[2].Width = new GridLength(0, GridUnitType.Pixel);
                    }
                    else
                    {
                        grid.ColumnDefinitions[1].Width = new GridLength(5, GridUnitType.Pixel);
                        grid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                    }
                }

                if (editor is IPropertyGridEditor pge)
                {
                    if (!pge.SetContext(property, parameter))
                        return null;
                }
            }
            return editor;
        }

        private static readonly HashSet<Type> _collectionEditorHasOnlyOneColumnList = new(new[]
            {
                typeof(string), typeof(decimal), typeof(byte), typeof(sbyte), typeof(float), typeof(double),
                typeof(int), typeof(uint), typeof(short), typeof(ushort), typeof(long), typeof(ulong),
                typeof(bool), typeof(Guid), typeof(char),
                typeof(Uri), typeof(Version)
                // NOTE: timespan, datetime?
            });

        protected virtual bool CollectionEditorHasOnlyOneColumn(PropertyGridProperty property)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var att = PropertyGridOptionsAttribute.FromProperty(property);
            if (att != null)
                return att.CollectionEditorHasOnlyOneColumn;

            if (_collectionEditorHasOnlyOneColumnList.Contains(property.CollectionItemPropertyType))
                return true;

            return !PropertyGridDataProvider.HasProperties(property.CollectionItemPropertyType);
        }

        public virtual bool? ShowEditor(string propertyName, object parameter)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var property = GetProperty(propertyName);
            if (property == null)
                return null;

            return ShowEditor(property, parameter);
        }

        public virtual bool? ShowEditor(PropertyGridProperty property, object parameter)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            var editor = GetEditor(property, parameter);
            if (editor != null)
            {
                bool? ret;
                var go = property.DataProvider.Data as IPropertyGridObject;
                if (go != null)
                {
                    if (go.TryShowEditor(property, editor, out ret))
                        return ret;

                    RefreshSelectedObject(editor);
                }

                ret = editor.ShowDialog();
                if (go != null)
                {
                    go.EditorClosed(property, editor);
                }
                return ret;
            }
            return null;
        }

        protected virtual void OnBrowseCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var browse = new RoutedEventArgs(BrowseEvent, e.OriginalSource);
            RaiseEvent(browse);
            if (browse.Handled)
                return;

            var property = PropertyGridProperty.FromEvent(e);
            if (property != null)
            {
                property.Executed(sender, e);
                if (!e.Handled)
                {
                    ShowEditor(property, e.Parameter);
                }
            }
        }

        protected virtual void OnGuidCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.OriginalSource is TextBox tb)
            {
                if (NewGuidCommand.Equals(e.Command))
                {
                    tb.Text = Guid.NewGuid().ToString(NormalizeGuidParameter(e.Parameter));
                    return;
                }

                if (EmptyGuidCommand.Equals(e.Command))
                {
                    tb.Text = Guid.Empty.ToString(NormalizeGuidParameter(e.Parameter));
                    return;
                }

                if (IncrementGuidCommand.Equals(e.Command))
                {
                    var g = Conversions.ChangeType(tb.Text.Trim(), Guid.Empty);
                    var bytes = g.ToByteArray();
                    bytes[15]++;
                    tb.Text = new Guid(bytes).ToString(NormalizeGuidParameter(e.Parameter));
                    return;
                }
            }
        }

        private static string NormalizeGuidParameter(object parameter)
        {
            const string GuidParameters = "DNBPX";
            var p = string.Format("{0}", parameter).ToUpperInvariant();
            if (p.Length == 0)
                return GuidParameters[0].ToString(CultureInfo.InvariantCulture);

            var ch = GuidParameters.FirstOrDefault(c => c == p[0]);
            return ch == 0 ? GuidParameters[0].ToString(CultureInfo.InvariantCulture) : ch.ToString(CultureInfo.InvariantCulture);
        }

        protected virtual void OnGuidCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var property = PropertyGridProperty.FromEvent(e);
            if (property != null && (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?)))
            {
                e.CanExecute = true;
            }
        }

        public virtual PropertyGridDataProvider CreateDataProvider(object value) => new(this, value);
        public virtual PropertyGridEventArgs CreateEventArgs(PropertyGridProperty property) => new(property);

        private static void IsReadOnlyPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var grid = (PropertyGrid)source;
            grid.PropertiesSource.Source = grid.PropertiesSource.Source;
        }

        public static void FocusChildUsingBinding(FrameworkElement element)
        {
            if (element == null)
                throw new ArgumentNullException(nameof(element));

            // for some reason, this binding does not work, but we still use it and do our own automatically
            var expr = element.GetBindingExpression(FocusManager.FocusedElementProperty);
            if (expr != null && expr.ParentBinding != null && expr.ParentBinding.ElementName != null)
            {
                var child = element.FindFocusableVisualChild<FrameworkElement>(expr.ParentBinding.ElementName);
                if (child != null)
                {
                    child.Focus();
                }
            }
        }

        public static void RefreshSelectedObject(DependencyObject editor)
        {
            foreach (var grid in editor.GetChildren<PropertyGrid>())
            {
                grid.RefreshSelectedObject();
            }
        }

        public virtual void RefreshSelectedObject()
        {
            if (SelectedObject == null)
                return;

            PropertiesSource.Source = CreateDataProvider(SelectedObject);
        }

        private static void SelectedObjectPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs e)
        {
            var grid = (PropertyGrid)source;
            if (e.OldValue is INotifyPropertyChanged pc)
            {
                pc.PropertyChanged -= grid.OnDispatcherSourcePropertyChanged;
            }

            if (e.NewValue == null)
            {
                grid.PropertiesSource.Source = null;
                return;
            }

            var roa = e.NewValue.GetType().GetAttribute<ReadOnlyAttribute>();
            if (roa != null && roa.IsReadOnly)
            {
                grid.IsReadOnly = true;
            }
            else
            {
                grid.IsReadOnly = false;
            }

            pc = e.NewValue as INotifyPropertyChanged;
            if (pc != null)
            {
                pc.PropertyChanged += grid.OnDispatcherSourcePropertyChanged;
            }

            grid.PropertiesSource.Source = grid.CreateDataProvider(e.NewValue);
        }

        private void OnDispatcherSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(() => OnSourcePropertyChanged(sender, e)));
            }
            else
            {
                OnSourcePropertyChanged(sender, e);
            }
        }

        protected virtual void OnPropertyChanged(object sender, PropertyGridEventArgs e) => PropertyChanged?.Invoke(sender, e);

        public virtual PropertyGridProperty GetProperty(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var context = GetDataProvider();
            if (context == null)
                return null;

            return context.Properties.FirstOrDefault(p => p.Name.EqualsIgnoreCase(name));
        }

        public virtual PropertyGridDataProvider GetDataProvider() => PropertiesSource.Source as PropertyGridDataProvider;

        protected virtual void OnSourcePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e == null || e.PropertyName == null)
                return;

            var property = GetProperty(e.PropertyName);
            if (property != null)
            {
                var forceRaise = false;
                var options = PropertyGridOptionsAttribute.FromProperty(property);
                if (options != null)
                {
                    forceRaise = options.ForcePropertyChanged;
                }

                var doptions = DictionaryObjectPropertySetOptions.TrackChanges;
                if (forceRaise)
                {
                    doptions |= DictionaryObjectPropertySetOptions.ForceRaiseOnPropertyChanged;
                }

                property.RefreshValueFromDescriptor(doptions);
                OnPropertyChanged(this, CreateEventArgs(property));
            }
        }

        public virtual void OnToggleButtonIsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is ToggleButton button)
            {
                if (button.DataContext is PropertyGridItem item && item.Property != null && item.Property.IsEnum && item.Property.IsFlagsEnum)
                {
                    if (button.IsChecked.HasValue)
                    {
                        var itemValue = PropertyGridComboBoxExtension.EnumToUInt64(item.Property, item.Value);
                        var propertyValue = PropertyGridComboBoxExtension.EnumToUInt64(item.Property, item.Property.Value);
                        ulong newValue;
                        if (button.IsChecked.Value)
                        {
                            if (itemValue == 0)
                            {
                                newValue = 0;
                            }
                            else
                            {
                                newValue = propertyValue | itemValue;
                            }
                        }
                        else
                        {
                            newValue = propertyValue & ~itemValue;
                        }

                        var propValue = PropertyGridComboBoxExtension.EnumToObject(item.Property, newValue);
                        item.Property.Value = propValue;

                        var li = button.GetVisualSelfOrParent<ListBoxItem>();
                        if (li != null)
                        {
                            var parent = ItemsControl.ItemsControlFromItemContainer(li);
                            if (parent != null)
                            {
                                if (button.IsChecked.Value && itemValue == 0)
                                {
                                    foreach (var gridItem in parent.Items.OfType<PropertyGridItem>())
                                    {
                                        gridItem.IsChecked = PropertyGridComboBoxExtension.EnumToUInt64(item.Property, gridItem.Value) == 0;
                                    }
                                }
                                else
                                {
                                    foreach (var gridItem in parent.Items.OfType<PropertyGridItem>())
                                    {
                                        var gridItemValue = PropertyGridComboBoxExtension.EnumToUInt64(item.Property, gridItem.Value);
                                        if (gridItemValue == 0)
                                        {
                                            gridItem.IsChecked = newValue == 0;
                                            continue;
                                        }

                                        gridItem.IsChecked = (newValue & gridItemValue) == gridItemValue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void OnUIElementPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (e.OriginalSource is ListBoxItem item)
                {
                    if (item.DataContext is PropertyGridItem gridItem)
                    {
                        if (gridItem.IsChecked.HasValue)
                        {
                            gridItem.IsChecked = !gridItem.IsChecked.Value;
                        }
                    }
                }
            }
        }

        protected virtual void OnEditorWindowSaveExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (Window)sender;
            if (window.DataContext is PropertyGridProperty prop)
            {
                prop.Executed(sender, e);
            }
        }

        protected virtual void OnEditorWindowSaveCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var window = (Window)sender;
            if (window.DataContext is PropertyGridProperty prop)
            {
                prop.CanExecute(sender, e);
                if (e.Handled)
                    return;
            }
            e.CanExecute = true;
        }

        protected virtual void OnEditorWindowCloseExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var window = (Window)sender;
            if (window.DataContext is PropertyGridProperty prop)
            {
                prop.Executed(sender, e);
                if (e.Handled)
                    return;
            }
            window.Close();
        }

        protected virtual void OnEditorWindowCloseCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            var window = (Window)sender;
            if (window.DataContext is PropertyGridProperty prop)
            {
                prop.CanExecute(sender, e);
                if (e.Handled)
                    return;
            }
            e.CanExecute = true;
        }

        protected virtual void OnEditorSelectorSelectionChanged(object sender, SelectionChangedEventArgs e) => OnEditorSelectorSelectionChanged(this, "CollectionEditorPropertiesGrid", sender, e);
        public static void OnEditorSelectorSelectionChanged(string childPropertyGridName, object sender, SelectionChangedEventArgs e) => OnEditorSelectorSelectionChanged(null, childPropertyGridName, sender, e);

        public static void OnEditorSelectorSelectionChanged(PropertyGrid parentGrid, string childPropertyGridName, object sender, SelectionChangedEventArgs e)
        {
            if (childPropertyGridName == null)
                throw new ArgumentNullException(nameof(childPropertyGridName));

            if (e.AddedItems.Count > 0)
            {
                if (sender is FrameworkElement obj)
                {
                    var window = obj.GetSelfOrParent<Window>();
                    if (window != null)
                    {
                        if (LogicalTreeHelper.FindLogicalNode(window, childPropertyGridName) is PropertyGrid pg)
                        {
                            if (parentGrid != null)
                            {
                                pg.DefaultCategoryName = parentGrid.DefaultCategoryName;
                            }
                            pg.SelectedObject = e.AddedItems[0];
                        }
                    }
                }
            }
        }
    }
}
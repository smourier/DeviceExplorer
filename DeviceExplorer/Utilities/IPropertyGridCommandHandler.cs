using System.Windows.Input;

namespace DeviceExplorer.Utilities
{
    public interface IPropertyGridCommandHandler
    {
        void CanExecute(PropertyGridProperty property, object sender, CanExecuteRoutedEventArgs e);
        void Executed(PropertyGridProperty property, object sender, ExecutedRoutedEventArgs e);
    }
}
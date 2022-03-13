using System.Windows;

namespace DeviceExplorer
{
    public partial class App : Application
    {
        public new static App Current => (App)Application.Current;
    }
}

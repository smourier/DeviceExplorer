using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace DeviceExplorer.Utilities
{
    public interface IDictionaryObject
    {
        ConcurrentDictionary<string, DictionaryObjectProperty> Properties { get; }

        T GetPropertyValue<T>(T defaultValue, [CallerMemberName] string name = null);
        void SetPropertyValue(object value, DictionaryObjectPropertySetOptions options, [CallerMemberName] string name = null);
    }
}

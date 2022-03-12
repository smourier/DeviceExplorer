using System;
using System.Globalization;
using System.Windows.Data;

namespace DeviceExplorer.Utilities
{
    public class PropertyGridConverter : IValueConverter
    {
        private static Type GetParameterAsType(object parameter)
        {
            if (parameter == null)
                return null;

            var typeName = string.Format("{0}", parameter);
            if (string.IsNullOrWhiteSpace(typeName))
                return null;

            return Type.GetType(typeName, true);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var parameterType = GetParameterAsType(parameter);
            if (parameterType != null)
            {
                value = Conversions.ChangeType(value, parameterType, null, culture);
            }

            var convertedValue = targetType == null ? value : Conversions.ChangeType(value, targetType, null, culture);
            return convertedValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var convertedValue = targetType == null ? value : Conversions.ChangeType(value, targetType, null, culture);
            return convertedValue;
        }
    }
}
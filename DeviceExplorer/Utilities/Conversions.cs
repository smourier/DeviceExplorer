using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace DeviceExplorer.Utilities
{
    public static class Conversions
    {
        public static string ToHex(this int i) => "0x" + i.ToString("X8");
        public static string ToHex(this int? i) => i.HasValue ? "0x" + i.Value.ToString("X8") : null;
        public static string ToHex(this long? i) => i.HasValue ? "0x" + i.Value.ToString("X16") : null;
        public static string ToHex(this long i) => "0x" + i.ToString("X16");

        public static string GetAllMessagesWithDots(this Exception exception) => GetAllMessages(exception, s => s?.EndsWith(".") == true ? null : ". ");
        public static string GetAllMessages(this Exception exception, string separator) => GetAllMessages(exception, s => separator);
        public static string GetAllMessages(this Exception exception) => GetAllMessages(exception, s => Environment.NewLine);
        public static string GetAllMessages(this Exception exception, Func<string, string> separator)
        {
            if (exception == null)
                return null;

            var sb = new StringBuilder();
            AppendMessages(sb, exception, separator);
            return sb.ToString().Replace("..", ".").Nullify();
        }

        private static void AppendMessages(StringBuilder sb, Exception e, Func<string, string> separator)
        {
            if (e == null)
                return;

            if (e is AggregateException agg)
            {
                foreach (var ex in agg.InnerExceptions)
                {
                    AppendMessages(sb, ex, separator);
                }
                return;
            }

            if (e is not TargetInvocationException)
            {
                if (sb.Length > 0 && separator != null)
                {
                    var sep = separator(sb.ToString());
                    if (sep != null)
                    {
                        sb.Append(sep);
                    }
                }
                sb.Append(e.Message);
            }
            AppendMessages(sb, e.InnerException, separator);
        }

        public static bool IsFlagsEnum(this Type type) => type != null && type.IsEnum && type.IsDefined(typeof(FlagsAttribute), true);

        public static IEnumerable<Exception> EnumerateAllExceptions(this Exception exception)
        {
            if (exception == null)
                yield break;

            yield return exception;
            if (exception is AggregateException agg)
            {
                foreach (var ae in agg.InnerExceptions)
                {
                    foreach (var child in EnumerateAllExceptions(ae))
                    {
                        yield return child;
                    }
                }
            }
            else
            {
                if (exception.InnerException != null)
                {
                    foreach (var child in EnumerateAllExceptions(exception.InnerException))
                    {
                        yield return child;
                    }
                }
            }
        }

        public static bool EqualsIgnoreCase(this string thisString, string text, bool trim = false)
        {
            if (trim)
            {
                thisString = thisString.Nullify();
                text = text.Nullify();
            }

            if (thisString == null)
                return text == null;

            if (text == null)
                return false;

            if (thisString.Length != text.Length)
                return false;

            return string.Compare(thisString, text, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string Nullify(this string text)
        {
            if (text == null)
                return null;

            if (string.IsNullOrWhiteSpace(text))
                return null;

            var t = text.Trim();
            return t.Length == 0 ? null : t;
        }

        public static string JoinWithDots(params object[] texts) => JoinWithDots(texts?.Select(t => t?.ToString()));
        public static string JoinWithDots(this IEnumerable<string> texts)
        {
            if (texts == null)
                return null;

            var sb = new StringBuilder();
            foreach (var text in texts)
            {
                var t = text;
                if (t == null)
                    continue;

                while (t.StartsWith("."))
                {
                    t = t.Substring(1);
                }

                t = t.Nullify();
                if (t == null)
                    continue;

                if (sb.Length > 0)
                {
                    sb.Append(". ");
                }
                sb.Append(t);
            }
            return sb.ToString().Nullify();
        }

        public static string ComputeHashString(string text) => ComputeGuidHash(text).ToString("N");
        public static Guid ComputeGuidHash(string text)
        {
            if (text == null)
                return Guid.Empty;

            using var md5 = MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }

        public static bool AreDictionaryEquals<T>(this IDictionary<string, T> dic1, IDictionary<string, T> dic2, IEqualityComparer<T> comparer = null)
        {
            if (ReferenceEquals(dic1, dic2))
                return true;

            if (dic1 == null)
                return dic1 == null;

            if (dic2 == null)
                return false;

            if (dic1.Count != dic2.Count)
                return false;

            comparer ??= EqualityComparer<T>.Default;
            foreach (var kv in dic1)
            {
                if (!dic2.TryGetValue(kv.Key, out var value))
                    return false;

                if (!comparer.Equals(kv.Value, value))
                    return false;
            }

            foreach (var kv in dic2)
            {
                if (!dic1.TryGetValue(kv.Key, out var value))
                    return false;

                if (!comparer.Equals(kv.Value, value))
                    return false;
            }
            return true;
        }

        public static new bool Equals(object o1, object o2)
        {
            if (o1 == null)
                return o2 == null;

            if (o2 == null)
                return false;

            if (object.Equals(o1, o2))
                return true;

            if (TryChangeType(o2, o1.GetType(), out var co2) && o1.Equals(co2))
                return true;

            if (TryChangeType(o1, o2.GetType(), out var co1) && o2.Equals(co1))
                return true;

            return false;
        }

        public static CultureInfo ParseCultureInfo(string language, CultureInfo defaultValue = null)
        {
            var culture = defaultValue ?? CultureInfo.CurrentCulture;
            language = language.Nullify();
            if (language != null)
            {
                try
                {
                    if (int.TryParse(language, out var lcid))
                    {
                        culture = CultureInfo.GetCultureInfo(lcid);
                    }
                    else
                    {
                        culture = CultureInfo.GetCultureInfo(language);
                    }
                }
                catch
                {
                }
            }
            return culture;
        }

        private static readonly char[] _enumSeparators = new char[] { ',', ';', '+', '|', ' ' };

        public static bool TryParseEnum(Type type, object input, out object value)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (!type.IsEnum)
                throw new ArgumentException(null, nameof(type));

            if (input == null)
            {
                value = Activator.CreateInstance(type);
                return false;
            }

            var stringInput = string.Format(CultureInfo.InvariantCulture, "{0}", input);
            stringInput = stringInput.Nullify();
            if (stringInput == null)
            {
                value = Activator.CreateInstance(type);
                return false;
            }

            if (stringInput.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (ulong.TryParse(stringInput.AsSpan(2), NumberStyles.HexNumber, null, out ulong ulx))
                {
                    value = ToEnum(ulx.ToString(CultureInfo.InvariantCulture), type);
                    return true;
                }
            }

            var names = Enum.GetNames(type);
            if (names.Length == 0)
            {
                value = Activator.CreateInstance(type);
                return false;
            }

            var values = Enum.GetValues(type);
            // some enums like System.CodeDom.MemberAttributes *are* flags but are not declared with Flags...
            if (!type.IsDefined(typeof(FlagsAttribute), true) && stringInput.IndexOfAny(_enumSeparators) < 0)
                return StringToEnum(type, names, values, stringInput, out value);

            // multi value enum
            var tokens = stringInput.Split(_enumSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                value = Activator.CreateInstance(type);
                return false;
            }

            ulong ul = 0;
            foreach (var tok in tokens)
            {
                var token = tok.Nullify(); // NOTE: we don't consider empty tokens as errors
                if (token == null)
                    continue;

                if (!StringToEnum(type, names, values, token, out object tokenValue))
                {
                    value = Activator.CreateInstance(type);
                    return false;
                }

                ulong tokenUl;
#pragma warning disable IDE0066 // Convert switch statement to expression
                switch (Convert.GetTypeCode(tokenValue))
                {
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.SByte:
                        tokenUl = (ulong)Convert.ToInt64(tokenValue, CultureInfo.InvariantCulture);
                        break;

                    default:
                        tokenUl = Convert.ToUInt64(tokenValue, CultureInfo.InvariantCulture);
                        break;
                }
#pragma warning restore IDE0066 // Convert switch statement to expression

                ul |= tokenUl;
            }
            value = Enum.ToObject(type, ul);
            return true;
        }

        public static object ToEnum(string text, Type enumType)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            TryParseEnum(enumType, text, out object value);
            return value;
        }

        private static bool StringToEnum(Type type, string[] names, Array values, string input, out object value)
        {
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i].EqualsIgnoreCase(input))
                {
                    value = values.GetValue(i);
                    return true;
                }
            }

            for (var i = 0; i < values.GetLength(0); i++)
            {
                var valuei = values.GetValue(i);
                if (input.Length > 0 && input[0] == '-')
                {
                    var ul = (long)EnumToUInt64(valuei);
                    if (ul.ToString().EqualsIgnoreCase(input))
                    {
                        value = valuei;
                        return true;
                    }
                }
                else
                {
                    var ul = EnumToUInt64(valuei);
                    if (ul.ToString().EqualsIgnoreCase(input))
                    {
                        value = valuei;
                        return true;
                    }
                }
            }

            if (char.IsDigit(input[0]) || input[0] == '-' || input[0] == '+')
            {
                var obj = EnumToObject(type, input);
                if (obj == null)
                {
                    value = Activator.CreateInstance(type);
                    return false;
                }
                value = obj;
                return true;
            }

            value = Activator.CreateInstance(type);
            return false;
        }

        public static object EnumToObject(Type enumType, object value)
        {
            if (enumType == null)
                throw new ArgumentNullException(nameof(enumType));

            if (!enumType.IsEnum)
                throw new ArgumentException(null, nameof(enumType));

            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var underlyingType = Enum.GetUnderlyingType(enumType);
            if (underlyingType == typeof(long))
                return Enum.ToObject(enumType, ChangeType<long>(value));

            if (underlyingType == typeof(ulong))
                return Enum.ToObject(enumType, ChangeType<ulong>(value));

            if (underlyingType == typeof(int))
                return Enum.ToObject(enumType, ChangeType<int>(value));

            if ((underlyingType == typeof(uint)))
                return Enum.ToObject(enumType, ChangeType<uint>(value));

            if (underlyingType == typeof(short))
                return Enum.ToObject(enumType, ChangeType<short>(value));

            if (underlyingType == typeof(ushort))
                return Enum.ToObject(enumType, ChangeType<ushort>(value));

            if (underlyingType == typeof(byte))
                return Enum.ToObject(enumType, ChangeType<byte>(value));

            if (underlyingType == typeof(sbyte))
                return Enum.ToObject(enumType, ChangeType<sbyte>(value));

            throw new ArgumentException(null, nameof(enumType));
        }

        public static ulong EnumToUInt64(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var typeCode = Convert.GetTypeCode(value);
#pragma warning disable IDE0066 // Convert switch statement to expression
            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return (ulong)Convert.ToInt64(value, CultureInfo.InvariantCulture);

                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return Convert.ToUInt64(value, CultureInfo.InvariantCulture);

                case TypeCode.String:
                default:
                    return ChangeType<ulong>(value, 0, CultureInfo.InvariantCulture);
            }
#pragma warning restore IDE0066 // Convert switch statement to expression
        }

        public static object ChangeType(object input, Type conversionType, object defaultValue = null, IFormatProvider provider = null)
        {
            if (!TryChangeType(input, conversionType, provider, out object value))
                return defaultValue;

            return value;
        }

        public static T ChangeType<T>(object input, T defaultValue = default, IFormatProvider provider = null)
        {
            if (!TryChangeType(input, provider, out T value))
                return defaultValue;

            return value;
        }

        public static bool TryChangeType<T>(object input, out T value) => TryChangeType(input, null, out value);
        public static bool TryChangeType<T>(object input, IFormatProvider provider, out T value)
        {
            if (!TryChangeType(input, typeof(T), provider, out object tvalue))
            {
                value = default;
                return false;
            }

            value = (T)tvalue;
            return true;
        }

        public static bool TryChangeType(object input, Type conversionType, out object value) => TryChangeType(input, conversionType, null, out value);
        public static bool TryChangeType(object input, Type conversionType, IFormatProvider provider, out object value)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            if (conversionType == typeof(object))
            {
                value = input;
                return true;
            }

            value = conversionType.IsValueType ? Activator.CreateInstance(conversionType) : null;
            if (input == null)
                return !conversionType.IsValueType;

            var inputType = input.GetType();
            if (conversionType.IsAssignableFrom(inputType))
            {
                value = input;
                return true;
            }

            if (conversionType.IsEnum)
                return TryParseEnum(conversionType, input, out value);

            if (conversionType == typeof(Guid))
            {
                var svalue = string.Format(provider, "{0}", input).Nullify();
                if (svalue != null && Guid.TryParse(svalue, out Guid guid))
                {
                    value = guid;
                    return true;
                }
                return false;
            }

            if (conversionType == typeof(Type))
            {
                var typeName = string.Format(provider, "{0}", input).Nullify();
                if (typeName == null)
                    return false;

                var type = Type.GetType(typeName, false);
                if (type == null)
                    return false;

                value = type;
                return true;
            }

            if (conversionType == typeof(IntPtr))
            {
                if (IntPtr.Size == 8)
                {
                    if (TryChangeType(input, provider, out long l))
                    {
                        value = new IntPtr(l);
                        return true;
                    }
                }
                else if (TryChangeType(input, provider, out int i))
                {
                    value = new IntPtr(i);
                    return true;
                }
                return false;
            }

            if (conversionType == typeof(int))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((int)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((int)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((int)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((int)(byte)input);
                    return true;
                }
            }

            if (conversionType == typeof(long))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((long)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((long)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((long)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((long)(byte)input);
                    return true;
                }
            }

            if (conversionType == typeof(short))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((short)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((short)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((short)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((short)(byte)input);
                    return true;
                }
            }

            if (conversionType == typeof(sbyte))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((sbyte)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((sbyte)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((sbyte)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((sbyte)(byte)input);
                    return true;
                }
            }

            if (conversionType == typeof(uint))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((uint)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((uint)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((uint)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((uint)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(ulong))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((ulong)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((ulong)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((ulong)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((ulong)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(ushort))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((ushort)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((ushort)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((ushort)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((ushort)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(byte))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((byte)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((byte)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((byte)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((byte)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(DateTime))
            {
                if (input is double dbl)
                {
                    try
                    {
                        value = DateTime.FromOADate(dbl);
                        return true;
                    }
                    catch
                    {
                        value = DateTime.MinValue;
                        return false;
                    }
                }
            }

            if (conversionType == typeof(DateTimeOffset))
            {
                if (input is double dbl)
                {
                    try
                    {
                        value = new DateTimeOffset(DateTime.FromOADate(dbl));
                        return true;
                    }
                    catch
                    {
                        value = DateTimeOffset.MinValue;
                        return false;
                    }
                }
            }

            var nullable = conversionType.IsGenericType && conversionType.GetGenericTypeDefinition() == typeof(Nullable<>);
            if (nullable)
            {
                if (input == null || string.Empty.Equals(input))
                {
                    value = null;
                    return true;
                }

                var type = conversionType.GetGenericArguments()[0];
                if (TryChangeType(input, type, provider, out var vtValue))
                {
                    var nullableType = typeof(Nullable<>).MakeGenericType(type);
                    value = Activator.CreateInstance(nullableType, vtValue);
                    return true;
                }

                value = null;
                return false;
            }

            if (input is IConvertible convertible)
            {
                try
                {
                    value = convertible.ToType(conversionType, provider);
                    return true;
                }
                catch
                {
                    // do nothing
                    return false;
                }
            }

            return false;
        }

        public static T GetValue<T>(this IDictionary<string, object> dictionary, string key, T defaultValue = default, IFormatProvider provider = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (dictionary == null)
                return defaultValue;

            if (!dictionary.TryGetValue(key, out object o))
                return defaultValue;

            return ChangeType(o, defaultValue, provider);
        }

        public static string GetNullifiedValue(this IDictionary<string, string> dictionary, string key, string defaultValue = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (dictionary == null)
                return defaultValue;

            if (!dictionary.TryGetValue(key, out string str))
                return defaultValue;

            return str.Nullify();
        }

        public static string GetNullifiedValue(this IDictionary<string, object> dictionary, string key, IFormatProvider provider = null, string defaultValue = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (dictionary == null)
                return defaultValue;

            if (!dictionary.TryGetValue(key, out var obj))
                return defaultValue;

            if (obj == null)
                return null;

            if (obj is string s)
                return s.Nullify();

            if (provider == null)
                return string.Format("{0}", obj).Nullify();

            return string.Format(provider, "{0}", obj).Nullify();
        }

        public static T GetValue<T>(this IDictionary<string, string> dictionary, string key, T defaultValue = default, IFormatProvider provider = null)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (dictionary == null)
                return defaultValue;

            if (!dictionary.TryGetValue(key, out string str))
                return defaultValue;

            return ChangeType(str, defaultValue, provider);
        }
    }
}

using System.Collections.Concurrent;
using System.Reflection;
using Google.Protobuf.Reflection;

namespace Backend.Common.ChirpStack;

public static class ProtoEnumMapper<TEnum> where TEnum : struct, Enum
{
    private static readonly Dictionary<string, TEnum> _byName = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<TEnum, string> _byValue = new();

    static ProtoEnumMapper()
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var enumValue = (TEnum)field.GetValue(null)!;

            var attr = field.GetCustomAttribute<OriginalNameAttribute>();
            var originalName = attr?.Name ?? field.Name;

            _byName[originalName] = enumValue;
            _byValue[enumValue] = originalName;
        }
    }

    public static TEnum FromOriginalName(string originalName)
    {
        if (_byName.TryGetValue(originalName, out var enumValue))
            return enumValue;


        throw new ArgumentException(
            $"No matching enum value found for original name '{originalName}' in {typeof(TEnum).Name}." +
            $" Available names: {string.Join(", ", _byName.Keys)}");
    }

    public static string ToOriginalName(TEnum value)
    {
        return _byValue.TryGetValue(value, out var name) ? name : value.ToString();
    }
}
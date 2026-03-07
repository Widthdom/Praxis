using System.Reflection;

namespace Praxis.Core.Logic;

public static class NonPublicPropertySetter
{
    public static bool TrySet(object? target, string propertyName, object? value)
    {
        if (target is null || string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property is null || !property.CanWrite || property.GetIndexParameters().Length != 0)
        {
            return false;
        }

        if (value is not null)
        {
            var targetType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (!targetType.IsAssignableFrom(value.GetType()))
            {
                return false;
            }
        }

        try
        {
            property.SetValue(target, value);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

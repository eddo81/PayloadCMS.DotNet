using Payload.CMS.Internal.Attributes;
using System.Reflection;

namespace Payload.CMS.Internal.Utils;
internal static class EnumExtensions
{
    public static string ToStringValue(this Enum value)
    {
        var type = value.GetType();
        var member = type.GetMember(value.ToString(), MemberTypes.Field, BindingFlags.Public | BindingFlags.Static);

        if (member.Length > 0)
        {
            var attribute = member[0].GetCustomAttribute<StringValueAttribute>();

            if (attribute != null)
            {
                return attribute.Value;
            }
        }

        return value.ToString();
    }
}

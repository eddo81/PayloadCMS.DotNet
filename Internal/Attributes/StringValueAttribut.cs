namespace Payload.CMS.Internal.Attributes;

using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class StringValueAttribute : Attribute
{
    public string Value { get; }

    public StringValueAttribute(string value)
    {
        Value = value;
    }
}

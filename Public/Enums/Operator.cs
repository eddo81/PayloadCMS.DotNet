using PayloadCMS.DotNet.Internal.Attributes;

namespace PayloadCMS.DotNet.Enums;

/// <summary>Comparison operators for Payload CMS <c>where</c> query clauses.</summary>
public enum Operator
{
    /// <summary>Field value equals the given value.</summary>
    [StringValue("equals")]
    Equals,

    /// <summary>Field value contains the given string.</summary>
    [StringValue("contains")]
    Contains,

    /// <summary>Field value does not equal the given value.</summary>
    [StringValue("not_equals")]
    NotEquals,

    /// <summary>Field value is one of the given values.</summary>
    [StringValue("in")]
    In,

    /// <summary>Field array contains all of the given values.</summary>
    [StringValue("all")]
    All,

    /// <summary>Field value is not one of the given values.</summary>
    [StringValue("not_in")]
    NotIn,

    /// <summary>Field value exists (is not null).</summary>
    [StringValue("exists")]
    Exists,

    /// <summary>Field value is greater than the given value.</summary>
    [StringValue("greater_than")]
    GreaterThan,

    /// <summary>Field value is greater than or equal to the given value.</summary>
    [StringValue("greater_than_equal")]
    GreaterThanEqual,

    /// <summary>Field value is less than the given value.</summary>
    [StringValue("less_than")]
    LessThan,

    /// <summary>Field value is less than or equal to the given value.</summary>
    [StringValue("less_than_equal")]
    LessThanEqual,

    /// <summary>Field value matches the given pattern (partial match).</summary>
    [StringValue("like")]
    Like,

    /// <summary>Field value does not match the given pattern.</summary>
    [StringValue("not_like")]
    NotLike,

    /// <summary>Geo: point is within the given polygon.</summary>
    [StringValue("within")]
    Within,

    /// <summary>Geo: geometry intersects the given geometry.</summary>
    [StringValue("intersects")]
    Intersects,

    /// <summary>Geo: point is near the given coordinates.</summary>
    [StringValue("near")]
    Near,
}

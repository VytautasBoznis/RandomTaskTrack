using System.Data;
using Dapper;

namespace RandomTaskTrack.Business.Base;

/// <summary>
/// Dapper targets netstandard2.0, so DateOnly and TimeOnly are absent from its
/// type map and any parameter of those types fails with "cannot be used as a
/// parameter value". Npgsql maps both natively, so the handler only has to hand
/// the value over untouched.
///
/// Registering a handler also routes *reads* of that type through Parse.
/// </summary>
public class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.Value = value;
    }

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly date => date,
        DateTime timestamp => DateOnly.FromDateTime(timestamp),
        _ => throw new DataException($"Cannot convert {value.GetType()} to DateOnly")
    };
}

/// <inheritdoc cref="DateOnlyTypeHandler"/>
public class TimeOnlyTypeHandler : SqlMapper.TypeHandler<TimeOnly>
{
    public override void SetValue(IDbDataParameter parameter, TimeOnly value)
    {
        parameter.Value = value;
    }

    public override TimeOnly Parse(object value) => value switch
    {
        TimeOnly time => time,
        TimeSpan span => TimeOnly.FromTimeSpan(span),
        _ => throw new DataException($"Cannot convert {value.GetType()} to TimeOnly")
    };
}

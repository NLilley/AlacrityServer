using Dapper;
using System.Data;

public class DateTimeHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
    }

    public override DateTime Parse(object value)
    {
        if (value is string str)
            value = DateTime.Parse(str);

        return DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
    }
}
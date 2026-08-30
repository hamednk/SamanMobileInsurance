using System.Globalization;

namespace SamanMobileInsurance.Application.Common;

public static class IranDateTime
{
    public static TimeZoneInfo TehranTimeZone { get; } = ResolveTehran();

    public static DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    public static DateTimeOffset TehranNow => TimeZoneInfo.ConvertTime(UtcNow, TehranTimeZone);

    public static DateOnly TehranToday => DateOnly.FromDateTime(TehranNow.DateTime);

    public static int JalaliYear(DateTimeOffset utc)
    {
        var local = TimeZoneInfo.ConvertTime(utc, TehranTimeZone).DateTime;
        return new PersianCalendar().GetYear(local);
    }

    public static string ToJalaliDate(DateTimeOffset utc)
    {
        var local = TimeZoneInfo.ConvertTime(utc, TehranTimeZone).DateTime;
        var pc = new PersianCalendar();
        return $"{pc.GetYear(local):0000}/{pc.GetMonth(local):00}/{pc.GetDayOfMonth(local):00}";
    }

    public static string ToJalaliDate(DateOnly date)
    {
        var (year, month, day) = ToJalaliParts(date);
        return $"{year:0000}/{month:00}/{day:00}";
    }

    public static (int Year, int Month, int Day) ToJalaliParts(DateOnly date)
    {
        var dt = date.ToDateTime(TimeOnly.MinValue);
        var pc = new PersianCalendar();
        return (pc.GetYear(dt), pc.GetMonth(dt), pc.GetDayOfMonth(dt));
    }

    private static TimeZoneInfo ResolveTehran()
    {
        foreach (var id in new[] { "Iran Standard Time", "Asia/Tehran" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.CreateCustomTimeZone("Asia/Tehran", TimeSpan.FromHours(3.5), "Iran Standard Time", "Iran Standard Time");
    }
}

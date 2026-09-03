using System.Globalization;

namespace Api.Helpers;

//Time here is weird, it is tracked until 26:00 (2 hours after midnight on the service day) this makes time values in some places useless
//So time is transfered as seconds since service date start and then translated accordingly wherever we need dates and times
public static class GtfsDateTimeHelper
{
    public static readonly TimeZoneInfo MetraTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
    public const string IsoDateFormat = "yyyy-MM-dd";
    private const string GtfsDateFormat = "yyyyMMdd";
    private const string PublishedFormat = "MM/dd/yy hh:mm:ss tt";
    private const string PublishedZoneSuffix = "America/Chicago";
    private const int MaxServiceSeconds = 108000;
    
    //GTFS "20260816" → DateOnly
    public static DateOnly ParseServiceDate(string yyyymmdd) => DateOnly.ParseExact(yyyymmdd, GtfsDateFormat, CultureInfo.InvariantCulture);

    //DateOnly → "2026-08-16"
    public static string ToIsoDate(DateOnly date) => date.ToString(IsoDateFormat, CultureInfo.InvariantCulture);
    
    //GTFS "4:00:01" / "26:08:00" → seconds since the start of the service day.
    public static int ParseServiceTimeToSeconds(string gtfsTime)
    {
        var parts = gtfsTime.Split(':');

        if (parts.Length != 3
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
            || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            || minutes > 59 || seconds > 59)
        {
            throw new FormatException($"Invalid GTFS time '{gtfsTime}'");
        }

        var total = hours * 3600 + minutes * 60 + seconds;

        if (total > MaxServiceSeconds)
        {
            throw new FormatException($"GTFS time '{gtfsTime}' exceeds {MaxServiceSeconds} seconds");   
        }

        return total;
    }

    //Parse published.txt into a usable DateTime value
    public static DateTime? ParsePublishedTimestamp(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var text = body.Trim();

        if (text.EndsWith(PublishedZoneSuffix, StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^PublishedZoneSuffix.Length].TrimEnd();
        }
        
        if (!DateTime.TryParseExact(text, PublishedFormat, CultureInfo.InvariantCulture,DateTimeStyles.None, out var local))
        {
            return null;
        }

        return TimeZoneInfo.ConvertTimeToUtc(local, MetraTimeZone);
    }

    
    //Service date + service seconds → UTC instant
    public static DateTime ComposeUtcInstant(DateOnly serviceDate, int departureSeconds)
    {
        var localNoon = serviceDate.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Unspecified);
        var utcNoon = TimeZoneInfo.ConvertTimeToUtc(localNoon, MetraTimeZone);
        return utcNoon.AddHours(-12).AddSeconds(departureSeconds);
    }


    public static DateOnly TodayInChicago(DateTime utcNow)
    {
      return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTimeHelper.NormalizeToUtc(utcNow), MetraTimeZone));  
    } 
    
    public static TimeSpan DelayUntilNextChicagoTime(TimeOnly localTime, DateTime utcNow)
    {
        utcNow = DateTimeHelper.NormalizeToUtc(utcNow);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, MetraTimeZone);
        var candidate = localNow.Date + localTime.ToTimeSpan();

        if (candidate <= localNow)
        {
            candidate = candidate.AddDays(1);
        }
        
        return TimeZoneInfo.ConvertTimeToUtc(candidate, MetraTimeZone) - utcNow;
    }
}

using System.Globalization;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;

namespace Api.Gtfs;

public static class GtfsFeedReader
{
    private static readonly CsvConfiguration Config = new(CultureInfo.InvariantCulture)
    {
        Delimiter = ",",
        TrimOptions = TrimOptions.Trim,
        PrepareHeaderForMatch = args => args.Header.Trim()
    };

    private const string CalendarFile = "calendar.txt";
    private const string CalendarDatesFile = "calendar_dates.txt";
    private const string TripsFile = "trips.txt";
    private const string StopTimesFile = "stop_times.txt";

    public static GtfsFeed ReadFromZip(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        return Read(name => zip.GetEntry(name)?.Open() ?? throw new InvalidDataException($"zip is missing '{name}'"));
    }


    public static GtfsFeed ReadFromDirectory(string folderPath)
    {
        return Read(name =>
        {
            var path = Path.Combine(folderPath, name);
            return File.Exists(path) ? File.OpenRead(path) : throw new InvalidDataException($"GTFS folder is missing '{name}'");
        });
    }

    private static GtfsFeed Read(Func<string, Stream> open)
    {
        return new GtfsFeed
        {
            Calendars = ReadFile<GtfsCalendar>(open, CalendarFile),
            CalendarDates = ReadFile<GtfsCalendarDate>(open, CalendarDatesFile),
            Trips = ReadFile<GtfsTrip>(open, TripsFile),
            StopTimes = ReadFile<GtfsStopTime>(open, StopTimesFile)
        };
    }

    private static List<T> ReadFile<T>(Func<string, Stream> open, string name)
    {
        using var stream = open(name);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, Config);
        return csv.GetRecords<T>().ToList();
    }
}

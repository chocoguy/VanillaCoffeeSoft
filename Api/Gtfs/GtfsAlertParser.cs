using System.Security.Cryptography;
using System.Text;
using TransitRealtime;
using Alert = Model.BlueTrains.Alert;

namespace Api.Gtfs;

public static class GtfsAlertParser
{
    private const string PreferredLanguage = "en";

    public static List<Alert> Parse(FeedMessage feed, long observedAt, Action<string> warn)
    {
        var alerts = new List<Alert>();
        var seenEntityIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entity in feed.Entity)
        {
            var alert = entity.Alert;

            if (alert == null)
                continue;

            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                warn("alert entity has no id, skipping");
                continue;
            }

            var header = SelectText(alert.HeaderText);
            if (string.IsNullOrWhiteSpace(header))
            {
                warn($"alert '{entity.Id}' has no header text, skipping");
                continue;
            }
            
            if (alert.InformedEntity.Count != 1)
                warn($"alert '{entity.Id}' has {alert.InformedEntity.Count} informed_entity entries, using the first");

            var informed = alert.InformedEntity.FirstOrDefault();
            var routeId = NullIfBlank(informed?.RouteId);
            var description = NullIfBlank(SelectText(alert.DescriptionText));

            if (!seenEntityIds.Add(entity.Id))
            {
                warn($"alert '{entity.Id}' appears more than once in this feed, keeping the first");
                continue;
            }

            alerts.Add(new Alert
            {
                FeedEntityId = entity.Id,
                StaticRouteId = routeId,
                AgencyId = NullIfBlank(informed?.AgencyId),
                HeaderText = header,
                DescriptionText = description,
                Url = NullIfBlank(SelectText(alert.Url)),
                Cause = (int)alert.Cause,
                Effect = (int)alert.Effect,
                ContentHash = ContentHash(routeId, header, description),
                FirstSeen = observedAt,
                LastSeen = observedAt
            });
        }

        return alerts;
    }
    
    private static string ContentHash(string? routeId, string header, string? description)
    {
        var payload = $"{routeId}{header}{description}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
    
    private static string? SelectText(TranslatedString? translated)
    {
        if (translated == null || translated.Translation.Count == 0)
            return null;

        var english = translated.Translation.FirstOrDefault(t =>
            t.Language.StartsWith(PreferredLanguage, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(t.Text));

        return (english ?? translated.Translation.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.Text)))?.Text;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}

namespace CCE.Application.Content.Public;

public static class IcsBuilder
{
    public static string ToIcs(Dtos.PublicEventDto evt)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("BEGIN:VCALENDAR\r\n");
        sb.Append("VERSION:2.0\r\n");
        sb.Append("PRODID:-//CCE Knowledge Center//EN\r\n");
        sb.Append("CALSCALE:GREGORIAN\r\n");
        sb.Append("METHOD:PUBLISH\r\n");
        sb.Append("BEGIN:VEVENT\r\n");
        sb.Append("UID:").Append(evt.ICalUid).Append("\r\n");
        sb.Append("DTSTAMP:").Append(System.DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture)).Append("\r\n");
        sb.Append("DTSTART:").Append(evt.StartsOn.UtcDateTime.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture)).Append("\r\n");
        sb.Append("DTEND:").Append(evt.EndsOn.UtcDateTime.ToString("yyyyMMddTHHmmssZ", System.Globalization.CultureInfo.InvariantCulture)).Append("\r\n");
        sb.Append("SUMMARY:").Append(EscapeIcs(evt.TitleEn)).Append("\r\n");
        sb.Append("DESCRIPTION:").Append(EscapeIcs(evt.DescriptionEn)).Append("\r\n");
        if (!string.IsNullOrEmpty(evt.LocationEn))
        {
            sb.Append("LOCATION:").Append(EscapeIcs(evt.LocationEn)).Append("\r\n");
        }
        if (!string.IsNullOrEmpty(evt.OnlineMeetingUrl))
        {
            sb.Append("URL:").Append(evt.OnlineMeetingUrl).Append("\r\n");
        }
        sb.Append("END:VEVENT\r\n");
        sb.Append("END:VCALENDAR\r\n");
        return sb.ToString();
    }

    private static string EscapeIcs(string input) =>
        (input ?? string.Empty)
            .Replace("\\", "\\\\", System.StringComparison.Ordinal)
            .Replace(",", "\\,", System.StringComparison.Ordinal)
            .Replace(";", "\\;", System.StringComparison.Ordinal)
            .Replace("\n", "\\n", System.StringComparison.Ordinal);
}

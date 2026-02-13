using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PlayCutWin.Models;

namespace PlayCutWin.Services;

public interface ICsvClipService
{
    Task ExportAsync(string path, IEnumerable<Clip> clips);
    Task<List<Clip>> ImportAsync(string path);
}

/// <summary>
/// CSV schema is intentionally tolerant so we can import files from the Mac app and older Windows builds.
/// Supported headers (case-insensitive):
/// - id / clipid
/// - startseconds / start / start_sec / starttime
/// - endseconds / end / end_sec / endtime
/// - team / clipteam / side (A/B/Home/Away)
/// - tags / tag (semicolon or comma separated)
/// - note / comment (ignored for now)
/// Time values can be seconds ("12.34") or timecode ("mm:ss", "hh:mm:ss", "mm:ss.fff").
/// </summary>
public sealed class CsvClipService : ICsvClipService
{
    private sealed class ClipRow
    {
        public string Id { get; set; } = "";
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }
        public string Team { get; set; } = "TeamA";
        public string Tags { get; set; } = "";
    }

    public async Task ExportAsync(string path, IEnumerable<Clip> clips)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            NewLine = Environment.NewLine,
        };

        await using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
        await using var csv = new CsvWriter(writer, config);

        var rows = clips.Select(c => new ClipRow
        {
            Id = c.Id.ToString(),
            StartSeconds = c.StartSeconds,
            EndSeconds = c.EndSeconds,
            Team = c.Team.ToString(),
            Tags = string.Join(";", c.Tags)
        });

        await csv.WriteRecordsAsync(rows);
    }

    public async Task<List<Clip>> ImportAsync(string path)
    {
        // Try UTF-8 first; if the file is not UTF-8 (common in Japan), fall back to Shift-JIS.
        // (StreamReader with detectEncodingFromByteOrderMarks=true handles UTF-8 BOM)
        StreamReader OpenReader()
        {
            try
            {
                return new StreamReader(path, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            }
            catch
            {
                // Fallback for legacy exports
                return new StreamReader(path, System.Text.Encoding.GetEncoding("shift_jis"), detectEncodingFromByteOrderMarks: true);
            }
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            DetectDelimiter = true
        };

        using var reader = OpenReader();
        using var csv = new CsvReader(reader, config);

        // Read header
        if (!await csv.ReadAsync()) return new List<Clip>();
        csv.ReadHeader();
        var headers = (csv.HeaderRecord ?? Array.Empty<string>())
            .Select(h => (h ?? "").Trim())
            .ToArray();

        // Build header map (case-insensitive)
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var h in headers)
        {
            if (!map.ContainsKey(h)) map[h] = h;
        }

        string? FindHeader(params string[] candidates)
        {
            foreach (var c in candidates)
            {
                var hit = headers.FirstOrDefault(h => string.Equals(h, c, StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }

            // also allow contains-based match (e.g., "Start (sec)")
            foreach (var c in candidates)
            {
                var hit = headers.FirstOrDefault(h => h.Replace(" ", "", StringComparison.OrdinalIgnoreCase)
                    .Contains(c.Replace(" ", "", StringComparison.OrdinalIgnoreCase), StringComparison.OrdinalIgnoreCase));
                if (hit != null) return hit;
            }
            return null;
        }

        var hId = FindHeader("Id", "ClipId", "UUID");
        var hStart = FindHeader("StartSeconds", "Start", "StartSec", "Start_sec", "StartTime", "Start time");
        var hEnd = FindHeader("EndSeconds", "End", "EndSec", "End_sec", "EndTime", "End time");
        var hTeam = FindHeader("Team", "ClipTeam", "Side");
        var hTags = FindHeader("Tags", "Tag", "TagNames");

        double ParseTimeToSeconds(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;

            s = s.Trim();

            // plain number (seconds)
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
                return seconds;

            // timecode formats
            // allow "mm:ss", "hh:mm:ss", and fractional seconds
            if (TimeSpan.TryParseExact(s,
                    new[] { @"hh\:mm\:ss\.fff", @"hh\:mm\:ss", @"mm\:ss\.fff", @"mm\:ss" },
                    CultureInfo.InvariantCulture,
                    out var ts))
                return ts.TotalSeconds;

            // last attempt: replace comma with dot for decimal
            var normalized = s.Replace(",", ".");
            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds))
                return seconds;

            return 0;
        }

        ClipTeam ParseTeam(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return ClipTeam.TeamA;
            s = s.Trim();

            // direct enum parse (TeamA/TeamB)
            if (Enum.TryParse<ClipTeam>(s, ignoreCase: true, out var team))
                return team;

            // common exports
            if (string.Equals(s, "A", StringComparison.OrdinalIgnoreCase)) return ClipTeam.TeamA;
            if (string.Equals(s, "B", StringComparison.OrdinalIgnoreCase)) return ClipTeam.TeamB;

            if (s.Contains("Home", StringComparison.OrdinalIgnoreCase) || s.Contains("Our", StringComparison.OrdinalIgnoreCase))
                return ClipTeam.TeamA;
            if (s.Contains("Away", StringComparison.OrdinalIgnoreCase) || s.Contains("Opp", StringComparison.OrdinalIgnoreCase))
                return ClipTeam.TeamB;

            return ClipTeam.TeamA;
        }

        List<string> ParseTags(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new List<string>();

            // accept ";" or "," separated
            var raw = s.Replace("|", ";").Replace(",", ";");
            return raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();
        }

        var result = new List<Clip>();

        while (await csv.ReadAsync())
        {
            string? idStr = hId is null ? null : csv.GetField(hId);
            string? startStr = hStart is null ? null : csv.GetField(hStart);
            string? endStr = hEnd is null ? null : csv.GetField(hEnd);
            string? teamStr = hTeam is null ? null : csv.GetField(hTeam);
            string? tagsStr = hTags is null ? null : csv.GetField(hTags);

            var start = ParseTimeToSeconds(startStr);
            var end = ParseTimeToSeconds(endStr);

            if (end < start) (start, end) = (end, start);

            // Skip empty rows
            if (start == 0 && end == 0 && string.IsNullOrWhiteSpace(tagsStr) && string.IsNullOrWhiteSpace(teamStr))
                continue;

            var clip = new Clip
            {
                Id = Guid.TryParse(idStr, out var gid) ? gid : Guid.NewGuid(),
                StartSeconds = start,
                EndSeconds = end,
                Team = ParseTeam(teamStr),
                Tags = ParseTags(tagsStr)
            };

            result.Add(clip);
        }

        return result;
    }
}

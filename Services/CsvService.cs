using PlayCutWin.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace PlayCutWin.Services
{
    public static class CsvService
    {
        // Mac互換を壊しにくい順で列名候補を持つ
        private static readonly string[] ColVideoName = { "VideoName", "videoName", "video_name" };
        private static readonly string[] ColTeam = { "Team", "team" };
        private static readonly string[] ColStart = { "Start", "start", "StartTime", "startTime" };
        private static readonly string[] ColEnd = { "End", "end", "EndTime", "endTime" };
        private static readonly string[] ColTags = { "Tags", "tags" };
        private static readonly string[] ColClipNote = { "ClipNote", "clipNote", "Note", "note" };
        private static readonly string[] ColSetPlay = { "SetPlay", "setPlay", "SetPlayName", "setPlayName" };

        public static void Export(string path, string videoName, IEnumerable<Clip> clips)
        {
            // UTF-8(BOM付き)にしてExcel/日本語事故を減らす
            using var sw = new StreamWriter(path, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            // “BBVideoTagger寄り”のヘッダ（不足が出にくい）
            sw.WriteLine(string.Join(",", new[]
            {
                "VideoName","Team","Start","End","Tags","ClipNote","SetPlay"
            }));

            foreach (var c in clips)
            {
                sw.WriteLine(string.Join(",", new[]
                {
                    Q(videoName),
                    Q(NormalizeTeam(c.Team)),
                    Q(c.StartDisplay),
                    Q(c.EndDisplay),
                    Q(c.Tags ?? ""),
                    Q(c.ClipNote ?? ""),
                    Q(c.SetPlay ?? "")
                }));
            }
        }

        public static (string csvVideoName, List<Clip> clips) Import(string path)
        {
            var lines = File.ReadAllLines(path, DetectEncoding(path)).ToList();
            if (lines.Count == 0) return ("", new List<Clip>());

            // 先頭行ヘッダ判定：VideoName/Startなどが含まれればヘッダ扱い
            var header = SplitCsvLine(lines[0]);
            bool hasHeader = header.Any(h => EqualsAny(h, ColStart) || EqualsAny(h, ColVideoName));

            int startIndex = hasHeader ? 1 : 0;
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (hasHeader)
            {
                for (int i = 0; i < header.Count; i++)
                    map[header[i].Trim()] = i;
            }

            string csvVideoName = "";
            var result = new List<Clip>();

            for (int i = startIndex; i < lines.Count; i++)
            {
                var row = SplitCsvLine(lines[i]);
                if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace)) continue;

                string video = Get(row, map, ColVideoName);
                if (!string.IsNullOrWhiteSpace(video) && string.IsNullOrWhiteSpace(csvVideoName))
                    csvVideoName = video;

                string team = NormalizeTeam(Get(row, map, ColTeam));
                string start = Get(row, map, ColStart);
                string end = Get(row, map, ColEnd);

                var clip = new Clip
                {
                    Team = team,
                    StartSeconds = ParseTimeToSeconds(start),
                    EndSeconds = ParseTimeToSeconds(end),
                    Tags = Get(row, map, ColTags),
                    ClipNote = Get(row, map, ColClipNote),
                    SetPlay = Get(row, map, ColSetPlay),
                };

                // Start/Endが0/0で、rowが短すぎる等のゴミは弾く（必要なら外してOK）
                if (clip.EndSeconds < clip.StartSeconds)
                {
                    var tmp = clip.StartSeconds;
                    clip.StartSeconds = clip.EndSeconds;
                    clip.EndSeconds = tmp;
                }

                result.Add(clip);
            }

            return (csvVideoName, result);
        }

        private static string NormalizeTeam(string? t)
        {
            t = (t ?? "").Trim();
            if (string.Equals(t, "A", StringComparison.OrdinalIgnoreCase)) return "A";
            if (string.Equals(t, "B", StringComparison.OrdinalIgnoreCase)) return "B";
            // "Team A"/"Team B"っぽい入力も吸収
            if (t.Contains("A", StringComparison.OrdinalIgnoreCase)) return "A";
            if (t.Contains("B", StringComparison.OrdinalIgnoreCase)) return "B";
            return "A";
        }

        private static Encoding DetectEncoding(string path)
        {
            // ざっくり：BOMがあれば尊重、なければUTF-8として読む
            using var fs = File.OpenRead(path);
            if (fs.Length >= 3)
            {
                var bom = new byte[3];
                fs.Read(bom, 0, 3);
                if (bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF) return new UTF8Encoding(true);
            }
            return new UTF8Encoding(false);
        }

        private static string Q(string s)
        {
            s ??= "";
            // CSVエスケープ
            if (s.Contains('"') || s.Contains(',') || s.Contains('\n') || s.Contains('\r'))
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            return s;
        }

        private static List<string> SplitCsvLine(string line)
        {
            var res = new List<string>();
            if (line == null) return res;

            var sb = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        res.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            res.Add(sb.ToString());
            return res;
        }

        private static bool EqualsAny(string value, string[] candidates)
            => candidates.Any(c => string.Equals(value?.Trim(), c, StringComparison.OrdinalIgnoreCase));

        private static string Get(List<string> row, Dictionary<string, int> map, string[] keys)
        {
            // ヘッダなしCSVの場合：列順を想定（最小限）
            // VideoName,Team,Start,End,Tags,ClipNote,SetPlay
            if (map.Count == 0)
            {
                int idx = keys == ColVideoName ? 0 :
                          keys == ColTeam ? 1 :
                          keys == ColStart ? 2 :
                          keys == ColEnd ? 3 :
                          keys == ColTags ? 4 :
                          keys == ColClipNote ? 5 :
                          keys == ColSetPlay ? 6 : -1;

                return (idx >= 0 && idx < row.Count) ? row[idx] : "";
            }

            foreach (var k in keys)
            {
                if (map.TryGetValue(k, out int idx))
                    return (idx >= 0 && idx < row.Count) ? row[idx] : "";
            }

            // 見つからない場合は空
            return "";
        }

        private static double ParseTimeToSeconds(string s)
        {
            s = (s ?? "").Trim();
            if (string.IsNullOrWhiteSpace(s)) return 0;

            // "m:ss" / "h:mm:ss" / "ss" / "mm:ss.xx" などに対応
            var parts = s.Split(':');
            try
            {
                if (parts.Length == 1)
                {
                    if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                        return sec;
                    if (double.TryParse(parts[0], out sec))
                        return sec;
                    return 0;
                }
                if (parts.Length == 2)
                {
                    double m = double.Parse(parts[0]);
                    double sec = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    return m * 60 + sec;
                }
                if (parts.Length == 3)
                {
                    double h = double.Parse(parts[0]);
                    double m = double.Parse(parts[1]);
                    double sec = double.Parse(parts[2], CultureInfo.InvariantCulture);
                    return h * 3600 + m * 60 + sec;
                }
            }
            catch { }
            return 0;
        }
    }
}

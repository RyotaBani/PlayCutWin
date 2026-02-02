using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PlayCutWin.Helpers
{
    /// <summary>
    /// Persist per-tag notes to a small JSON file under AppData.
    /// Safe-by-default: any IO/JSON failure should not crash the app.
    /// </summary>
    public static class TagNoteStore
    {
        private const string AppFolderName = "PlayCutWin";
        private const string FileName = "tag_notes.json";

        private static string GetFilePath()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppFolderName);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, FileName);
        }

        public static Dictionary<string, string> Load()
        {
            try
            {
                var path = GetFilePath();
                if (!File.Exists(path)) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                return data != null
                    ? new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // Never crash on load.
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void Save(Dictionary<string, string> notes)
        {
            try
            {
                var path = GetFilePath();
                var json = JsonSerializer.Serialize(notes, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Never crash on save.
            }
        }
    }
}

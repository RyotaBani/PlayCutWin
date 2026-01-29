using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PlayCutWin.Models;

namespace PlayCutWin.Services
{
    /// <summary>
    /// Stores tag definitions (offense/defense) with optional per-tag comments (tooltips).
    /// File location: %APPDATA%\PlayCutWin\tags.json
    /// </summary>
    public sealed class TagCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _filePath;

        public List<TagDefinition> Items { get; private set; } = new();

        public TagCatalog(string filePath)
        {
            _filePath = filePath;
        }

        public static TagCatalog LoadOrCreate()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlayCutWin");
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "tags.json");

            var catalog = new TagCatalog(file);
            catalog.Items = catalog.LoadFromDisk() ?? DefaultItems();
            catalog.MergeDefaults();
            catalog.Save();
            return catalog;
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(Items, JsonOptions);
            File.WriteAllText(_filePath, json);
        }

        public void RestoreDefaults()
        {
            Items = DefaultItems();
            Save();
        }

        public IEnumerable<TagDefinition> Get(TagCategory category)
            => Items.Where(t => t.Category == category).OrderBy(t => t.Name);

        public string GetComment(TagCategory category, string tagName)
        {
            return Items.FirstOrDefault(t => t.Category == category && string.Equals(t.Name, tagName, StringComparison.OrdinalIgnoreCase))?.Comment
                   ?? "";
        }

        private List<TagDefinition>? LoadFromDisk()
        {
            try
            {
                if (!File.Exists(_filePath)) return null;
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<TagDefinition>>(json, JsonOptions);
            }
            catch
            {
                return null;
            }
        }

        private void MergeDefaults()
        {
            var defaults = DefaultItems();
            foreach (var d in defaults)
            {
                if (!Items.Any(i => i.Category == d.Category && string.Equals(i.Name, d.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    Items.Add(d);
                }
            }
        }

        private static List<TagDefinition> DefaultItems()
        {
            // Same order and labels as the Mac app.
            var offense = new[] { "Transition", "Set", "PnR", "BLOB", "SLOB", "vs M/M", "vs Zone", "2nd Attack", "3rd Attack more" };
            var defense = new[] { "M/M", "Zone", "Rebound", "Steal" };

            var list = new List<TagDefinition>();
            list.AddRange(offense.Select(t => new TagDefinition { Category = TagCategory.Offense, Name = t, Comment = "" }));
            list.AddRange(defense.Select(t => new TagDefinition { Category = TagCategory.Defense, Name = t, Comment = "" }));
            return list;
        }
    }
}

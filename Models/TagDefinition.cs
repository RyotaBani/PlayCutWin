using System.Text.Json.Serialization;

namespace PlayCutWin.Models
{
    public enum TagCategory
    {
        Offense,
        Defense
    }

    public sealed class TagDefinition
    {
        public TagCategory Category { get; set; }
        public string Name { get; set; } = "";
        public string Comment { get; set; } = "";

        [JsonIgnore]
        public string CategoryLabel => Category == TagCategory.Offense ? "Offense" : "Defense";
    }
}

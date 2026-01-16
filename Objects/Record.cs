using System.Collections.Generic;
namespace Assets.Scripts.Objects
{
    public class Record
    {
        public string? Name { get; set; }
        public string? CardType { get; set; }
        public string? Rarity { get; set; }
        public List<string>? Product { get; set; }
        public string? Color { get; set; }
        public string? HP { get; set; }
        public string? BloomLevel { get; set; }
        public OshiSkill? OshiSkill { get; set; }
        public OshiSkill? SPOshiSkill { get; set; }
        public List<string>? AbilityText { get; set; }
        public string? Illustrator { get; set; }
        public string? CardNumber { get; set; }
        public string? Life { get; set; }
        public List<string>? Tag { get; set; }
        public List<JsonArt>? Arts { get; set; }
        public List<string>? BatonTouchCost { get; set; }
        public List<string>? Extra { get; set; }
        public int? Id { get; set; }
        public Gift? Gift { get; set; }
    }
}

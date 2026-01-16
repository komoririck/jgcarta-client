using System;
using System.Collections.Generic;

namespace Assets.Scripts.Objects
{
    public class JsonArt
    {
        public string Name { get; set; }
        public List<List<object>> Cost { get; set; }
        public int Damage { get; set; }
        public JsonTokkou Tokkou { get; set; }
        public string Effect { get; set; }
        public static Art Convert(JsonArt jsonArt)
        {
            var art = new Art
            {
                Name = jsonArt.Name,
                Damage = jsonArt.Damage,
                Effect = jsonArt.Effect,
                Cost = new List<ColorCount>()
            };

            // ---- COST ----
            if (jsonArt.Cost != null)
            {
                foreach (var pair in jsonArt.Cost)
                {
                    if (pair.Count != 2)
                        continue;

                    art.Cost.Add(new ColorCount
                    {
                        Color = (ColorCard)Enum.Parse(typeof(ColorCard), pair[0].ToString()),
                        Amount = System.Convert.ToInt32(pair[1]),
                    });
                }
            }

            // ---- TOKKOU ----
            if (jsonArt.Tokkou != null)
            {
                art.Tokkou = new JsonTokkou
                {
                    Color = jsonArt.Tokkou.Color,
                    Amount = jsonArt.Tokkou.Amount
                };
            }

            return art;
        }
    }
}

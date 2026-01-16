using Assets.Scripts.Objects;
using System.Collections.Generic;

public class Art
{
    public string Name { get; set; }

    public List<ColorCount> Cost { get; set; }

    public int Damage { get; set; }

    public JsonTokkou Tokkou { get; set; }

    public string Effect { get; set; }
}
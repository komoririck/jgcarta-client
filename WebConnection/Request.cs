using Newtonsoft.Json;
using System;

[Serializable]
public class Request
{
    public string? playerID { get; set; }
    public string? password { get; set; }
    public string? email { get; set; }
    public string? type { get; set; }
    public string? description { get; set; }
    public DuelAction? duelAction { get; set; }
}

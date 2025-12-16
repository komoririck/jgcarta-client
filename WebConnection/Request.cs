using Newtonsoft.Json;
using System;

[Serializable]
public class Request
{
    public string playerID;
    public string password;
    public string email;
    public string type;
    public string description;
    [JsonIgnore]
    public object jsonObject;
    public DuelAction duelAction;
}

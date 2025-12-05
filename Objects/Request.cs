using System;

[Serializable]
public class Request
{
    public string playerID;
    public string password;
    public string email;
    public string type;
    public string description;
    public object jsonObject;
    public DuelAction duelAction;
}

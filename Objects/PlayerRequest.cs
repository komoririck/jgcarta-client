using System;
using System.Collections.Generic;

[Serializable]
public class PlayerRequest
{
    public string playerID;
    public string password;
    public string email;
    public string type;
    public string description;
    public object jsonObject;
    public string requestObject;
}

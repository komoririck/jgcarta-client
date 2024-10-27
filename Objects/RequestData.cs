using System;
using System.Collections.Generic;

[Serializable]
public class RequestData
{
    public string type;
    public string description;
    public int sync;
    public string requestObject;
    public string extraRequestObject;
}

[Serializable]
public class PlayerRequest
{
    public string playerID;
    public string password;
    public RequestData requestData = new();
}
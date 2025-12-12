using System.Collections;

public class ActionItem
{
    public string Type;               
    public IEnumerator Routine;      

    public ActionItem(string type, IEnumerator routine)
    {
        this.Type = type;
        Routine = routine;
    }
}


using System.Collections;
using System.Collections.Generic;

public class ActionItem
{
    public static Queue<ActionItem> visualActionQueue = new();
    private static string lastEnqueuedType = null;

    public string Type;               
    public IEnumerator Routine;      
    public ActionItem(string type, IEnumerator routine)
    {
        this.Type = type;
        Routine = routine;
    }
    public static void Add(string Type, IEnumerator Routine) {
        if (ShouldPreventEnqueue(Type))
            return;

        visualActionQueue.Enqueue(new ActionItem(Type, Routine));
        lastEnqueuedType = Type;
    }
    private static bool ShouldPreventEnqueue(string newType)
    {
        if (lastEnqueuedType != newType)
            return false;
        // can add other conditions here to optimize queue
        return skipDuplicatesOf.Contains(newType);
    }
    private static readonly HashSet<string> skipDuplicatesOf = new()
    {
        "GetUsableCards",
    };
}


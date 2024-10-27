using UnityEngine;
using UnityEditor;

public class ConsoleClearer
{
  //  [MenuItem("Tools/Clear Console %#c")] // Ctrl + Shift + C shortcut
    public static void ClearConsole()
    {
        /* // Get the type of the LogEntries class
         var logEntriesType = System.Type.GetType("UnityEditor.LogEntries,UnityEditor");

         // Call the Clear method from LogEntries
         var clearMethod = logEntriesType.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
         clearMethod.Invoke(null, null);

         // Optionally, you can log a message
         Debug.Log("Console cleared!");
        */
    }
}

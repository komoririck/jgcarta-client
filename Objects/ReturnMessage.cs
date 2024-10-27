using System;
using UnityEngine;

public class ReturnMessage : MonoBehaviour
{
    [SerializeField] private string requestReturn;

    private void Start()
    {
        requestReturn = "";
    }

    public void resetMessage() {
        requestReturn = "";
    }
    public string getRequestReturn()
    {
        return requestReturn;
    }
    public void setRequestReturn(string s)
    {
        requestReturn = s;
    }
}
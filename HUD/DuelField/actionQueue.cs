using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using static Lib;
public sealed class GameAction
{
    public string Type;
    public IEnumerator Routine;
    public DuelAction DA;

    public GameAction(string type, IEnumerator routine, DuelAction dA)
    {
        Type = type;
        Routine = routine;
        DA = dA;
    }
}
public sealed class ActionQueue
{
    public  DuelAction CUR_DA;
    public  string CUR_DA_TYPE;
    public  bool IsRunning { get; private set; }
    private  readonly List<GameAction> _actions = new();
    private  int _currentIndex = 0;
    private  int _insertionIndex = -1;
    public  void CancelAll()
    {
        _actions.Clear();
    }
    public  void Enqueue(string Type, IEnumerator Routine, DuelAction DA)
    {
        _actions.Add(new GameAction(Type, Routine, DA));
    }
    public  void EnqueueNext(string Type, IEnumerator Routine)
    {
        _actions.Insert(_insertionIndex, new GameAction(Type, Routine, CUR_DA));
        _insertionIndex++;
    }

    public  IEnumerator Run()
    {
        if (IsRunning)
            yield break;

        IsRunning = true;

        while (_currentIndex < _actions.Count)
        {
            var action = _actions[_currentIndex];

            _insertionIndex = _currentIndex + 1;

            CUR_DA = action.DA;
            CUR_DA_TYPE = action.Type;
            yield return action.Routine;

            foreach (GameObject obj in DuelField.INSTANCE.changedZones.Distinct())
            {
                if (obj.gameObject.name.Contains("Hand"))
                    yield return ActionLibrary.ArrangeCards(obj.gameObject);
            }

            _currentIndex++;
            _insertionIndex = -1;
        }

        foreach (GameObject obj in DuelField.INSTANCE.changedZones.Distinct())
        {
            if (obj.gameObject.name.Contains("Life"))
                yield return ActionLibrary.ArrangeCards(obj.gameObject, true);

            if (!(obj.gameObject.name.Contains("Hand")))
            {
                yield return ActionLibrary.CardCounter(obj);
                yield return ActionLibrary.StackCardsEffect(obj);
            }
        }
        DuelField.INSTANCE.changedZones.Clear();
        Reset();
    }
    private  void Reset()
    {
        _actions.Clear();
        _currentIndex = 0;
        _insertionIndex = -1;
        IsRunning = false;
    }
}

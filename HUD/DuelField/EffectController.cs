using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class EffectController : MonoBehaviour
{
    static public EffectController INSTANCE;

    public List<Func<IEnumerator>> menuActions = new List<Func<IEnumerator>>();

    public bool isSelectionCompleted = false;
    public bool isServerResponseArrive = false;

    public static DuelAction Da_DiceRoll;

    void Start()
    {
        INSTANCE = this;
    }
    private IEnumerator RollDiceTilNotAbleOrDontWantTo()
    {
        List<Card> allAttachments = CardLib.GetAndFilterCards(CardThatAllowReRoll: true);
        // Check if the player can reroll based on game rules.
        if (allAttachments.Count > 0)
        {
            // Ask the player if they want to reroll and wait for the response.
            menuActions.Insert(0, () =>
            {                          // Roll the dice and store the result.
                int diceRoll = Da_DiceRoll.diceRoll.Last();
                return DuelField_YesOrNoMenu.INSTANCE.ShowYesOrNoMenu(Da_DiceRoll, $"You rolled a {diceRoll}. Reroll?");
            });

            menuActions.Insert(1, (Func<IEnumerator>)(() =>
            {
                bool choosed = DuelField_YesOrNoMenu.GetDA().yesOrNo;
                // If player chooses "YES", resolve the reroll effect
                if (choosed)
                {
                    menuActions.Insert(0, () =>
                    {
                        return DuelField_DetachCardMenu.INSTANCE.SetupSelectableItems(DuelField_YesOrNoMenu.GetDA(), IsACheer: false);
                    });
                    menuActions.Insert(1, () =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(DuelField_DetachCardMenu.GetDA(), "ResolveRerollEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Insert(2, (Func<IEnumerator>)(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo();
                        return dummy();
                    }));
                }
                return dummy();
            }));
        }
        return dummy();
    }
    public IEnumerator dummy()
    {
        yield return new WaitUntil(() => true);
    }
    IEnumerator WaitForServerResponse()
    {
        while (!isServerResponseArrive)
        {
            yield return null;
        }
    }
}
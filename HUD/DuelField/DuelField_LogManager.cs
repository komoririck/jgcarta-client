using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DuelField_LogManager : MonoBehaviour
{
    public static void AddLog(DuelAction _DuelAction, string type) {
        string whichPlayer = null;
        if (_DuelAction == null || _DuelAction.player != null)
            whichPlayer = DuelField.INSTANCE.IsMyTurn() ? "You" : "Your Opponent";
        else
            whichPlayer = _DuelAction.player == DuelField.INSTANCE.playersType[PlayerInfo.INSTANCE.PlayerID] ? "You" : "Your Opponent";

        switch (type){
            case "StartDuel":
                break;
            case "InitialDrawP2":
            case "InitialDraw":
                InstantiateLogObj($"{whichPlayer} drew the initial hand");
                break;
            case "PAMulligan":
            case "PBMulligan":
                InstantiateLogObj($"{whichPlayer} decided to mulligan and redraw his hand");
                break;
            case "PBNoMulligan":
            case "PANoMulligan":
                InstantiateLogObj($"{whichPlayer} decided to not mulligan");
                break;
            case "PBMulliganF":
            case "PAMulliganF":
                if (_DuelAction.yesOrNo)
                    InstantiateLogObj($"{whichPlayer} have not playable cards in hand\nForced to mulligan drawing {_DuelAction.cards.Count}");
                else
                    InstantiateLogObj($"No need to force {whichPlayer} to mulligan");
                break;
            case "ResetStep":
                var textValue = $"New turn started for {whichPlayer}\n";
                if (_DuelAction.used != null) {
                    textValue += $"Collab holomember {_DuelAction.used.cardNumber} send to backstage suspended\n";
                }
                InstantiateLogObj(textValue);
                break;
            case "ReSetStage":
                textValue = null;
                if (_DuelAction.used != null)
                {
                    textValue += $"Last turn {whichPlayer} center holomember has defeated, your choosed {_DuelAction.used.cardNumber} as your new holomember\n";
                }
                InstantiateLogObj(textValue);
                break;
            case "DrawPhase":
                InstantiateLogObj($"{whichPlayer} drew for turn");
                break;
            case "DefeatedHoloMember":
                InstantiateLogObj($"{whichPlayer} {_DuelAction.used.cardNumber} holomember has defeated");
                break;
            case "HolomemDefatedSoGainCheer":
                textValue = $"{whichPlayer} gained {_DuelAction.cards.Count} because holomember has defeated\n";
                foreach (CardData card in _DuelAction.cards) {
                    textValue += $"{card.cardNumber}\n";
                }
                InstantiateLogObj(textValue);
                break;
            case "CheerStepEnd":
            case "CheerStepEndDefeatedHolomem":
                InstantiateLogObj($"{whichPlayer} assigned cheer {_DuelAction.used.cardNumber} to {_DuelAction.target.cardNumber} at {_DuelAction.target.curZone}");
                break;
            case "CheerStep":
                InstantiateLogObj($"{whichPlayer} gained a energy for turn {_DuelAction.cards[0].cardNumber}");
                break;
            case "MainPhase":
                InstantiateLogObj($"{whichPlayer} started the main phase");
                break;
            case "Endturn":
                InstantiateLogObj($"{whichPlayer} started the main phase");
                break;
            case "PlayHolomem":
                InstantiateLogObj($"{whichPlayer} played a {_DuelAction.used.cardNumber} at {_DuelAction.used.curZone}");
                break;
            case "BloomHolomem":
                InstantiateLogObj($"{whichPlayer} bloomed his {_DuelAction.target.cardNumber} to {_DuelAction.used.cardNumber} at {_DuelAction.used.curZone}");
                break;
            case "DoCollab":
                InstantiateLogObj($"{whichPlayer} collabed using his {_DuelAction.used.cardNumber}");
                break;
            case "UnDoCollab":
                InstantiateLogObj($"{whichPlayer} ended his collab his {_DuelAction.used.cardNumber}");
                break;
            case "AttachEnergyResponse":
                InstantiateLogObj($"{whichPlayer} attached a cheer {_DuelAction.used.cardNumber} to {_DuelAction.target.cardNumber} at {_DuelAction.target.curZone}");
                break;
            case "DisposeUsedSupport":
                InstantiateLogObj($"{whichPlayer} used a support card {_DuelAction.used.cardNumber}");
                break;
            case "ResolveOnSupportEffect":
                break;
            case "OnCollabEffect":
                if (DuelField_UI_MAP.INSTANCE.transform.GetChild(DuelField_UI_MAP.INSTANCE.transform.childCount - 1).GetComponent<TMP_Text>().text.Equals($"{whichPlayer} used a support card {_DuelAction.used.cardNumber}"))
                    return;
                InstantiateLogObj($"{whichPlayer} used a support card {_DuelAction.used.cardNumber}");
                break;
            case "OnArtEffect":
                if (DuelField_UI_MAP.INSTANCE.transform.GetChild(DuelField_UI_MAP.INSTANCE.transform.childCount - 1).GetComponent<TMP_Text>().text.Equals($"{whichPlayer} activated a art effect {_DuelAction.used.cardNumber}"))
                    return;
                InstantiateLogObj($"{whichPlayer} activated a art effect {_DuelAction.used.cardNumber}");
                break;
            case "PickFromListThenGiveBacKFromHandDone":
                break;
            case "DrawCollabEffect":
            case "DrawArtEffect":
            case "SupportEffectDraw":
                InstantiateLogObj($"{whichPlayer} drew {_DuelAction.cards.Count} by card effect");
                break;
            case "RollDice":
                int diceRoll = _DuelAction.indexes[0];
                InstantiateLogObj($"{whichPlayer} rolled a dice {diceRoll}");
                break;
        }
        void InstantiateLogObj(string text) {
            var NewDuelLogObject = Instantiate(DuelField_UI_MAP.INSTANCE.LogPrefab, DuelField_UI_MAP.INSTANCE.transform);
            var txt = NewDuelLogObject.GetComponent<TMP_Text>();
            txt.text = text;
            Instantiate(DuelField_UI_MAP.INSTANCE.LogBar, DuelField_UI_MAP.INSTANCE.transform);
        }
    }
}

using Newtonsoft.Json;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DuelField_LogManager : MonoBehaviour
{
    public static void AddLog(DuelAction _DuelAction, string type) {
        PlayerInfo _PlayerInfo = FindAnyObjectByType<PlayerInfo>();
        DuelFieldData _DuelFieldData = DuelField.INSTANCE.duelFieldData;

        string whichPlayer = "";
        if(_DuelAction == null || string.IsNullOrEmpty(_DuelAction.playerID))
            whichPlayer = _DuelFieldData.currentPlayerTurn.Equals(_PlayerInfo.PlayerID) ? "You" : "Your Opponent";
        else
            whichPlayer = _DuelAction.playerID.Equals(_PlayerInfo.PlayerID) ? "You" : "Your Opponent";

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
                if (_DuelAction.actionObject.Equals("True"))
                    InstantiateLogObj($"{whichPlayer} have not playable cards in hand\nForced to mulligan drawing {_DuelAction.cardList.Count}");
                else
                    InstantiateLogObj($"No need to force {whichPlayer} to mulligan");
                break;
            case "DuelUpdate":
                InstantiateLogObj($"{whichPlayer} setup his initial board");
                break;
            case "ResetStep":
                var textValue = $"New turn started for {whichPlayer}\n";
                if (_DuelAction.usedCard != null) {
                    textValue += $"Collab holomember {_DuelAction.usedCard.cardNumber} send to backstage suspended\n";
                }
                InstantiateLogObj(textValue);
                break;
            case "ReSetStage":
                textValue = "";
                if (_DuelAction.usedCard != null)
                {
                    textValue += $"Last turn {whichPlayer} center holomember has defeated, your choosed {_DuelAction.usedCard.cardNumber} as your new holomember\n";
                }
                InstantiateLogObj(textValue);
                break;
            case "DrawPhase":
                InstantiateLogObj($"{whichPlayer} drew for turn");
                break;
            case "DefeatedHoloMember":
                InstantiateLogObj($"{whichPlayer} {_DuelAction.usedCard.cardNumber} holomember has defeated");
                break;
            case "HolomemDefatedSoGainCheer":
                textValue = $"{whichPlayer} gained {_DuelAction.cardList.Count} because holomember has defeated\n";
                foreach (CardData card in _DuelAction.cardList) {
                    textValue += $"{card.cardNumber}\n";
                }
                InstantiateLogObj(textValue);
                break;
            case "CheerStepEnd":
            case "CheerStepEndDefeatedHolomem":
                InstantiateLogObj($"{whichPlayer} assigned cheer {_DuelAction.usedCard.cardNumber} to {_DuelAction.targetCard.cardNumber} at {_DuelAction.targetCard.curZone}");
                break;
            case "CheerStep":
                InstantiateLogObj($"{whichPlayer} gained a energy for turn {_DuelAction.cardList[0].cardNumber}");
                break;
            case "MainPhase":
                InstantiateLogObj($"{whichPlayer} started the main phase");
                break;
            case "Endturn":
                InstantiateLogObj($"{whichPlayer} started the main phase");
                break;
            case "PlayHolomem":
                InstantiateLogObj($"{whichPlayer} played a {_DuelAction.usedCard.cardNumber} at {_DuelAction.usedCard.curZone}");
                break;
            case "BloomHolomem":
                InstantiateLogObj($"{whichPlayer} bloomed his {_DuelAction.targetCard.cardNumber} to {_DuelAction.usedCard.cardNumber} at {_DuelAction.usedCard.curZone}");
                break;
            case "DoCollab":
                InstantiateLogObj($"{whichPlayer} collabed using his {_DuelAction.usedCard.cardNumber}");
                break;
            case "UnDoCollab":
                InstantiateLogObj($"{whichPlayer} ended his collab his {_DuelAction.usedCard.cardNumber}");
                break;
            case "AttachEnergyResponse":
                InstantiateLogObj($"{whichPlayer} attached a cheer {_DuelAction.usedCard.cardNumber} to {_DuelAction.targetCard.cardNumber} at {_DuelAction.targetCard.curZone}");
                break;
            case "DisposeUsedSupport":
                InstantiateLogObj($"{whichPlayer} used a support card {_DuelAction.usedCard.cardNumber}");
                break;
            case "ResolveOnSupportEffect":
                break;
            case "OnCollabEffect":
                if (DuelField_UI_MAP.INSTANCE.transform.GetChild(DuelField_UI_MAP.INSTANCE.transform.childCount - 1).GetComponent<TMP_Text>().text.Equals($"{whichPlayer} used a support card {_DuelAction.usedCard.cardNumber}"))
                    return;
                InstantiateLogObj($"{whichPlayer} used a support card {_DuelAction.usedCard.cardNumber}");
                break;
            case "OnArtEffect":
                if (DuelField_UI_MAP.INSTANCE.transform.GetChild(DuelField_UI_MAP.INSTANCE.transform.childCount - 1).GetComponent<TMP_Text>().text.Equals($"{whichPlayer} activated a art effect {_DuelAction.usedCard.cardNumber}"))
                    return;
                InstantiateLogObj($"{whichPlayer} activated a art effect {_DuelAction.usedCard.cardNumber}");
                break;
            case "PickFromListThenGiveBacKFromHandDone":
                break;
            case "DrawCollabEffect":
            case "DrawArtEffect":
            case "SupportEffectDraw":
                InstantiateLogObj($"{whichPlayer} drew {_DuelAction.cardList.Count} by card effect");
                break;
            case "RollDice":
                List<string> serverReturn = JsonConvert.DeserializeObject<List<string>>(_DuelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                string diceRoll = serverReturn[0];
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

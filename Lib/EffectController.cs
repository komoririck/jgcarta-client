using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using static DuelField;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts.Lib
{
    class EffectController : MonoBehaviour
    {

        private DuelField_ShowListPickThenReorder _DuelField_ShowListPickThenReorder;
        private DuelField_TargetForEffectMenu _DuelField_TargetForEffectMenu;
        private DuelField_DetachCardMenu _DuelField_DetachEnergyOrEquipMenu;
        private DuelField_ShowAlistPickOne _DuelField_ShowAlistPickOne;
        private DuelField_YesOrNoMenu _DuelField_YesOrNoMenu;

        private DuelField _DuelField;
        private List<Func<IEnumerator>> menuActions = new List<Func<IEnumerator>>();

        public List<object> EffectInformation;
        private object lastRetrievedValue;

        public DuelAction duelActionOutput;
        public DuelAction duelActionInput;

        public bool isSelectionCompleted = false;
        public bool isServerResponseArrive = false;

        void Start()
        {
            _DuelField = FindAnyObjectByType<DuelField>();
            _DuelField_ShowListPickThenReorder = FindAnyObjectByType<DuelField_ShowListPickThenReorder>();
            _DuelField_TargetForEffectMenu = FindAnyObjectByType<DuelField_TargetForEffectMenu>();
            _DuelField_ShowAlistPickOne = FindAnyObjectByType<DuelField_ShowAlistPickOne>();
            _DuelField_DetachEnergyOrEquipMenu = FindAnyObjectByType<DuelField_DetachCardMenu>();
            _DuelField_YesOrNoMenu = FindAnyObjectByType<DuelField_YesOrNoMenu>();
            EffectInformation = new List<object>();
        }
        public IEnumerator OshiSkill(DuelAction _DuelActionFirstAction)
        {
            List<Card> holoPowerList;
            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                case "hSD01-001":
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new string[] { "Stage" };
                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionFirstAction, AddCostToEffectInformation: true, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5", "Collaboration" };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Player, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    menuActions.Add(() =>
                    {
                        Card X = (Card)EffectInformation[0];
                        CardData cardData = CardData.CreateCardDataFromCard(X.cardNumber, X.playedFrom, X.cardPosition);
                        duelActionOutput = (DuelAction)EffectInformation[1];
                        duelActionOutput.actionObject = JsonConvert.SerializeObject(cardData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        duelActionOutput.cheerCostCard = null;
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnOshiEffect");
                        return WaitForServerResponse();
                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());

            yield return true;
        }
        public IEnumerator SPOshiSkill(DuelAction _DuelActionFirstAction)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;
            EffectInformation.Clear();

            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                case "hSD01-001":
                    //select the card to return to back
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;

            }
            StartCoroutine(StartMenuSequenceCoroutine());

            yield return true;
        }
        public void ResolveOnCollabEffect(DuelAction _DuelActionR)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;

            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionR.usedCard.cardNumber)
            {
                case "hSD01-015":
                case "hSD01-004":
                case "hBP01-016":
                case "hBP01-023":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-012":
                    string WillActivate = "";
                    //select if active
                    menuActions.Add(() =>
                    {
                        if (_DuelField.GetZone("CardCheer", TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });

                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });

                    menuActions.Add(() =>
                    {
                        WillActivate = (string)EffectInformation[0];

                        if (!WillActivate.Equals("Yes"))
                        {
                            menuActions.Clear();
                            return null;
                        }

                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        holoPowerList = JsonConvert.DeserializeObject<List<Card>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        //show the list to the player, player pick 1
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, holoPowerList, holoPowerList);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = (List<string>)EffectInformation[1];
                        _DuelActionR.usedCard = new CardData() { cardNumber = cardnumber[0] };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, new string[] { "Stage" });
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[2];
                        duelActionOutput.actionObject = WillActivate;

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return dummy();
                    });
                    break;
                case "hSD01-007":
                    //we get the powerlist from the server
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });

                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        holoPowerList = JsonConvert.DeserializeObject<List<Card>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        //show the list to the player, player pick 1
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, holoPowerList, holoPowerList);
                    });

                    menuActions.Add(() =>

                    {
                        isSelectionCompleted = false;
                        //fetch player hand
                        List<Card> secondList = GameObject
                            .Find("MatchField")
                            .transform.Find("PlayersHands/PlayerHand")
                            .GetComponentsInChildren<Card>()
                            .ToList();

                        //now we show the hand + the card the player selected so he can pick and send pack to the server
                        if (EffectInformation.Count > 0)
                        {
                            List<string> pickedFromHoloPower = (List<string>)EffectInformation[0];
                            secondList.Add(new Card(pickedFromHoloPower[0]));

                            // Setup the second menu
                            return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, secondList, secondList);
                        }
                        return dummy();
                    });

                    menuActions.Add(() =>
                    {
                        List<string> returnList = (List<string>)EffectInformation[0];
                        returnList.AddRange((List<string>)EffectInformation[1]);
                        duelActionOutput = new DuelAction()
                        {
                            actionObject = JsonConvert.SerializeObject(returnList),
                        };
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-009":
                    int diceRoll = 0;
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });

                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });

                    menuActions.Add(() =>
                    {
                        diceRoll = GetLastValue<int>(1);

                        if (diceRoll > 4)
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player, new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" });
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        duelActionOutput.actionType = "AskAttachTopCheerEnergyToBack";

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");

                        return WaitForServerResponse();

                    });
                    //select if retreat
                    menuActions.Add(() =>
                    {
                        diceRoll = GetLastValue<int>(2);
                        if (diceRoll < 2)
                        {
                            return _DuelField_YesOrNoMenu.ShowYesOrNoMenu($"Retreat ?");
                        }
                        else
                        {
                            EffectInformation.Add("No");
                        }
                        return dummy();
                    });
                    //inform server if retreat
                    menuActions.Add(() =>
                    {
                        duelActionOutput.actionObject = GetLastValue<string>();
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();

                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());

        }
        public void ResolveSuportEffect(DuelAction _DuelActionFirstAction)
        {
            List<Card> energyList;

            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                case "hBP01-103":
                    menuActions.Add(() =>
                    {

                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionFirstAction);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    //we recieve the list then callback again to finish the effect
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        var selected = (List<string>)EffectInformation[1];
                        duelActionOutput.actionObject = selected[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-102":

                    if (_DuelField.cardsPlayer.Count > 6)
                        break;

                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        List<Card> filteresList = new();
                        foreach (Card card in duelActionInput.cardList)
                        {
                            card.GetCardInfo();
                            if (card.cardTag.Contains($"#歌"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, 1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-016":
                case "hSD01-017":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return dummy();
                    });
                    break;
                case "hSD01-018":

                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        List<Card> filteresList = new();
                        foreach (Card card in duelActionInput.cardList)
                        {
                            card.GetCardInfo();
                            if (card.cardType.Equals("サポート・アイテム・LIMITED") || card.cardType.Equals("サポート・イベント・LIMITED") || card.cardType.Equals("サポート・スタッフ・LIMITED"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, 1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {

                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-019":
                    menuActions.Add(() =>
                    {

                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionFirstAction);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    //we recieve the list then callback again to finish the effect
                    menuActions.Add(() =>
                    {

                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        var selected = (List<string>)EffectInformation[1];
                        duelActionOutput.actionObject = selected[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-020":

                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionFirstAction);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        //if not oddnumber, we draw a card calling "Draw" at DuelField, so break
                        if (GetLastValue<int>(1) < 3)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //then, target one card to assign the energy
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        duelActionInput.actionObject = cardnumber[0];
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelActionInput);
                    });

                    //inform the server
                    menuActions.Add(() =>
                    {
                        DuelAction tempaction = GetLastValue<DuelAction>();
                        duelActionOutput = new DuelAction
                        {
                            usedCard = tempaction.usedCard,
                            targetCard = tempaction.targetCard,
                            actionObject = tempaction.actionObject,
                        };

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-021":

                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        List<Card> filteresList = new();
                        foreach (Card card in duelActionInput.cardList)
                        {
                            card.GetCardInfo();
                            if (card.cardName.Equals("ときのそら") || card.cardName.Equals("AZKi") || card.cardName.Equals("SorAZ"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, -1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-104":

                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = (List<string>)EffectInformation[0];
                        _DuelActionFirstAction.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-105":

                    //pay the cost
                    menuActions.Add(() =>
                    {

                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionFirstAction);
                    });
                    //add the targert to action with the cost
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelActionOutput);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[1];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    //from the list of energy recieved, pick one
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //then, target one card to assign the energy
                    menuActions.Add(() =>
                    {


                        List<string> cardnumber = (List<string>)EffectInformation[2];
                        duelActionInput.actionObject = cardnumber[0];
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelActionInput);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = new DuelAction
                        {
                            usedCard = ((DuelAction)EffectInformation[3]).usedCard,
                            targetCard = ((DuelAction)EffectInformation[3]).targetCard,
                            actionObject = ((DuelAction)EffectInformation[3]).actionObject,
                        };
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-106":

                    //select the card to return to back
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-108":

                    //select the card to return to back
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-109":

                    if (_DuelField.cardsPlayer.Count > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        List<Card> filteresList = new();
                        foreach (Card card in duelActionInput.cardList)
                        {
                            card.GetCardInfo();
                            if (card.cardName.Equals("兎田ぺこら") || card.cardName.Equals("ムーナ・ホシノヴァ"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, 1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-111":

                    if (_DuelField.cardsPlayer.Count > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        List<Card> filteresList = new();
                        foreach (Card card in duelActionInput.cardList)
                        {
                            card.GetCardInfo();
                            if (card.cardTag.Contains($"#ID３期生"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, 1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-113":
                    if (_DuelField.cardsPlayer.Count > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        List<Card> filteresList = new();
                        foreach (Card card in duelActionInput.cardList)
                        {
                            card.GetCardInfo();
                            if (card.cardTag.Contains($"#Promise"))
                            {
                                filteresList.Add(card);
                            }
                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, 1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-107":

                    if (_DuelField.cardsPlayer.Count > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        List<Card> filteresList = new();
                        foreach (Card card in duelActionInput.cardList)
                        {
                            card.GetCardInfo();
                            if (card.cardNumber.Equals("hY04-001") || card.cardNumber.Equals("hY02-001") || card.cardNumber.Equals("hY03-001") || card.cardNumber.Equals("hY01-001"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, doubleselect: false, 3);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-112":

                    string diceRoll = "-1";
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionFirstAction);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        if (GetLastValue<int>(1) > 3)
                        {
                            var zonesThatPlayerCanSelect = new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };
                            return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                        }
                        else
                        {
                            return dummy();
                        }
                    });

                    menuActions.Add(() =>
                    {
                        if (GetLastValue<int>(2) < 4)
                            return dummy();

                        duelActionOutput = GetLastValue<DuelAction>();
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });

                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());

        }
        public void ResolveOnArtEffect(DuelAction _DuelActionR)
        {
            List<Card> energyList;

            switch (_DuelActionR.usedCard.cardNumber + "-" + _DuelActionR.selectedSkill)
            {
                case "hSD01-011-デスティニーソング":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-011-SorAZ グラビティ":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        if (!_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().name.Equals("ときのそら") || !_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().name.Equals("SorAZ"))
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, null);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        DuelAction _duelaction = GetLastValue<DuelAction>();
                        DuelAction da = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        _duelaction.actionObject = da.actionObject;

                        _DuelField.GenericActionCallBack(_duelaction, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-013-越えたい未来":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());


        }
        public IEnumerator RetreatArt(DuelAction _DuelActionR)
        {
            //pay the cost
            menuActions.Add(() =>
            {

                var zonesThatPlayerCanSelect = new string[] { _DuelActionR.usedCard.cardPosition };
                return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionR, AddCostToEffectInformation: true, zonesThatPlayerCanSelect);
            });
            /*
            * this is comented, but in the future we may need to implement a counter to be able to charge more than 1 energy for retrat depending of the card
            menuActions.Add(() =>
            {
                
                return _DuelField_DetachEnergyMenu.SetupSelectableItems(_DuelActionR, AddCostToEffectInformation: true);
            });
            */
            //select the card to return to back
            menuActions.Add(() =>
            {
                var zonesThatPlayerCanSelect = new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };
                return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
            });
            //send both cost and targer(color) to the server
            menuActions.Add(() =>
            {
                duelActionOutput = (DuelAction)EffectInformation[1];
                //duelActionOutput.cardList = new List<Card>() { (Card)EffectInformation[0] };//, (Card)EffectInformation[1] };
                //duelActionOutput.cheerCostCard = (CardData.CreateCardDataFromCard((Card)EffectInformation[0]));
                _DuelField.GenericActionCallBack(duelActionOutput, "Retreat");
                return dummy();
            });
            StartCoroutine(StartMenuSequenceCoroutine());

            yield return true;
        }
        internal void ResolveOnDamageResolveEffect(DuelAction _DuelActionDamageResolveFirst)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;
            EffectInformation.Clear();

            if (!CanActivateDamageStepEffect())
            {

                DuelAction _response = new()
                {
                    actionObject = "false"
                };

                _DuelField.GenericActionCallBack(_response, "ResolveDamageToHolomem");
                return;
            }


            switch (_DuelActionDamageResolveFirst.usedCard.cardNumber)
            {
                case "hSD01-011":
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
        }
        internal void ResolveOnBloomEffect(DuelAction duelAction)
        {
        }
        public IEnumerator StartMenuSequenceCoroutine()
        {
            while (menuActions.Count > 0)
            {
                Func<IEnumerator> nextMenu = menuActions[0];
                menuActions.RemoveAt(0);
                isServerResponseArrive = false;
                isSelectionCompleted = false;

                if (menuActions.Count == 0)
                {
                    var finishActions = StartCoroutine(nextMenu());
                    isServerResponseArrive = false;
                    isSelectionCompleted = false;
                    EffectInformation.Clear();
                    yield return finishActions;
                }
                else
                {
                    yield return StartCoroutine(nextMenu());
                }
            }
        }
        public IEnumerator dummy()
        {
            yield return new WaitUntil(() => true);
        }
        public IEnumerator WaitForServerResponse()
        {
            yield return new WaitUntil(() => isServerResponseArrive);
        }

        private void ShowCardEffect(string cheerNumber)
        {
            //throw new NotImplementedException();
        }
        public static bool CheckForDetachableEnergy()
        {
            List<Card> cardList = new();

            cardList.AddRange(GameObject.Find("Collaboration").GetComponentsInChildren<Card>(true));
            cardList.AddRange(GameObject.Find("Stage").GetComponentsInChildren<Card>(true));
            cardList.AddRange(GameObject.Find("CardCheer").GetComponentsInChildren<Card>(true));

            foreach (Card c in cardList)
            {
                if (c.cardType.Equals("エール"))
                    return true;
            }
            Debug.Log("No Avalible cards found to deatch");
            return false;
        }
        private bool CanActivateDamageStepEffect()
        {
            return false;
        }
        private List<Card> CanReRollDice()
        {
            List<Card> allAttachments = new();
            HashSet<Card> uniqueParents = new HashSet<Card>();

            allAttachments.AddRange(_DuelField.GetZone("BackStage1", TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(_DuelField.GetZone("BackStage2", TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(_DuelField.GetZone("BackStage3", TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(_DuelField.GetZone("BackStage4", TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(_DuelField.GetZone("BackStage5", TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(_DuelField.GetZone("Collaboration", TargetPlayer.Player).GetComponentsInChildren<Card>(true));

            foreach (Card card in allAttachments)
            {
                if (card.cardNumber.Equals("hBP01-123"))
                {
                    Card parentCard = card.transform.parent.GetComponent<Card>();
                    uniqueParents.Add(parentCard);
                }
            }

            return uniqueParents.ToList();
        }
        private IEnumerator RollDiceTilNotAbleOrDontWantTo(DuelAction _DuelAction)
        {
            List<Card> allAttachments = CanReRollDice();
            // Check if the player can reroll based on game rules.
            if (allAttachments.Count > 0)
            {
                // Ask the player if they want to reroll and wait for the response.
                menuActions.Insert(0, () =>
                {                          // Roll the dice and store the result.
                    int diceRoll = GetLastValue<int>();
                    return _DuelField_YesOrNoMenu.ShowYesOrNoMenu($"You rolled a {diceRoll}. Reroll?");
                });

                menuActions.Insert(1, () =>
                {
                    string choosed = GetLastValue<string>();
                    // If player chooses "YES", resolve the reroll effect
                    if (choosed.Equals("Yes"))
                    {
                        menuActions.Insert(0, () =>
                        {
                            return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelAction, Cheer: false);
                        });
                        menuActions.Insert(1, () =>
                        {
                            DuelAction da = GetLastValue<DuelAction>();
                            EffectInformation.RemoveAt(EffectInformation.Count - 1);
                            _DuelField.GenericActionCallBack(da, "ResolveRerollEffect");
                            var x = WaitForServerResponse();
                            RollDiceTilNotAbleOrDontWantTo(_DuelAction);
                            return x;
                        });
                    }
                    return dummy();
                });
            }
            else
            {
                EffectInformation.Add("No");
                return dummy();
            }
            return dummy();
        }
        public T GetLastValue<T>(int minus = 0)
        {
            if (EffectInformation.Count == 0)
                throw new InvalidOperationException("EffectInformation list is empty.");

            // Find the last item in the list
            object lastItem = EffectInformation[EffectInformation.Count - 1 - minus];

            // Check if the item can be cast to the requested type
            if (lastItem is T typedValue)
            {
                lastRetrievedValue = typedValue; // Track the last retrieved item
                return typedValue;
            }
            else
            {
                throw new InvalidCastException($"Cannot cast item of type {lastItem.GetType()} to {typeof(T)}.");
            }
        }
        public object GetLastRetrievedValue()
        {
            return lastRetrievedValue;
        }
        internal void ResolveOnRecoveryEffect(Card targetedCard)
        {
        }
    }
}

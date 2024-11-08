using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using static DuelField;

namespace Assets.Scripts.Lib
{
    class EffectController : MonoBehaviour
    {

        private DuelField_ShowListPickThenReorder _DuelField_ShowListPickThenReorder;
        private DuelField_TargetForEffectMenu _DuelField_TargetForEffectMenu;
        private DuelField_DetachEnergyMenu _DuelField_DetachEnergyMenu;
        private DuelField_ShowAlistPickOne _DuelField_ShowAlistPickOne;
        private DuelField_YesOrNoMenu _DuelField_YesOrNoMenu;

        private DuelField _DuelField;
        private List<Func<IEnumerator>> menuActions;
        public List<object> EffectInformation;

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
            _DuelField_DetachEnergyMenu = FindAnyObjectByType<DuelField_DetachEnergyMenu>();
            _DuelField_YesOrNoMenu = FindAnyObjectByType<DuelField_YesOrNoMenu>();
            EffectInformation = new List<object>();
        }
        public IEnumerator OshiSkill(DuelAction _DuelActionFirstAction)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;
            EffectInformation.Clear();

            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                    case "hSD01-001":
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new string[] { "Stage"};
                        return _DuelField_DetachEnergyMenu.SetupSelectableItems(_DuelActionFirstAction, AddCostToEffectInformation: true, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect, DestroyCostWhenDeAtach: true);
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

            switch (_DuelActionFirstAction.usedCard.cardNumber) {
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
            EffectInformation.Clear();

            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionR.usedCard.cardNumber)
            {
                case "hSD01-015":
                    //we recieve a list, first pos is the number of the dice, second pos is the card from top of cheer
                    serverReturn = JsonConvert.DeserializeObject<List<string>>(_DuelActionR.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                    cheerNumber = "";
                    if (serverReturn.Count > 0)
                        cheerNumber = serverReturn[0];

                    ShowCardEffect(cheerNumber);
                    //target the card
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, new string[] { _DuelActionR.usedCard.cardPosition });
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        duelActionOutput.actionType = "AskAttachTopCheerEnergyToBack";

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");

                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-012":
                    string WillActivate = "";
                    //select if active
                    menuActions.Add(() =>
                    {
                        
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });

                    menuActions.Add(() =>
                    {
                        WillActivate = (string)EffectInformation[0];

                        if (!WillActivate.Equals("Yes"))
                            return null;

                        //we get the powerlist from the server
                        holoPowerList = JsonConvert.DeserializeObject<List<Card>>(_DuelActionR.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

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
                    holoPowerList = JsonConvert.DeserializeObject<List<Card>>(_DuelActionR.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                    menuActions.Add(() =>
                    {
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
                        return null;
                    });

                    menuActions.Add(() =>
                    {
                        List<string> returnList = (List<string>)EffectInformation[0];
                        returnList.AddRange((List<string>)EffectInformation[1]);
                        //go to "PickFromListThenGiveBacKFromHand"
                        duelActionOutput = new DuelAction()
                        {
                            actionObject = JsonConvert.SerializeObject(returnList),
                            actionType = "PickFromListThenGiveBacKFromHand",

                        };
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return dummy();
                    });
                    break;
                case "hSD01-009":
                    //we recieve a list, first pos is the number of the dice, second pos is the card from top of cheer
                    serverReturn = JsonConvert.DeserializeObject<List<string>>(_DuelActionR.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                    string diceRoll = serverReturn[0];

                    cheerNumber = "";
                    if (serverReturn.Count > 1)
                        cheerNumber = serverReturn[1];

                    //if higher than 4, return since the effect dont active and also, the server don't send the energy
                    if (int.Parse(diceRoll) > 4)
                        return;

                    ShowCardEffect(cheerNumber);
                    //target the card
                    menuActions.Add(() =>
                    {

                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" });

                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        duelActionOutput.actionType = "AskAttachTopCheerEnergyToBack";

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");

                        return WaitForServerResponse();

                    });
                    //select if retreat
                    menuActions.Add(() =>
                    {
                        
                        if (int.Parse(diceRoll) < 2)
                        {
                            return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
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

                        duelActionOutput.actionObject = (string)EffectInformation[1];
                        duelActionOutput.actionType = "Retreat";

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");

                        return WaitForServerResponse();

                    });

                    menuActions.Add(() =>
                    {
                        
                        //chama de novo para finalizar
                        List<string> returnList = (List<string>)EffectInformation[0];
                        duelActionOutput.actionObject = returnList[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return dummy();
                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
            
        }
        public void ResolveSuportEffect(DuelAction _DuelActionFirstAction)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;
            EffectInformation.Clear();

            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                case "hBP01-103":
                    menuActions.Add(() =>
                    {
                        
                        return _DuelField_DetachEnergyMenu.SetupSelectableItems(_DuelActionFirstAction);
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
                        
                        return _DuelField_DetachEnergyMenu.SetupSelectableItems(_DuelActionFirstAction);
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
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        //we recieve a list, first pos is the number of the dice, second pos is the card from top of cheer
                        serverReturn = JsonConvert.DeserializeObject<List<string>>(duelActionInput.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        string diceRoll = serverReturn[0];

                        //if not oddnumber, we draw a card calling "Draw" at DuelField, so break
                        if (int.Parse(diceRoll) < 3)
                            return null;

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //then, target one card to assign the energy
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = (List<string>)EffectInformation[0];
                        duelActionInput.actionObject = cardnumber[0];
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelActionInput);
                    });

                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = new DuelAction
                        {
                            usedCard = ((DuelAction)EffectInformation[1]).usedCard,
                            targetCard = ((DuelAction)EffectInformation[1]).targetCard,
                            actionObject = ((DuelAction)EffectInformation[1]).actionObject,
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
                        
                        return _DuelField_DetachEnergyMenu.SetupSelectableItems(_DuelActionFirstAction);
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
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        serverReturn = JsonConvert.DeserializeObject<List<string>>(duelActionInput.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                         diceRoll = serverReturn[0];

                        if (int.Parse(diceRoll) > 3)
                        {
                            var zonesThatPlayerCanSelect = new string[] { "BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" };
                            return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                        }
                        else {
                            return dummy();
                        }
                    });

                    menuActions.Add(() =>
                    {
                        

                        if (int.Parse(diceRoll) < 4)
                            return dummy();

                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });

                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
            
        }
        public void ResolveOnArtEffect(DuelAction _DuelActionR)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;
            EffectInformation.Clear();

            switch (_DuelActionR.usedCard.cardNumber)
            {
                case "hSD01-011":
                    //we recieve a list, first pos is the number of the dice, second pos is the card from top of cheer
                    List<string> serverReturn = JsonConvert.DeserializeObject<List<string>>(_DuelActionR.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                    string cheerNumber = "";
                    if (serverReturn.Count > 0)
                        cheerNumber = serverReturn[0];

                    

                    if (!_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().name.Equals("ときのそら"))
                        return;

                    ShowCardEffect(cheerNumber);
                    //target the card
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, null);
                    });

                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnArtEffect");
                        return WaitForServerResponse();

                    });
                    break;
                case "hSD01-013":
                    //we recieve a list, first pos is the number of the dice, second pos is the card from top of cheer
                    serverReturn = JsonConvert.DeserializeObject<List<string>>(_DuelActionR.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                    string diceRoll = serverReturn[0];

                    cheerNumber = "";
                    if (serverReturn.Count > 1)
                        cheerNumber = serverReturn[1];

                    //if not oddnumber, we draw a card calling "Draw" at DuelField, so break
                    if (int.Parse(diceRoll) == 2 || int.Parse(diceRoll) == 4 || int.Parse(diceRoll) == 6)
                        break;

                    ShowCardEffect(cheerNumber);
                    //target the card
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, new string[] { _DuelActionR.usedCard.cardPosition });
                    });

                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
            
        }
        public IEnumerator RetreatArt(DuelAction _DuelActionR)
        {
            menuActions = new List<Func<IEnumerator>>();
            EffectInformation.Clear();

            
            //pay the cost
            menuActions.Add(() =>
            {
                
                var zonesThatPlayerCanSelect = new string[] { _DuelActionR.usedCard.cardPosition };
                return _DuelField_DetachEnergyMenu.SetupSelectableItems(_DuelActionR, AddCostToEffectInformation: true, zonesThatPlayerCanSelect);
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
                return WaitForServerResponse();
            });
            StartCoroutine(StartMenuSequenceCoroutine());
            
            yield return true;
        }

        private void ShowCardEffect(string cheerNumber)
        {
            //throw new NotImplementedException();
        }
        public IEnumerator StartMenuSequenceCoroutine(bool type = false)
        {
            while (menuActions.Count > 0)
            {
                Func<IEnumerator> nextMenu = menuActions[0];
                menuActions.RemoveAt(0);
                isServerResponseArrive = false;
                isSelectionCompleted = false;
                yield return StartCoroutine(nextMenu());
            }
            isServerResponseArrive = false;
            isSelectionCompleted = false;
            EffectInformation.Clear();
        }
        public IEnumerator dummy()
        {
            yield return new WaitUntil(() => true);
        }
        public IEnumerator WaitForServerResponse()
        {
            yield return new WaitUntil(() => isServerResponseArrive);
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

        internal void ResolveOnBloomEffect(DuelAction duelAction)
        {
        }

        internal void ResolveOnDamageResolveEffect(DuelAction _DuelActionDamageResolveFirst)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;
            EffectInformation.Clear();

            if (!CanActivateDamageStepEffect()) {

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

        private bool CanActivateDamageStepEffect()
        {
            return false;
        }
    }
}

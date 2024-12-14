using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using static DuelField;
using static UnityEngine.GraphicsBuffer;
using Unity.VisualScripting;
using System.Runtime.ConstrainedExecution;
using UnityEngine.UI;
using Unity.VisualScripting.Antlr3.Runtime;

namespace Assets.Scripts.Lib
{
    class EffectController : MonoBehaviour
    {

        private DuelField_ShowListPickThenReorder _DuelField_ShowListPickThenReorder;
        private DuelField_TargetForEffectMenu _DuelField_TargetForEffectMenu;
        private DuelField_DetachCardMenu _DuelField_DetachEnergyOrEquipMenu;
        private DuelField_ShowAlistPickOne _DuelField_ShowAlistPickOne;
        private DuelField_YesOrNoMenu _DuelField_YesOrNoMenu;
        private DuelField_ShowANumberList _DuelField_ShowANumberList;

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
            _DuelField_ShowANumberList = FindAnyObjectByType<DuelField_ShowANumberList>();
            EffectInformation = new List<object>();
        }
        public IEnumerator OshiSkill(DuelAction _DuelActionFirstAction)
        {
            List<Card> holoPowerList;
            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                case "hBP01-006":
                    menuActions.Add(() =>
                    {
                        List<Card> canSelect = new();
                        foreach (Card card in _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>())
                            if (card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン"))
                                canSelect.Add(card);

                        if (canSelect.Count == 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionFirstAction.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiEffect");
                        return WaitForServerResponse();
                    });
                    break;
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
                case "hYS01-001":
                case "hYS01-002":
                case "hYS01-003":
                case "hBP01-004":
                case "hBP01-003":
                case "hBP01-001":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-002":
                    menuActions.Add(() =>
                    {
                        return _DuelField_ShowANumberList.SetupSelectableNumbers(1, 6);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionFirstAction.actionObject = GetLastValue<string>();
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiSPEffect");
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
                case "hSD01-002":
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Player);
                    });
                    menuActions.Add(() =>
                    {
                        var list = _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>();

                        List<Card> canSelect = null;
                        foreach (Card card in list)
                            if (card.cardType.Equals("エール"))
                                canSelect.Add(card);

                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(GetLastValue<DuelAction>(), canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
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
                case "hBP01-001":
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new string[] { "Stage", "Collaboration" };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hYS01-003":
                    menuActions.Add(() =>
                    {
                        List<Card> canSelect = new();
                        foreach (Card card in _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>())
                            if (card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン"))
                                canSelect.Add(card);

                        if (canSelect.Count == 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionFirstAction.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hYS01-004":
                case "hYS01-002":
                case "hBP01-006":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-003":
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (duelAction.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, duelAction.cardList, duelAction.cardList);
                    });
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction);
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = GetLastValue<DuelAction>();
                        List<string> cardnumber = GetLastValue<List<string>>(1);
                        duelAction.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(duelAction, "ResolveOnOshiSPEffect");
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
                case "hBP01-075":
                case "hBP01-057":
                case "hSD01-015":
                case "hSD01-004":
                case "hBP01-016":
                case "hBP01-023":
                case "hBP01-010":
                case "hBP01-015":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-031":
                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });

                    menuActions.Add(() =>
                    {
                        string WillActivate = GetLastValue<string>();

                        if (!_DuelField.GetZone("Favorite", TargetPlayer.Player).GetComponentInChildren<Card>().cardName.Equals("星街すいせい") || !WillActivate.Equals("Yes") || _DuelField.GetZone(_DuelActionR.usedCard.cardPosition, TargetPlayer.Player).GetComponentInChildren<Card>().attachedEnergy.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionR);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-071":
                    menuActions.Add(() =>
                    {
                        var list = _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>();

                        List<Card> canSelect = null;
                        foreach (Card card in list)
                            if (card.cardName.Equals("座員"))
                                canSelect.Add(card);

                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(GetLastValue<DuelAction>(), canSelect, canSelect, MaximumCanPick: 1);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-100":
                    menuActions.Add(() =>
                    {
                        var list = _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>();

                        List<Card> canSelect = null;
                        foreach (Card card in list)
                            if (card.cardType.Equals("エール"))
                                canSelect.Add(card);

                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(GetLastValue<DuelAction>(), canSelect, canSelect, MaximumCanPick: 3);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
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
                        if (holoPowerList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
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
                        if (holoPowerList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, holoPowerList, holoPowerList);
                    });

                    menuActions.Add(() =>

                    {
                        isSelectionCompleted = false;
                        //fetch player hand
                        List<Card> secondList = GameObject.Find("MatchField").transform.Find("PlayersHands/PlayerHand").GetComponentsInChildren<Card>().ToList();

                        List<string> pickedFromHoloPower = (List<string>)EffectInformation[0];
                        secondList.Add(new Card(pickedFromHoloPower[0]));

                        if (secondList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        // Setup the second menu
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, secondList, secondList);

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
                case "hBP01-099":
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
                        int diceRoll = GetLastValue<int>(1);
                        if (!IsOddNumber(diceRoll) && _DuelField.CountBackStageTotal(onlyBackstage:true, TargetPlayer.Oponnent) == 0)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Oponnent, new[] {"BackStage5", "BackStage4", "BackStage3", "BackStage2", "BackStage1"});
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-096":
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
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        diceRoll = GetLastValue<int>(1);
                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        List<Card> canSelect = JsonConvert.DeserializeObject<List<Card>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (!IsOddNumber(diceRoll) || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionR.actionObject = GetLastValue<List<string>>()[0];
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-033":
                    diceRoll = 0;
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

                        if (diceRoll == 1 || diceRoll == 3 || diceRoll == 5)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, target: TargetPlayer.Player);
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelaction = GetLastValue<DuelAction>();
                        _DuelField.GenericActionCallBack(duelaction, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-036":
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, target: TargetPlayer.Player);
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelaction = GetLastValue<DuelAction>();
                        _DuelField.GenericActionCallBack(duelaction, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-009":
                    diceRoll = 0;
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
                case "hBP01-098":
                    menuActions.Add(() =>
                    {
                        Card[] _cardList = _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>();

                        List<Card> selectable = new();
                        foreach (Card card in _cardList)
                        {
                            if ((card.cardType.Equals("エール")))
                            {
                                selectable.Add(card);
                            }
                        }

                        if (selectable.Count < 1)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, selectable, selectable);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionR.actionObject = GetLastValue<List<string>>()[0];
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-020":
                    menuActions.Add(() =>
                    {
                        if (!_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().cardTag.Contains("#ID"))
                        {
                            menuActions.Clear();
                            return null;
                        }

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
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        if (GetLastValue<int>(1) < 3 || _DuelField.GetZone("CheerDeck", TargetPlayer.Player).GetComponentsInChildren<Card>().Count() == 0)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelActionInput);
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction tempaction = GetLastValue<DuelAction>();
                        duelActionOutput = new DuelAction
                        {
                            usedCard = tempaction.usedCard,
                            targetCard = tempaction.targetCard,
                            actionObject = tempaction.actionObject,
                        };

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-039":
                    menuActions.Add(() =>
                    {
                        if (!_DuelField.GetZone("Favourite", TargetPlayer.Player).GetComponentInChildren<Card>().cardName.Equals("兎田ぺこら"))
                        {
                            menuActions.Clear();
                            return null;
                        }

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
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        if (IsOddNumber(GetLastValue<int>(1)) || _DuelField.GetZone("CheerDeck", TargetPlayer.Player).GetComponentsInChildren<Card>().Count() == 0)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelActionInput);
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction tempaction = GetLastValue<DuelAction>();
                        duelActionOutput = new DuelAction
                        {
                            usedCard = tempaction.usedCard,
                            targetCard = tempaction.targetCard,
                            actionObject = tempaction.actionObject,
                        };

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-101":
                case "hSD01-019":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput.actionObject = GetLastValue<List<string>>()[0];
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
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
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
                        if (duelActionInput.cardList.Count == 0 || filteresList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
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
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
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
                        if (GetLastValue<int>(1) < 3 || duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return dummy();
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
                        if (duelActionInput.cardList.Count == 0 || filteresList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
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
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
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
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
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
                case "hBP01-031-約束の力":
                case "hBP01-035-アキロゼ幻想曲":
                case "hBP01-057-漆黒の翼で誘おう":
                case "hBP01-051-エールを束ねて":
                case "hBP01-037-秘密の合鍵":
                case "hBP01-027-アクセスコード：ID":
                case "hSD01-006-SorAZ シンパシー":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-042-きｔらあああ":
                case "hBP01-038-こんぺこー！":
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
                case "hBP01-072-WAZZUP!!":
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
                        int diceRoll = GetLastValue<int>();

                        bool hasRedEnergy = false;

                        Card thisCard = _DuelActionR.usedCard.cardPosition.Equals("Stage") ? _DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>() : _DuelField.GetZone("Collaboration", TargetPlayer.Player).GetComponentInChildren<Card>();

                        foreach (GameObject cardObj in thisCard.attachedEnergy)
                        {
                            Card card = cardObj.GetComponent<Card>();
                            if (card.color.Equals("赤"))
                                hasRedEnergy = true;
                        }

                        if (IsOddNumber(diceRoll) || !hasRedEnergy)
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
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
                case "hBP01-062-キッケリキー！":
                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });
                    menuActions.Add(() =>
                    {
                        string WillActivate = (string)EffectInformation[0];

                        List<Card> canSelect = _DuelField.cardHolderPlayer.GetComponentsInChildren<Card>().ToList();

                        if (!WillActivate.Equals("Yes") || _DuelField.cardsPlayer.Count > 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionR.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-020-みんな一緒に":
                    menuActions.Add(() =>
                    {
                        if (_DuelField.GetZone("CardCheer", TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        List<Card> selectableList = JsonConvert.DeserializeObject<List<Card>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (selectableList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, selectableList, selectableList);
                    });
                    menuActions.Add(() =>
                    {

                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, GetAreasThatContainsCardWithColorOrTagOrName(tag: "#秘密結社holoX"));
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction da = GetLastValue<DuelAction>();
                        da.actionObject = GetLastValue<List<string>>(1)[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                default:
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionR, "resolveArt");
                        return dummy();
                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
        }
        internal void ResolveOnDamageResolveEffect(DuelAction _DuelActionDamageResolveFirst)
        {
            bool WillActivate = false;

            if (!CanActivateDamageStepEffect() || !WillActivate)
            {

                DuelAction _response = new()
                {
                    actionObject = "false"
                };

                _DuelField.GenericActionCallBack(_response, "AskServerToResolveDamageToHolomem");
                return;
            }

            switch (_DuelActionDamageResolveFirst.usedCard.cardNumber)
            {
                case "hSD01-011":
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
        }
        internal void ResolveOnAttachEffect(DuelAction _DuelActionR)
        {
            List<Card> energyList;

            switch (_DuelActionR.usedCard.cardNumber)
            {
                case "hBP01-125":
                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });
                    menuActions.Add(() =>
                    {
                        string WillActivate = (string)EffectInformation[0];

                        List<Card> canSelect = _DuelField.cardHolderPlayer.GetComponentsInChildren<Card>().ToList();

                        if (!WillActivate.Equals("Yes") || _DuelField.cardsPlayer.Count > 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionR.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(_DuelActionR, "ResolveOnAttachEffect");
                        return WaitForServerResponse();
                    });
                    break;
            }

            StartCoroutine(StartMenuSequenceCoroutine());
        }
        internal void ResolveOnBloomEffect(DuelAction _DuelActionFirstAction)
        {
            menuActions = new List<Func<IEnumerator>>();

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                case "hBP01-074":
                    menuActions.Add(() =>
                    {
                        if (!_DuelField.GetZone(_DuelActionFirstAction.usedCard.cardPosition, TargetPlayer.Player).GetComponentInChildren<Card>().bloomChild.Last().GetComponent<Card>().bloomLevel.Equals("Debut"))
                        {
                            menuActions.Clear();
                            return null;
                        }

                        Card[] _cardList = _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>();

                        List<Card> selectable = new();
                        foreach (Card card in _cardList)
                        {
                                if ((card.bloomLevel.Equals("Debut") || card.bloomLevel.Equals("1st")))
                            {
                                selectable.Add(card);
                            }
                        }
                        if (selectable.Count < 1)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, selectable, selectable);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionFirstAction.actionObject = GetLastValue<List<string>>()[0];
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-070":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput.actionObject = GetLastValue<List<string>>()[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-043":
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionFirstAction);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-014":
                case "hBP01-013":
                case "hBP01-030":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-060":
                    menuActions.Add(() =>
                    {
                        List<Card> canSelect = _DuelField.cardHolderPlayer.GetComponentsInChildren<Card>().ToList();
                        if (canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionFirstAction.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-012":
                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });
                    menuActions.Add(() =>
                    {
                        string WillActivate = (string)EffectInformation[0];

                        if (!WillActivate.Equals("Yes") || _DuelField.cardsPlayer.Count > 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        _DuelActionFirstAction.actionObject = WillActivate;
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        int diceRoll = GetLastValue<int>();
                        if (diceRoll > 3)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        RollDiceTilNotAbleOrDontWantTo(_DuelActionFirstAction);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction da = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        List<Card> canSelect = _DuelField.cardHolderPlayer.GetComponentsInChildren<Card>().ToList();
                        if (da.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(da, da.cardList, da.cardList);
                    });
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Player);
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelaction = GetLastValue<DuelAction>();
                        List<string> cardnumber = GetLastValue<List<string>>(1);
                        duelaction.actionObject = cardnumber[0];
                        _DuelField.GenericActionCallBack(duelaction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-090":
                    string WillActivate = "";
                    //select if active
                    menuActions.Add(() =>
                    {
                        if (_DuelField.GetZone("CardCheer", TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
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
                        List<Card> cheerList = JsonConvert.DeserializeObject<List<Card>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (cheerList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        //show the list to the player, player pick 1
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, cheerList, cheerList);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = (List<string>)EffectInformation[1];
                        _DuelActionFirstAction.usedCard = new CardData() { cardNumber = cardnumber[0] };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, TargetPlayer.Player);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[2];
                        duelActionOutput.actionObject = WillActivate;

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
                        return dummy();
                    });
                    break;
                case "hBP01-037":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");

                        return WaitForServerResponse();

                    });
                    break;
                case "hBP01-081":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        string[] targetZones = GetAreasThatContainsCardWithColorOrTagOrName(color: "青");

                        if (targetZones.Length < 1)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player, targetZones);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");

                        return WaitForServerResponse();

                    });
                    break;
                case "hBP01-054":
                    menuActions.Add(() =>
                    {
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        string[] targetZones = GetAreasThatContainsCardWithColorOrTagOrName(tag: "#ID");
                        string[] removeZones = GetAreasThatContainsCardWithColorOrTagOrName(name: "アイラニ・イオフィフティーン");

                        string[] filteredZones = targetZones.Except(removeZones).ToArray();

                        if (targetZones.Length < 1)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player, filteredZones);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");

                        return WaitForServerResponse();

                    });
                    break;
                case "hBP01-035":
                    menuActions.Add(() =>
                    {
                        Card[] _cardList = _DuelField.GetZone("Arquive", TargetPlayer.Player).GetComponentsInChildren<Card>();

                        List<Card> selectable = new();
                        List<Card> allequipe = new();
                        foreach (Card card in _cardList)
                        {
                            if ((card.cardType.Equals("サポート・ツール") || card.cardType.Equals("サポート・マスコット") || card.cardType.Equals("サポート・ファン")))
                            {
                                allequipe.Add(card);
                                if (!_DuelField.HasRestrictionsToPlayEquipCheckField(card))
                                {
                                    selectable.Add(card);
                                }
                            }
                        }

                        if (selectable.Count < 1)
                        {
                            menuActions.Clear();
                            return null;
                        }

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, allequipe, selectable);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionFirstAction.actionObject = GetLastValue<List<string>>()[0];
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-094":
                    menuActions.Add(() =>
                    {
                        if (_DuelField.GetZone("CardCheer", TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        _DuelField.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        List<Card> selectableList = JsonConvert.DeserializeObject<List<Card>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (selectableList.Count == 0)
                        {
                            menuActions.Clear();
                            return null;
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, selectableList, selectableList);
                    });
                    menuActions.Add(() =>
                    {
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, TargetPlayer.Player, GetAreasThatContainsCardWithColorOrTagOrName(tag: "#Promise"));
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction da = GetLastValue<DuelAction>();
                        da.actionObject = GetLastValue<List<string>>(1)[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
            }
        }
        public IEnumerator RetreatArt(DuelAction _DuelActionR)
        {
            //pay the cost
            menuActions.Add(() =>
            {

                var zonesThatPlayerCanSelect = new string[] { _DuelActionR.usedCard.cardPosition };
                return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionR, AddCostToEffectInformation: true, zonesThatPlayerCanSelect);
            });
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
                _DuelField.GenericActionCallBack(duelActionOutput, "Retreat");
                return dummy();
            });
            StartCoroutine(StartMenuSequenceCoroutine());

            yield return true;
        }
        private bool IsOddNumber(int x)
        {
            if ((x & 1) == 0)
                return false;
            else
                return true;
        }
        public string[] GetAreasThatContainsCardWithColorOrTagOrName(string color = "", string tag = "", string name = "")
        {
            List<Card> allAttachments = new();
            List<string> list = new();

            allAttachments.AddRange(_DuelField.GetZone("BackStage1", TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(_DuelField.GetZone("BackStage2", TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(_DuelField.GetZone("BackStage3", TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(_DuelField.GetZone("BackStage4", TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(_DuelField.GetZone("BackStage5", TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(_DuelField.GetZone("Collaboration", TargetPlayer.Player).GetComponentsInChildren<Card>());

            if (!string.IsNullOrEmpty(color))
            {
                foreach (Card card in allAttachments)
                    if (card.color.Equals(color))
                        list.Add(card.gameObject.transform.parent.name);
            }
            else if (!string.IsNullOrEmpty(tag))
            {
                foreach (Card card in allAttachments)
                    if (card.cardTag.Contains(tag))
                        list.Add(card.gameObject.transform.parent.name);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                foreach (Card card in allAttachments)
                    if (card.cardName.Contains(name))
                        list.Add(card.gameObject.transform.parent.name);
            }

            return list.ToArray();
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
                            return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelAction, IsACheer: false);
                        });
                        menuActions.Insert(1, () =>
                        {
                            DuelAction da = GetLastValue<DuelAction>();
                            EffectInformation.RemoveAt(EffectInformation.Count - 1);
                            _DuelField.GenericActionCallBack(da, "ResolveRerollEffect");
                            return WaitForServerResponse();
                        });
                        menuActions.Insert(2, () =>
                        {
                            RollDiceTilNotAbleOrDontWantTo(_DuelAction);
                            return dummy();
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using static DuelField;
using Unity.VisualScripting;

    class EffectController : MonoBehaviour
    {
        static public EffectController INSTANCE;

        private DuelField_ShowListPickThenReorder _DuelField_ShowListPickThenReorder;
        private DuelField_TargetForEffectMenu _DuelField_TargetForEffectMenu;
        private DuelField_DetachCardMenu _DuelField_DetachEnergyOrEquipMenu;
        private DuelField_ShowAlistPickOne _DuelField_ShowAlistPickOne;
        private DuelField_YesOrNoMenu _DuelField_YesOrNoMenu;
        private DuelField_ShowANumberList _DuelField_ShowANumberList;

        private List<Func<IEnumerator>> menuActions = new List<Func<IEnumerator>>();

        public List<DuelAction> EffectInformation = new();
        private object lastRetrievedValue;

        public DuelAction duelActionOutput;
        public DuelAction duelActionInput;

        public bool isSelectionCompleted = false;
        public bool isServerResponseArrive = false;

        void Start()
        {
            INSTANCE = this;

            _DuelField_ShowListPickThenReorder = FindAnyObjectByType<DuelField_ShowListPickThenReorder>();
            _DuelField_TargetForEffectMenu = FindAnyObjectByType<DuelField_TargetForEffectMenu>();
            _DuelField_ShowAlistPickOne = FindAnyObjectByType<DuelField_ShowAlistPickOne>();
            _DuelField_DetachEnergyOrEquipMenu = FindAnyObjectByType<DuelField_DetachCardMenu>();
            _DuelField_YesOrNoMenu = FindAnyObjectByType<DuelField_YesOrNoMenu>();
            _DuelField_ShowANumberList = FindAnyObjectByType<DuelField_ShowANumberList>();
        }
        public IEnumerator OshiSkill(DuelAction _DuelActionFirstAction)
        {
            List<CardData> holoPowerList;
            List<string> serverReturn;
            string cheerNumber;

            switch (_DuelActionFirstAction.usedCard.cardNumber)
            {
                case "hBP01-006":
                    menuActions.Add(() =>
                    {
                        List<CardData> canSelect = new();
                        foreach (CardData card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList())
                        if (card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン"))
                                canSelect.Add(card);

                        if (canSelect.Count == 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionFirstAction.actionObject = cardnumber[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-001":
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.Stage};
                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionFirstAction, AddCostToEffectInformation: true, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5, Lib.GameZone.Collaboration };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Player, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[1];
                        duelActionOutput.actionObject = JsonConvert.SerializeObject(EffectInformation[0].usedCard, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        duelActionOutput.cheerCostCard = null;
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnOshiEffect");
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
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiEffect");
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
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiSPEffect");
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
            List<CardData> holoPowerList;
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
                        var list = DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList();

                        List<CardData> canSelect = null;
                        foreach (CardData card in list)
                            if (card.cardType.Equals("エール"))
                                canSelect.Add(card);

                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(GetLastValue<DuelAction>(), canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-001":
                    //select the card to return to back
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-001":
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hYS01-003":
                    menuActions.Add(() =>
                    {
                        List<CardData> canSelect = new();
                        foreach (Card card in DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>())
                            if (card.cardType.Equals("ホロメン") || card.cardType.Equals("Buzzホロメン"))
                                canSelect.Add(card.ToCardData());

                        if (canSelect.Count == 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionFirstAction.actionObject = cardnumber[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hYS01-004":
                case "hYS01-002":
                case "hBP01-006":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnOshiSPEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-003":
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        if (duelAction.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
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
                        DuelField.INSTANCE.GenericActionCallBack(duelAction, "ResolveOnOshiSPEffect");
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
            List<CardData> holoPowerList;

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
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
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

                        if (!DuelField.INSTANCE.GetZone(Lib.GameZone.Favourite, TargetPlayer.Player).GetComponentInChildren<Card>().cardName.Equals("星街すいせい") || !WillActivate.Equals("Yes") || DuelField.INSTANCE.GetZone(_DuelActionR.usedCard.curZone, TargetPlayer.Player).GetComponentInChildren<Card>().attachedEnergy.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionR);
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-071":
                    menuActions.Add(() =>
                    {
                        var list = DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList();

                        List<CardData> canSelect = null;
                        foreach (CardData card in list)
                            if (card.cardName.Equals("座員"))
                                canSelect.Add(card);

                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(GetLastValue<DuelAction>(), canSelect, canSelect, MaximumCanPick: 1);
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-100":
                    menuActions.Add(() =>
                    {
                        var list = DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList();

                        List<CardData> canSelect = null;
                        foreach (CardData card in list)
                            if (card.cardType.Equals("エール"))
                                canSelect.Add(card);

                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(GetLastValue<DuelAction>(), canSelect, canSelect, MaximumCanPick: 3);
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-012":
                    DuelAction WillActivate;
                    //select if active
                    menuActions.Add(() =>
                    {
                        if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });

                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });

                    menuActions.Add(() =>
                    {
                        WillActivate = EffectInformation[0];

                        if (!WillActivate.Equals("Yes"))
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        holoPowerList = JsonConvert.DeserializeObject<List<CardData>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (holoPowerList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, holoPowerList, holoPowerList);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionR.usedCard = EffectInformation[1].usedCard;
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, new Lib.GameZone[] { Lib.GameZone.Stage });
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[2];

                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return dummy();
                    });
                    break;
                case "hSD01-007":
                    //we get the powerlist from the server
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });

                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        holoPowerList = JsonConvert.DeserializeObject<List<CardData>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (holoPowerList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, holoPowerList, holoPowerList);
                    });

                    menuActions.Add(() =>

                    {
                        isSelectionCompleted = false;
                        //fetch player hand
                        EffectInformation[0].cardList = GameObject.Find("MatchField").transform.Find("PlayerHand").GetComponentsInChildren<Card>().Select(item => item.ToCardData()).ToList();

                        List<CardData> secondList = (EffectInformation[0].cardList);

                        if (secondList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        // Setup the second menu
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, secondList, secondList);

                    });

                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(EffectInformation[0], "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-099":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
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
                        if (!IsOddNumber(diceRoll) && DuelField.INSTANCE.CountBackStageTotal(onlyBackstage:true, TargetPlayer.Oponnent) == 0)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Oponnent, new[] {Lib.GameZone.BackStage5, Lib.GameZone.BackStage4, Lib.GameZone.BackStage3, Lib.GameZone.BackStage2, Lib.GameZone.BackStage1});
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(GetLastValue<DuelAction>(), "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-096":
                    int diceRoll = 0;
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        diceRoll = GetLastValue<int>(1);
                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        List<CardData> canSelect = JsonConvert.DeserializeObject<List<CardData>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

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
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-033":
                    diceRoll = 0;
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
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
                        DuelField.INSTANCE.GenericActionCallBack(duelaction, "ResolveOnCollabEffect");
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
                        DuelField.INSTANCE.GenericActionCallBack(duelaction, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-009":
                    diceRoll = 0;
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
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

                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player, new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 });
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        duelActionOutput.actionType = "AskAttachTopCheerEnergyToBack";

                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");

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
                            EffectInformation.Add(new DuelAction { yesOrNo = false });
                        }
                        return dummy();
                    });
                    //inform server if retreat
                    menuActions.Add(() =>
                    {
                        duelActionOutput.actionObject = GetLastValue<string>();
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();

                    });
                    break;
                case "hBP01-098":
                    menuActions.Add(() =>
                    {
                        CardData[] _cardList = DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToArray();

                        List<CardData> selectable = new();
                        foreach (CardData card in _cardList)
                        {
                            if ((card.cardType.Equals("エール")))
                            {
                                selectable.Add(card);
                            }
                        }

                        if (selectable.Count < 1)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, selectable, selectable);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionR.actionObject = GetLastValue<List<string>>()[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-020":
                    menuActions.Add(() =>
                    {
                        if (!DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentInChildren<Card>().cardTag.Contains("#ID"))
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        if (GetLastValue<int>(1) < 3 || DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player).GetComponentsInChildren<Card>().Count() == 0)
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

                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-039":
                    menuActions.Add(() =>
                    {
                        if (!DuelField.INSTANCE.GetZone(Lib.GameZone.Favourite, TargetPlayer.Player).GetComponentInChildren<Card>().cardName.Equals("兎田ぺこら"))
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        if (IsOddNumber(GetLastValue<int>(1)) || DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player).GetComponentsInChildren<Card>().Count() == 0)
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

                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-101":
                case "hSD01-019":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput.actionObject = GetLastValue<List<string>>()[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return WaitForServerResponse();
                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());

        }
        public void ResolveSuportEffect(DuelAction _DuelActionFirstAction)
        {
            List<CardData> energyList;

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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    //we recieve the list then callback again to finish the effect
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        //selected[0] = EffectInformation[1][0]
                        DuelField.INSTANCE.GenericActionCallBack(EffectInformation[1], "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-102":

                    if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                        break;

                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

                        List<CardData> filteresList = new();
                        foreach (CardData card in duelActionInput.cardList)
                        {
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-016":
                case "hSD01-017":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return dummy();
                    });
                    break;
                case "hSD01-018":

                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

                        List<CardData> filteresList = new();
                        foreach (CardData card in duelActionInput.cardList)
                        {
                            if (card.cardType.Equals("サポート・アイテム・LIMITED") || card.cardType.Equals("サポート・イベント・LIMITED") || card.cardType.Equals("サポート・スタッフ・LIMITED"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        if (duelActionInput.cardList.Count == 0 || filteresList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, 1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {

                        duelActionOutput = (DuelAction)EffectInformation[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    //we recieve the list then callback again to finish the effect
                    menuActions.Add(() =>
                    {

                        duelActionInput = DuelField.INSTANCE.curResDA;
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                       // var selected[0] = (List<string>);
                        DuelField.INSTANCE.GenericActionCallBack(EffectInformation[1], "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-020":

                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionFirstAction);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

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

                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-021":

                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

                        List<CardData> filteresList = new();
                        foreach (CardData card in duelActionInput.cardList)
                        {
                            if (card.cardName.Equals("ときのそら") || card.cardName.Equals("AZKi") || card.cardName.Equals("SorAZ"))
                            {
                                filteresList.Add(card);
                            }

                        }
                        if (duelActionInput.cardList.Count == 0 || filteresList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowListPickThenReorder.SetupSelectableItems(duelActionInput, duelActionInput.cardList, filteresList, true, -1);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-104":

                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(EffectInformation[0], "ResolveOnSupportEffect");
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
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(EffectInformation[0]);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[1];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    //from the list of energy recieved, pick one
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    menuActions.Add(() =>
                    {
                       // --duelActionInput.actionObject = cardnumber[0];
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(EffectInformation[2]);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = new DuelAction
                        {
                            usedCard = ((DuelAction)EffectInformation[3]).usedCard,
                            targetCard = ((DuelAction)EffectInformation[3]).targetCard,
                            actionObject = ((DuelAction)EffectInformation[3]).actionObject,
                        };
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-106":

                    //select the card to return to back
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-108":

                    //select the card to return to back
                    menuActions.Add(() =>
                    {
                        var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, target: TargetPlayer.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                    });
                    //send both cost and targer(color) to the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-109":

                    if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

                        List<CardData> filteresList = new();
                        foreach (CardData card in duelActionInput.cardList)
                        {
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-111":

                    if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

                        List<CardData> filteresList = new();
                        foreach (CardData card in duelActionInput.cardList)
                        {
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-113":
                    if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

                        List<CardData> filteresList = new();
                        foreach (CardData card in duelActionInput.cardList)
                        {
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-107":

                    if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                        break;


                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;

                        List<CardData> filteresList = new();
                        foreach (CardData card in duelActionInput.cardList)
                        {
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-112":

                    string diceRoll = "-1";
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnSupportEffect");
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
                            var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnSupportEffect");
                        return WaitForServerResponse();
                    });

                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());

        }
        public void ResolveOnArtEffect(DuelAction _DuelActionR)
        {
            List<CardData> energyList;

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
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-042-きｔらあああ":
                case "hBP01-038-こんぺこー！":
                case "hSD01-011-デスティニーソング":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-011-SorAZ グラビティ":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        if (!DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentInChildren<Card>().name.Equals("ときのそら") || !DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentInChildren<Card>().name.Equals("SorAZ"))
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
                        DuelAction da = DuelField.INSTANCE.curResDA;
                        _duelaction.actionObject = da.actionObject;

                        DuelField.INSTANCE.GenericActionCallBack(_duelaction, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-072-WAZZUP!!":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
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

                        Card thisCard = _DuelActionR.usedCard.curZone.Equals(Lib.GameZone.Stage) ? DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentInChildren<Card>() : DuelField.INSTANCE.GetZone(Lib.GameZone.Collaboration, TargetPlayer.Player).GetComponentInChildren<Card>();

                        foreach (GameObject cardObj in thisCard.attachedEnergy)
                        {
                            CardData card = cardObj.GetComponent<Card>().ToCardData();
                            if (card.color.Equals("赤"))
                                hasRedEnergy = true;
                        }

                        if (IsOddNumber(diceRoll) || !hasRedEnergy)
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hSD01-013-越えたい未来":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        RollDiceTilNotAbleOrDontWantTo(_DuelActionR);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
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
                        var WillActivate = EffectInformation[0].yesOrNo;

                        List<CardData> canSelect = DuelField.INSTANCE.cardHolderPlayer.GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList();

                        if (!WillActivate || DuelField.INSTANCE.cardHolderPlayer.childCount > 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionR.actionObject = cardnumber[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnArtEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-020-みんな一緒に":
                    menuActions.Add(() =>
                    {
                        if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        List<CardData> selectableList = JsonConvert.DeserializeObject<List<CardData>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (selectableList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                default:
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "resolveArt");
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

                DuelField.INSTANCE.GenericActionCallBack(_response, "AskServerToResolveDamageToHolomem");
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
            List<CardData> energyList;

            switch (_DuelActionR.usedCard.cardNumber)
            {
                case "hBP01-125":
                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });
                    menuActions.Add(() =>
                    {
                        var WillActivate = EffectInformation[0].yesOrNo;

                        List<CardData> canSelect = DuelField.INSTANCE.cardHolderPlayer.GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList();

                        if (!WillActivate || DuelField.INSTANCE.cardHolderPlayer.childCount > 0 || canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionR.actionObject = cardnumber[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionR, "ResolveOnAttachEffect");
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
                        if (!DuelField.INSTANCE.GetZone(_DuelActionFirstAction.usedCard.curZone, TargetPlayer.Player).GetComponentInChildren<Card>().bloomChild.Last().GetComponent<Card>().bloomLevel.Equals("Debut"))
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        var _cardList = DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData());

                        List<CardData> selectable = new();
                        foreach (CardData card in _cardList)
                        {
                                if ((card.bloomLevel.Equals("Debut") || card.bloomLevel.Equals("1st")))
                            {
                                selectable.Add(card);
                            }
                        }
                        if (selectable.Count < 1)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, selectable, selectable);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionFirstAction.actionObject = GetLastValue<List<string>>()[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-070":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        duelActionInput = DuelField.INSTANCE.curResDA;
                        if (duelActionInput.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(duelActionInput, duelActionInput.cardList, duelActionInput.cardList);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput.actionObject = GetLastValue<List<string>>()[0];
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
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
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-014":
                case "hBP01-013":
                case "hBP01-030":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-060":
                    menuActions.Add(() =>
                    {
                        List<CardData> canSelect = DuelField.INSTANCE.cardHolderPlayer.GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList();
                        if (canSelect.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, canSelect, canSelect);
                    });
                    menuActions.Add(() =>
                    {
                        List<string> cardnumber = GetLastValue<List<string>>();
                        _DuelActionFirstAction.actionObject = cardnumber[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
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
                        var WillActivate = EffectInformation[0].yesOrNo;

                        if (!WillActivate || DuelField.INSTANCE.cardHolderPlayer.childCount > 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }
                        _DuelActionFirstAction.yesOrNo = WillActivate;
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        int diceRoll = GetLastValue<int>();
                        if (diceRoll > 3)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        RollDiceTilNotAbleOrDontWantTo(_DuelActionFirstAction);
                        return dummy();
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction da = DuelField.INSTANCE.curResDA;
                        List<CardData> canSelect = DuelField.INSTANCE.cardHolderPlayer.GetComponentsInChildren<Card>().ToList().Select(item => item.ToCardData()).ToList();
                        if (da.cardList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
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
                        DuelField.INSTANCE.GenericActionCallBack(duelaction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-090":
                    string WillActivate = "";
                    //select if active
                    menuActions.Add(() =>
                    {
                        if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }

                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });

                    menuActions.Add(() =>
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                    });

                    menuActions.Add(() =>
                    {
                        var WillActivate = EffectInformation[0].yesOrNo;

                        if (!WillActivate)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        List<CardData> cheerList = JsonConvert.DeserializeObject<List<CardData>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        if (cheerList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        //show the list to the player, player pick 1
                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, cheerList, cheerList);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionFirstAction.usedCard = EffectInformation[1].usedCard;
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionFirstAction, TargetPlayer.Player);
                    });
                    menuActions.Add(() =>
                    {
                        duelActionOutput = (DuelAction)EffectInformation[2];
                        duelActionOutput.actionObject = WillActivate;

                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
                        return dummy();
                    });
                    break;
                case "hBP01-037":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");

                        return WaitForServerResponse();

                    });
                    break;
                case "hBP01-081":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        Lib.GameZone[] targetZones = GetAreasThatContainsCardWithColorOrTagOrName(color: "青");

                        if (targetZones.Length < 1)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player, targetZones);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");

                        return WaitForServerResponse();

                    });
                    break;
                case "hBP01-054":
                    menuActions.Add(() =>
                    {
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    //target the card
                    menuActions.Add(() =>
                    {
                        Lib.GameZone[] targetZones = GetAreasThatContainsCardWithColorOrTagOrName(tag: "#ID");
                        Lib.GameZone[] removeZones = GetAreasThatContainsCardWithColorOrTagOrName(name: "アイラニ・イオフィフティーン");

                        Lib.GameZone[] filteredZones = targetZones.Except(removeZones).ToArray();

                        if (targetZones.Length < 1)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, TargetPlayer.Player, filteredZones);
                    });
                    //inform the server
                    menuActions.Add(() =>
                    {
                        duelActionOutput = GetLastValue<DuelAction>();
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");

                        return WaitForServerResponse();

                    });
                    break;
                case "hBP01-035":
                    menuActions.Add(() =>
                    {
                        var _cardList = DuelField.INSTANCE.GetZone(Lib.GameZone.Arquive, TargetPlayer.Player).GetComponentsInChildren<Card>();

                        List<CardData> selectable = new();
                        List<CardData> allequipe = new();
                        foreach (Card card in _cardList)
                        {
                            if ((card.cardType.Equals("サポート・ツール") || card.cardType.Equals("サポート・マスコット") || card.cardType.Equals("サポート・ファン")))
                            {
                                allequipe.Add(card.ToCardData());
                                if (!DuelField.INSTANCE.HasRestrictionsToPlayEquipCheckField(card))
                                {
                                    selectable.Add(card.ToCardData());
                                }
                            }
                        }

                        if (selectable.Count < 1)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
                        }

                        return _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionFirstAction, allequipe, selectable);
                    });
                    menuActions.Add(() =>
                    {
                        _DuelActionFirstAction.actionObject = GetLastValue<List<string>>()[0];
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    break;
                case "hBP01-094":
                    menuActions.Add(() =>
                    {
                        if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, TargetPlayer.Player).gameObject.transform.childCount - 1 < 1)
                        {
                            menuActions.Clear();
                            return dummy();
                        }
                        DuelField.INSTANCE.GenericActionCallBack(_DuelActionFirstAction, "ResolveOnBloomEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Add(() =>
                    {
                        DuelAction duelAction = DuelField.INSTANCE.curResDA;
                        List<CardData> selectableList = JsonConvert.DeserializeObject<List<CardData>>(duelAction.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                        if (selectableList.Count == 0)
                        {
                            menuActions.Clear();
                            return EmptyCoroutine();
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
                        DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "ResolveOnBloomEffect");
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

                var zonesThatPlayerCanSelect = new Lib.GameZone[] { _DuelActionR.usedCard.curZone };
                return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(_DuelActionR, AddCostToEffectInformation: true, zonesThatPlayerCanSelect);
            });
            //select the card to return to back
            menuActions.Add(() =>
            {
                var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
                return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
            });
            //send both cost and targer(color) to the server
            menuActions.Add(() =>
            {
                duelActionOutput = (DuelAction)EffectInformation[1];
                DuelField.INSTANCE.GenericActionCallBack(duelActionOutput, "Retreat");
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
        public Lib.GameZone[] GetAreasThatContainsCardWithColorOrTagOrName(string color = "", string tag = "", string name = "")
        {
            List<Card> allAttachments = new();
            List<string> list = new();

            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage1, TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage2, TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage3, TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage4, TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage5, TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentsInChildren<Card>());
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.Collaboration, TargetPlayer.Player).GetComponentsInChildren<Card>());

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

            return list.Select(x => DuelField.INSTANCE.GetZoneByString(x)).ToArray();
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
        private IEnumerator EmptyCoroutine()
        {
            yield break;
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
            List<CardData> cardList = new();

        try
        {
            cardList.AddRange(GameObject.Find(Lib.GameZone.Collaboration.ToString()).GetComponentsInChildren<Card>(true).Select(item => item.ToCardData()));
            cardList.AddRange(GameObject.Find(Lib.GameZone.Stage.ToString()).GetComponentsInChildren<Card>(true).Select(item => item.ToCardData()));
            cardList.AddRange(GameObject.Find(Lib.GameZone.CardCheer.ToString()).GetComponentsInChildren<Card>(true).Select(item => item.ToCardData()));
        }
        catch (Exception e) {
            Console.WriteLine("olhar saporra dps");
        }

            foreach (CardData c in cardList)
            {
            if (c.GetCardInfo().cardType == null)
                return false;

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
        private List<CardData> CanReRollDice()
        {
            List<Card> allAttachments = new();
            HashSet<CardData> uniqueParents = new HashSet<CardData>();

            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage1, TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage2, TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage3, TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage4, TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.BackStage5, TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, TargetPlayer.Player).GetComponentsInChildren<Card>(true));
            allAttachments.AddRange(DuelField.INSTANCE.GetZone(Lib.GameZone.Collaboration, TargetPlayer.Player).GetComponentsInChildren<Card>(true));

            foreach (Card card in allAttachments)
            {
                if (card.cardNumber.Equals("hBP01-123"))
                {
                    CardData parentCard = card.transform.parent.GetComponent<Card>().ToCardData();
                    uniqueParents.Add(parentCard);
                }
            }

            return uniqueParents.ToList();
        }
        private IEnumerator RollDiceTilNotAbleOrDontWantTo(DuelAction _DuelAction)
        {
            List<CardData> allAttachments = CanReRollDice();
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
                            DuelField.INSTANCE.GenericActionCallBack(da, "ResolveRerollEffect");
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
                EffectInformation.Add(new DuelAction { yesOrNo = false });
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
        internal void ResolveOnRecoveryEffect(CardData targetedCard)
        {
        }
    }

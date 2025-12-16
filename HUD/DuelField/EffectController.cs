using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using static DuelField;
using Unity.VisualScripting;

//BURRICE DO CARALHO, SEMPRE PEGAR O ULTIMO DA E ATUALIZAR AO PASSAR PELAS SELECOES, N FICAR MONTANDO MANUALMENTE ESSA PORRA Q DPS NINGUEM LEMBRA

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

    public EffectContext CurrentContext;

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
        CurrentContext = new();
    }
    public IEnumerator OshiSkill(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        switch (_DuelActionFirstAction.usedCard.cardNumber)
        {
            case "hBP01-006":
                menuActions.Add(() =>
                {
                    List<Card> canSelect = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, onlyVisible: true, GetOnlyHolomem: true);

                    if (canSelect.Count == 0 || canSelect.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(canSelect);
                });
                menuActions.Add(() =>
                {
                    DuelAction toReturn = CurrentContext.Build(CardList: true);
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnOshiEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-001":
                menuActions.Add(() =>
                {
                    return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(zonesThatPlayerCanSelect: new Lib.GameZone[] { Lib.GameZone.Stage });
                });
                menuActions.Add(() =>
                {
                    var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5, Lib.GameZone.Collaboration };
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), target: Player.Player, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                });
                menuActions.Add(() =>
                {
                    DuelAction toReturn = CurrentContext.Build(CardList: true, Target: true, Cost: true);
                    DuelField.INSTANCE.GenericActionCallBack(toReturn, "ResolveOnOshiEffect");
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
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnOshiEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-002":
                // 13/12 refactor, p q adicionamos uma lista de numeros e retornamos um yedorno ? ao invez de perguntar direito sim ou n ?????? parece errado
                menuActions.Add(() =>
                {
                    return _DuelField_ShowANumberList.SetupSelectableNumbers(1, 6);
                });
                menuActions.Add(() =>
                {
                    DuelAction toReturn = CurrentContext.Build(CardList: true, Target: true, Cost: true, YesOrNo: true);
                    DuelField.INSTANCE.GenericActionCallBack(toReturn, "ResolveOnOshiSPEffect");
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
        CurrentContext.Register(_DuelActionFirstAction);

        switch (_DuelActionFirstAction.usedCard.cardNumber)
        {
            case "hSD01-002":
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), target: Player.Player);
                });
                menuActions.Add(() =>
                {
                    var canSelect = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, cardType: new() { "エール" });

                    DuelAction toReturn = CurrentContext.Build(Target: true);
                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(toReturn, canSelect);
                });
                menuActions.Add(() =>
                {
                    DuelAction toReturn = CurrentContext.Build(Target: true, CardList: true);
                    DuelField.INSTANCE.GenericActionCallBack(toReturn, "ResolveOnOshiSPEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-001":
                //select the card to return to back
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), target: Player.Oponnent, zonesThatPlayerCanSelect: DuelField.DEFAULTBACKSTAGE);
                });
                //send both cost and targer(color) to the server
                menuActions.Add(() =>
                {
                    DuelAction toReturn = CurrentContext.Build(Target: true);
                    DuelField.INSTANCE.GenericActionCallBack(toReturn, "ResolveOnOshiSPEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-001":
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), target: Player.Oponnent, zonesThatPlayerCanSelect: new Lib.GameZone[] { Lib.GameZone.Stage, Lib.GameZone.Collaboration });
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnOshiSPEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hYS01-003":
                menuActions.Add(() =>
                {
                    List<Card> canSelect = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, onlyVisible: true, GetOnlyHolomem: true);
                    if (canSelect.Count == 0 || canSelect.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(canSelect);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnOshiSPEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hYS01-004":
            case "hYS01-002":
            case "hBP01-006":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnOshiSPEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-003":
                menuActions.Add(() =>
                {
                    DuelAction duelAction = DuelField.INSTANCE.CUR_DA;
                    if (duelAction.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(duelAction.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build());
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true, Target: true), "ResolveOnOshiSPEffect");
                    return WaitForServerResponse();
                });
                break;
        }
        StartCoroutine(StartMenuSequenceCoroutine());

        yield return true;
    }
    public void ResolveOnCollabEffect(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        switch (_DuelActionFirstAction.usedCard.cardNumber)
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
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
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
                    bool WillActivate = CurrentContext.Build().yesOrNo;

                    if (!DuelField.INSTANCE.GetZone(Lib.GameZone.Favourite, Player.Player).GetComponentInChildren<Card>().cardName.Equals("星街すいせい") || !WillActivate || DuelField.INSTANCE.GetZone(CurrentContext.Build().usedCard.curZone, Player.Player).GetComponentInChildren<Card>().attachedEnergy.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-071":
                menuActions.Add(() =>
                {
                    var canSelect = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, cardName: new() { "座員" });
                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), canSelect, MaximumCanPick: 1);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-100":
                menuActions.Add(() =>
                {
                    var canSelect = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, cardType: new() { "エール" });
                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), canSelect, MaximumCanPick: 3);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-012":

                menuActions.Add(() =>
                {
                    return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                });
                menuActions.Add(() =>
                {
                    bool WillActivate = CurrentContext.Build(YesOrNo: true).yesOrNo;
                    if (!WillActivate)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, Player.Player).gameObject.transform.childCount - 1 < 1)
                    {
                        menuActions.Clear();
                        return dummy();
                    }

                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(YesOrNo: true, Target: true, CardList: true), Player.Player, new Lib.GameZone[] { Lib.GameZone.Stage });
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(YesOrNo: true, Target: true, CardList: true), "ResolveOnCollabEffect");
                    return dummy();
                });
                break;
            case "hSD01-007":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });

                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });

                menuActions.Add(() =>
                {
                    isSelectionCompleted = false;
                    List<Card> secondList = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Hand }, player: Player.Player, onlyVisible: true);
                    if (secondList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(secondList);

                });

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-099":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(DiceRoll: true);
                    int diceRoll = da.diceRoll.Last();
                    if (!IsOddNumber(diceRoll) && CardLib.CountPlayerActiveHolomem(onlyBackstage: true, Player.Oponnent) == 0)
                    {
                        menuActions.Clear();
                        return dummy();
                    }
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(da, Player.Oponnent, DuelField.DEFAULTBACKSTAGE);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-096":
                int diceRoll = 0;
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(DiceRoll: true);
                    diceRoll = da.diceRoll.Last();

                    if (!IsOddNumber(diceRoll) || DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return dummy();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(DiceRoll: true, CardList:true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-033":
                diceRoll = 0;
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });

                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true);
                    diceRoll = da.diceRoll.Last();

                    if (diceRoll == 1 || diceRoll == 3 || diceRoll == 5)
                    {
                        menuActions.Clear();
                        return dummy();
                    }
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(da, target: Player.Player);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, Target: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-036":
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), target: Player.Player);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-009":
                diceRoll = 0;
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });

                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });

                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true);
                    diceRoll = da.diceRoll.Last();

                    if (diceRoll > 4)
                    {
                        menuActions.Clear();
                        return dummy();
                    }

                    DuelField.INSTANCE.GenericActionCallBack(da, "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    DuelAction duelAction = DuelField.INSTANCE.CUR_DA;
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, Player.Player, new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 });
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, Target:true), "ResolveOnCollabEffect");

                    return WaitForServerResponse();

                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(DiceRoll: true);
                    diceRoll = da.diceRoll.Last();
                    if (diceRoll < 2)
                    {
                        return _DuelField_YesOrNoMenu.ShowYesOrNoMenu($"Retreat ?");
                    }
                    else
                    {
                        CurrentContext.Register(new DuelAction { yesOrNo = false });
                    }
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, Target: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();

                });
                break;
            case "hBP01-098":
                menuActions.Add(() =>
                {
                    var selectable = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, cardType: new() { "エール" });

                    if (selectable.Count < 1)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(selectable);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-020":
                menuActions.Add(() =>
                {
                    if (!DuelField.INSTANCE.GetZone(Lib.GameZone.Stage, Player.Player).GetComponentInChildren<Card>().cardTag.Contains("#ID"))
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true);
                    if (da.diceRoll.Last() < 3 || DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, Player.Player).GetComponentsInChildren<Card>().Count() == 0)
                    {
                        menuActions.Clear();
                        return dummy();
                    }
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(da);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, Target: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-039":
                menuActions.Add(() =>
                {
                    if (!DuelField.INSTANCE.GetZone(Lib.GameZone.Favourite, Player.Player).GetComponentInChildren<Card>().cardName.Equals("兎田ぺこら"))
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true);
                    if (IsOddNumber(da.diceRoll.Last()) || DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, Player.Player).GetComponentsInChildren<Card>().Count() == 0)
                    {
                        menuActions.Clear();
                        return dummy();
                    }
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(da);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, Target: true), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-101":
            case "hSD01-019":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnCollabEffect");
                    return WaitForServerResponse();
                });
                break;
        }
        StartCoroutine(StartMenuSequenceCoroutine());
    }
    public void ResolveSuportEffect(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        switch (_DuelActionFirstAction.usedCard.cardNumber)
        {
            case "hBP01-103":
                menuActions.Add(() =>
                {
                    return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost:true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-102":
                if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                    break;

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var serverList = Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList);
                    var canSelect = CardLib.GetAndFilterCards(CardList: serverList, ContainTags: new() { "#歌" });
                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), serverList, canSelect, true, 1);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-016":
            case "hSD01-017":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return dummy();
                });
                break;
            case "hSD01-018":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var serverList = Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList);
                    var canSelect = CardLib.GetAndFilterCards(CardList: serverList, GetLimitedCards: true);

                    if (serverList.Count == 0 || canSelect.Count == 0)
                    {
                        DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                        menuActions.Clear();
                    }
                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), serverList, canSelect, true, 1);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-019":
                menuActions.Add(() =>
                {
                    return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-020":

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true);
                    if (da.diceRoll.Last() < 3 || DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return dummy();
                    }

                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, CardList: true));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, CardList: true, Target: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-021":

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var serverList = Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList);
                    List<string> cardname = new() { "ときのそら", "AZKi", "SorAZ" };
                    var canSelect = CardLib.GetAndFilterCards(CardList: serverList, cardName: cardname);

                    if (serverList.Count == 0 || canSelect.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), serverList, canSelect, true, -1);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-104":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-105":
                menuActions.Add(() =>
                {
                    return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems();
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(Cost: true));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, Target: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(Cost: true, CardList: true));
                });
                menuActions.Add(() =>
                {
                    //The TARGET SHOLD BE REPLACED HERE< IF NOT IS A BUG
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Cost: true, Target: true, CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-106":
                menuActions.Add(() =>
                {
                    var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-108":
                menuActions.Add(() =>
                {
                    var zonesThatPlayerCanSelect = new Lib.GameZone[] { Lib.GameZone.BackStage1, Lib.GameZone.BackStage2, Lib.GameZone.BackStage3, Lib.GameZone.BackStage4, Lib.GameZone.BackStage5 };
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), target: Player.Oponnent, zonesThatPlayerCanSelect: zonesThatPlayerCanSelect);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-109":

                if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                    break;

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var serverList = Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList);
                    List<string> cardname = new() { "兎田ぺこら", "ムーナ・ホシノヴァ" };
                    var canSelect = CardLib.GetAndFilterCards(CardList: serverList, cardName: cardname);

                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), serverList, canSelect, true, 1);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-111":
                if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                    break;

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var serverList = Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList);
                    var canSelect = CardLib.GetAndFilterCards(CardList: serverList, ContainTags: new() { "#ID３期生" });
                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), serverList, canSelect, true, 1);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-113":
                if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                    break;

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var serverList = Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList);
                    var canSelect = CardLib.GetAndFilterCards(CardList: serverList, ContainTags: new() { "#Promise" });

                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), serverList, canSelect, true, 1);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-107":
                if (DuelField.INSTANCE.cardHolderPlayer.childCount > 6)
                    break;

                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var serverList = Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList);
                    var cardNumber = new List<string> { "hY04-001", "hY02-001", "hY03-001", "hY01-001" };
                    var canSelect = CardLib.GetAndFilterCards(CardList: serverList, cardNumber: cardNumber);

                    return _DuelField_ShowListPickThenReorder.SetupSelectableItems(CurrentContext.Build(), serverList, canSelect, doubleselect: false, 3);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-112":
                string diceRoll = "-1";
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true);
                    return (da.diceRoll.Last() > 3) ? _DuelField_TargetForEffectMenu.SetupSelectableItems(da, target: Player.Oponnent, zonesThatPlayerCanSelect: DuelField.DEFAULTBACKSTAGE) : dummy();
                });

                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true, Target: true);
                    if (da.diceRoll.Last() < 4)
                        return dummy();

                    DuelField.INSTANCE.GenericActionCallBack(da, "ResolveOnSupportEffect");
                    return WaitForServerResponse();
                });

                break;
        }
        StartCoroutine(StartMenuSequenceCoroutine());

    }
    public void ResolveOnArtEffect(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        switch (_DuelActionFirstAction.usedCard.cardNumber + "-" + CurrentContext.Build().selectedSkill)
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
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-042-きｔらあああ":
            case "hBP01-038-こんぺこー！":
            case "hSD01-011-デスティニーソング":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-011-SorAZ グラビティ":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var list = CardLib.GetAndFilterCards(gameZones: new[] { Lib.GameZone.Stage }, player: Player.Player, cardName: new() { "ときのそら", "SorAZ" });
                    if (list.Count == 0)
                    {
                        menuActions.Clear();
                        return dummy();
                    }

                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), Player.Player, null);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-072-WAZZUP!!":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(DiceRoll: true, YesOrNo: true);
                    int diceRoll = da.diceRoll.Last();

                    var thisCard = CardLib.GetAndFilterCards(gameZones: new[] { da.usedCard.curZone }, player: Player.Player, color: new() { "赤" }, cardType: new() { "エール" });

                    if (IsOddNumber(diceRoll) || thisCard.Count > 0)
                    {
                        menuActions.Clear();
                        return dummy();
                    }

                    DuelField.INSTANCE.GenericActionCallBack(da, "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hSD01-013-越えたい未来":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnArtEffect");
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
                    var WillActivate = CurrentContext.Build(YesOrNo: true).yesOrNo;
                    List<Card> canSelect = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Hand }, player: Player.Player, onlyVisible: true);

                    if (!WillActivate || canSelect.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(canSelect);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(YesOrNo: true, CardList: true), "ResolveOnArtEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-020-みんな一緒に":
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, Player.Player).gameObject.transform.childCount - 1 < 1)
                    {
                        menuActions.Clear();
                        return dummy();
                    }
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), Player.Player, CardLib.ZonesForList(CardLib.GetAndFilterCards(player: Player.Player, ContainTags: new() { "秘密結社holoX" })));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true, Target: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
            default:
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "resolveArt");
                    return dummy();
                });
                break;
        }
        StartCoroutine(StartMenuSequenceCoroutine());
    }
    internal void ResolveOnDamageResolveEffect(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        bool WillActivate = false;

        if (!CanActivateDamageStepEffect() || !WillActivate)
        {

            DuelAction _response = new()
            {
                yesOrNo = false
            };

            DuelField.INSTANCE.GenericActionCallBack(_response, "AskServerToResolveDamageToHolomem");
            return;
        }

        switch (_DuelActionFirstAction.usedCard.cardNumber)
        {
            case "hSD01-011":
                break;
        }
        StartCoroutine(StartMenuSequenceCoroutine());
    }
    internal void ResolveOnAttachEffect(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        switch (_DuelActionFirstAction.usedCard.cardNumber)
        {
            case "hBP01-125":
                menuActions.Add(() =>
                {
                    return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                });
                menuActions.Add(() =>
                {
                    var WillActivate = CurrentContext.Build(YesOrNo: true).yesOrNo;
                    var canSelect = CardLib.GetAndFilterCards(gameZones: new[] { Lib.GameZone.Hand }, player: Player.Player, onlyVisible: true);

                    if (!WillActivate || canSelect.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(canSelect);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(YesOrNo: true, CardList: true), "ResolveOnAttachEffect");
                    return WaitForServerResponse();
                });
                break;
        }

        StartCoroutine(StartMenuSequenceCoroutine());
    }
    internal void ResolveOnBloomEffect(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        switch (_DuelActionFirstAction.usedCard.cardNumber)
        {
            case "hBP01-074":
                menuActions.Add(() =>
                {
                    Card AmIDebut = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { CurrentContext.Build().usedCard.curZone }, player: Player.Player, onlyVisible: true, bloomLevel: new() { "Debut" }).First();
                    if (AmIDebut != null)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    var selectable = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, onlyVisible: true, cardType: new() { "Debut", "1st" });
                    if (selectable.Count < 1)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(selectable);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-070":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-043":
                menuActions.Add(() =>
                {
                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-014":
            case "hBP01-013":
            case "hBP01-030":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-060":
                menuActions.Add(() =>
                {
                    List<Card> canSelect = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Hand }, player: Player.Player, onlyVisible: true);
                    if (canSelect.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(canSelect);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnBloomEffect");
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
                    var WillActivate = CurrentContext.Build(YesOrNo: true).yesOrNo;

                    if (!WillActivate || DuelField.INSTANCE.cardHolderPlayer.childCount > 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(YesOrNo: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    var da = CurrentContext.Build(DiceRoll: true, YesOrNo: true);
                    int diceRoll = da.diceRoll.Last();
                    if (diceRoll > 3)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    RollDiceTilNotAbleOrDontWantTo();
                    return dummy();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(DiceRoll: true, YesOrNo: true, CardList: true), target: Player.Player);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(DiceRoll: true, YesOrNo: true, CardList: true, Target: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-090":
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, Player.Player).gameObject.transform.childCount - 1 < 1)
                    {
                        menuActions.Clear();
                        return dummy();
                    }

                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    return _DuelField_YesOrNoMenu.ShowYesOrNoMenu();
                });

                menuActions.Add(() =>
                {
                    var WillActivate = CurrentContext.Build(YesOrNo: true).yesOrNo;

                    if (!WillActivate || DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(YesOrNo: true), Player.Player);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(YesOrNo: true, Target: true, CardList: true), "ResolveOnBloomEffect");
                    return dummy();
                });
                break;
            case "hBP01-037":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    DuelAction duelAction = DuelField.INSTANCE.CUR_DA;
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, Player.Player);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnBloomEffect");

                    return WaitForServerResponse();

                });
                break;
            case "hBP01-081":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    Lib.GameZone[] targetZones = CardLib.ZonesForList(CardLib.GetAndFilterCards(player: Player.Player, ContainTags: new() { "青" }));

                    if (targetZones.Length < 1)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    DuelAction duelAction = DuelField.INSTANCE.CUR_DA;
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, Player.Player, targetZones);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnBloomEffect");

                    return WaitForServerResponse();

                });
                break;
            case "hBP01-054":
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    Lib.GameZone[] targetZones = CardLib.ZonesForList(CardLib.GetAndFilterCards(player: Player.Player, ContainTags: new() { "#ID" }));
                    Lib.GameZone[] removeZones = CardLib.ZonesForList(CardLib.GetAndFilterCards(player: Player.Player, ContainTags: new() { "アイラニ・イオフィフティーン" }));
                    Lib.GameZone[] filteredZones = targetZones.Except(removeZones).ToArray();

                    if (targetZones.Length < 1)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }

                    DuelAction duelAction = DuelField.INSTANCE.CUR_DA;
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(duelAction, Player.Player, filteredZones);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();

                });
                break;
            case "hBP01-035":
                menuActions.Add(() =>
                {
                    List<Card> PlayerArquive = CardLib.GetAndFilterCards(gameZones: new Lib.GameZone[] { Lib.GameZone.Arquive }, player: Player.Player, onlyVisible: true);
                    List<Card> Selectable = CardLib.GetAndFilterCards(CardList: PlayerArquive, GetAllSuportTypes: true);

                    if (Selectable.Count < 1)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(PlayerArquive, Selectable);
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(CardList: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
            case "hBP01-094":
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.GetZone(Lib.GameZone.CardCheer, Player.Player).gameObject.transform.childCount - 1 < 1)
                    {
                        menuActions.Clear();
                        return dummy();
                    }
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                menuActions.Add(() =>
                {
                    if (DuelField.INSTANCE.CUR_DA.cardList.Count == 0)
                    {
                        menuActions.Clear();
                        return EmptyCoroutine();
                    }
                    return _DuelField_ShowAlistPickOne.SetupSelectableItems(Lib.ConvertToCard(DuelField.INSTANCE.CUR_DA.cardList));
                });
                menuActions.Add(() =>
                {
                    return _DuelField_TargetForEffectMenu.SetupSelectableItems(CurrentContext.Build(), Player.Player, CardLib.ZonesForList(CardLib.GetAndFilterCards(player: Player.Player, ContainTags: new() { "#Promise" })));
                });
                menuActions.Add(() =>
                {
                    DuelField.INSTANCE.GenericActionCallBack(CurrentContext.Build(Target: true, CardList: true), "ResolveOnBloomEffect");
                    return WaitForServerResponse();
                });
                break;
        }
    }
    internal void ResolveOnRecoveryEffect(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);
    }
    public IEnumerator RetreatArt(DuelAction _DuelActionFirstAction)
    {
        menuActions = new List<Func<IEnumerator>>();
        CurrentContext.Register(_DuelActionFirstAction);

        menuActions.Add(() =>
        {
            return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(new Lib.GameZone[] { CurrentContext.Build().usedCard.curZone });
        });
        menuActions.Add(() =>
        {
            DuelAction da = CurrentContext.Build(Cost: true);
            return _DuelField_TargetForEffectMenu.SetupSelectableItems(da, zonesThatPlayerCanSelect: DuelField.DEFAULTBACKSTAGE);
        });
        menuActions.Add(() =>
        {
            DuelAction da = CurrentContext.Build(Cost: true, Target: true);
            DuelField.INSTANCE.GenericActionCallBack(da, "Retreat");
            DuelField.INSTANCE.centerStageArtUsed = true;

            return dummy();
        });
        StartCoroutine(StartMenuSequenceCoroutine());

        yield return true;
    }
    private bool CanActivateDamageStepEffect()
    {
        return false;
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
                int diceRoll = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true).diceRoll.Last();
                return _DuelField_YesOrNoMenu.ShowYesOrNoMenu($"You rolled a {diceRoll}. Reroll?");
            });

            menuActions.Insert(1, () =>
            {
                bool choosed = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true).yesOrNo;
                // If player chooses "YES", resolve the reroll effect
                if (choosed)
                {
                    menuActions.Insert(0, () =>
                    {
                        return _DuelField_DetachEnergyOrEquipMenu.SetupSelectableItems(IsACheer: false);
                    });
                    menuActions.Insert(1, () =>
                    {
                        DuelAction da = CurrentContext.Build(Cost: true, DiceRoll: true, YesOrNo: true);
                        CurrentContext.RemoveAt(CurrentContext.History.Count - 1);
                        DuelField.INSTANCE.GenericActionCallBack(da, "ResolveRerollEffect");
                        return WaitForServerResponse();
                    });
                    menuActions.Insert(2, () =>
                    {
                        RollDiceTilNotAbleOrDontWantTo();
                        return dummy();
                    });
                }
                return dummy();
            });
        }
        else
        {
            CurrentContext.Register(new DuelAction { yesOrNo = false });
            return dummy();
        }
        return dummy();
    }
    private bool IsOddNumber(int x)
    {
        if ((x & 1) == 0)
            return false;
        else
            return true;
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
                yield return finishActions;
            }
            else
            {
                yield return StartCoroutine(nextMenu());
            }
        }
        if (menuActions.Count == 0)
            CurrentContext.Clear();
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
}

public class EffectContext
{
    public DuelAction RootAction;
    private readonly List<DuelAction> _history = new();
    public IReadOnlyList<DuelAction> History => _history;
    public void Register(DuelAction action)
    {
        if (action == null)
            return;

        if (RootAction == null)
            RootAction = action;

        _history.Add(action);
    }
    public void RemoveAt(int n)
    {
        _history.RemoveAt(n);
    }
    public void Clear()
    {
        RootAction = null;
        _history.Clear();
    }
    public T Last<T>() where T : DuelAction => _history.OfType<T>().LastOrDefault();
    public DuelAction LastWhere(Func<DuelAction, bool> predicate) => _history.LastOrDefault(predicate);
    public DuelAction LastCostAction() => _history.LastOrDefault(a => a.attachmentCost != null);
    public DuelAction LastTargetAction() => _history.LastOrDefault(a => a.targetCard != null);
    public DuelAction LastTargetZoneAction() => _history.LastOrDefault(a => a.targetZone != Lib.GameZone.na);
    public DuelAction LastSelectedList() => _history.LastOrDefault(a => a.cardList != null);
    public DuelAction Build(bool Cost = false,bool Target = false,bool TargetZone = false,bool CardList = false,bool YesOrNo = false, bool DiceRoll = false)
    {
        var costAction = Cost ? LastCostAction() : null;
        var targetAction = Target ? LastTargetAction() : null;
        var targetZoneAction = TargetZone ? LastTargetZoneAction() : null;
        var listAction = CardList ? LastSelectedList() : null;

        DuelAction lastBinaryAction = null;
        if ((YesOrNo || DiceRoll) && _history.Count > 0)
            lastBinaryAction = _history.Last();

        return new DuelAction
        {
            usedCard = RootAction?.usedCard,
            attachmentCost = costAction?.attachmentCost,
            targetCard = targetAction?.targetCard,
            targetZone = TargetZone ? targetZoneAction?.targetZone ?? Lib.GameZone.na : Lib.GameZone.na,
            cardList = listAction?.cardList,
            Order = listAction?.Order,
            yesOrNo = YesOrNo ? lastBinaryAction.yesOrNo : false,
            diceRoll = DiceRoll ? lastBinaryAction.diceRoll : null
        };
    }
}
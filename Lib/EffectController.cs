﻿using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using static DuelField;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Scripts.Lib
{
    class EffectController : MonoBehaviour
    {

        private DuelField_ShowListPickThenReorder _DuelField_SelectableCardMenu;
        private DuelField_TargetForEffectMenu _DuelField_TargetForEffectMenu;
        private DuelField_DetachEnergyMenu _DuelField_DetachEnergyMenu;
        private DuelField_ShowAlistPickOne _DuelField_ShowAlistPickOne;
        private DuelField_YesOrNoMenu _DuelField_YesOrNoMenu;

        private DuelField _DuelField; 
        private List<Func<IEnumerator>> menuActions;
        public List<object> EffectInformation;

        public DuelAction duelActionOutput;
        public DuelAction duelActionInput;

        public  bool isSelectionCompleted = false;
        public bool isServerResponseArrive = false;

        void Start()
        {
            _DuelField = FindAnyObjectByType<DuelField>();
            _DuelField_SelectableCardMenu = FindAnyObjectByType<DuelField_ShowListPickThenReorder>();
            _DuelField_TargetForEffectMenu = FindAnyObjectByType<DuelField_TargetForEffectMenu>();
            _DuelField_ShowAlistPickOne = FindAnyObjectByType<DuelField_ShowAlistPickOne>();
            _DuelField_DetachEnergyMenu = FindAnyObjectByType<DuelField_DetachEnergyMenu>();
            _DuelField_YesOrNoMenu = FindAnyObjectByType<DuelField_YesOrNoMenu>();
            EffectInformation = new List<object>();
        }

         public void ResolveOnCollabEffect(DuelAction _DuelActionR)
        {
            menuActions = new List<Func<IEnumerator>>();
            List<Card> holoPowerList;
            EffectInformation.Clear();

            switch (_DuelActionR.usedCard.cardNumber)
            {
                case "hSD01-012":
                    string WillActivate = "";
                    //select if active
                    menuActions.Add(() =>
                    {
                        isServerResponseArrive = false;
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
                        _DuelActionR.usedCard = new CardData() {cardNumber = cardnumber[0] };  
                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, new string[] {"Stage"});
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
                        return  _DuelField_ShowAlistPickOne.SetupSelectableItems(_DuelActionR, holoPowerList, holoPowerList);
                    });

                    menuActions.Add(() =>
                    {//fetch player hand
                        List<Card> secondList = GameObject
                            .Find("MatchField")
                            .transform.Find("PlayersHands/PlayerHand")
                            .GetComponentsInChildren<Card>()
                            .ToList();

                        //now we show the hand + the card the player selected so he can pick and send pack to the server
                        if (EffectInformation.Count > 0)
                        {
                            List<string> pickedFromHoloPower = (List<string>) EffectInformation[0];
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
                    List<string> serverReturn = JsonConvert.DeserializeObject<List<string>>(_DuelActionR.actionObject, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });
                    string diceRoll = serverReturn[0];

                    string cheerNumber = "";
                    if (serverReturn.Count > 1)
                        cheerNumber = serverReturn[1];

                    //if higher than 4, return since the effect dont active and also, the server don't send the energy
                    if (int.Parse(diceRoll) > 4)
                        return;

                    ShowCardEffect(cheerNumber);
                    //target the card
                    menuActions.Add(() =>
                    {

                        return _DuelField_TargetForEffectMenu.SetupSelectableItems(_DuelActionR, TargetPlayer.Player, new string[] {"BackStage1", "BackStage2", "BackStage3", "BackStage4", "BackStage5" });

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
                        isServerResponseArrive = false;
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
                        //aguarda posicao de retorno
                        duelActionInput = JsonConvert.DeserializeObject<DuelAction>(_DuelField._MatchConnection.DuelActionList.GetByIndex((_DuelField._MatchConnection.DuelActionList.Count() - 1)), new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, MissingMemberHandling = MissingMemberHandling.Ignore });

                        return dummy();
                    });

                    menuActions.Add(() =>
                    {
                        isServerResponseArrive = false;
                        //chama de novo para finalizar
                        List<string> returnList = (List<string>)EffectInformation[0];
                        duelActionOutput.actionObject = returnList[0];
                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnCollabEffect");
                        return dummy();
                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
            isSelectionCompleted = false;
        }
       public void ResolveSuportEffect()
        {
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

                    isServerResponseArrive = false;

//                    if (!_DuelField.GetZone("Stage", TargetPlayer.Player).GetComponentInChildren<Card>().name.Equals("ときのそら"))
//                        return;

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
                        duelActionOutput.actionType = "AskAttachTopCheerEnergyToBack";

                        _DuelField.GenericActionCallBack(duelActionOutput, "ResolveOnArtEffect");

                        return WaitForServerResponse();

                    });
                    break;
            }
            StartCoroutine(StartMenuSequenceCoroutine());
            isSelectionCompleted = false;
        }
        private void ShowCardEffect(string cheerNumber)
        {
            //throw new NotImplementedException();
        }
        public  IEnumerator StartMenuSequenceCoroutine(bool type = false)
        {
            while (menuActions.Count > 0)
            {
                Func<IEnumerator> nextMenu = menuActions[0];
                menuActions.RemoveAt(0);

                yield return StartCoroutine(nextMenu());
            }
            EffectInformation.Clear(); 
        }
        public IEnumerator dummy()
        {
            yield return new WaitUntil(() => true);
        }
        public IEnumerator WaitForServerResponse() {
            yield return new WaitUntil(() => isServerResponseArrive);
        }
        private static bool CheckForDetachableEnergy()
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
    }
}
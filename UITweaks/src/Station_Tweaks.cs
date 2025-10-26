using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UITweaks
{
    public class Station_Tweaks
    {
        // Right Click: Switch between Demand and Supply
        // Middle click: Switch between None and Demand

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStationStorage), nameof(UIStationStorage._OnOpen))]
        internal static void UIStationStorage_OnOpen(UIStationStorage __instance)
        {
            if (__instance.localSdButton.gameObject.GetComponent<ButtonClickHandler>() == null)
            {
                var handler = __instance.localSdButton.gameObject.AddComponent<ButtonClickHandler>();
                handler.OnRightClick = () => { __instance.poppedRemote = false; __instance.OnOptionButton1Click(); }; 
                handler.OnMiddleClick = () => { __instance.poppedRemote = false; __instance.OnOptionButton0Click(); };
            }
            if (__instance.remoteSdButton.gameObject.GetComponent<ButtonClickHandler>() == null)
            {
                var handler = __instance.remoteSdButton.gameObject.AddComponent<ButtonClickHandler>();
                handler.OnRightClick = () => { __instance.poppedRemote = true; __instance.OnOptionButton1Click(); };
                handler.OnMiddleClick = () => { __instance.poppedRemote = true; __instance.OnOptionButton0Click(); };
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIControlPanelStationStorage), nameof(UIControlPanelStationStorage._OnOpen))]
        internal static void UIControlPanelStationStorage_OnOpen(UIControlPanelStationStorage __instance)
        {
            if (__instance.localSdButton.gameObject.GetComponent<ButtonClickHandler>() == null)
            {
                var handler = __instance.localSdButton.gameObject.AddComponent<ButtonClickHandler>();
                handler.OnRightClick = () => { __instance.poppedRemote = false; __instance.OnOptionButton1Click(); };
                handler.OnMiddleClick = () => { __instance.poppedRemote = false; __instance.OnOptionButton0Click(); };
            }
            if (__instance.remoteSdButton.gameObject.GetComponent<ButtonClickHandler>() == null)
            {
                var handler = __instance.remoteSdButton.gameObject.AddComponent<ButtonClickHandler>();
                handler.OnRightClick = () => { __instance.poppedRemote = true; __instance.OnOptionButton1Click(); };
                handler.OnMiddleClick = () => { __instance.poppedRemote = true; __instance.OnOptionButton0Click(); };
            }
        }
    }

    public class ButtonClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public Action OnRightClick { get; set; }
        public Action OnMiddleClick { get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnRightClick?.Invoke();
            }
            else if (eventData.button == PointerEventData.InputButton.Middle)
            {
                OnMiddleClick?.Invoke();
            }
        }
    }
}

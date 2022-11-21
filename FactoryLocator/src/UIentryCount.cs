using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryLocator
{
    public class UIentryCount
    {
        static Text countText;
        static Dictionary<int, int> filterIds;

        public static void OnOpen(ESignalType signalType, Dictionary<int, int> filters)
        {
            filterIds = filters;

            if (countText == null)
                countText = Object.Instantiate(UIRoot.instance.uiGame.warningWindow.itemPrefab.countText);
            else
                countText.gameObject.SetActive(true);

            switch (signalType)
            {
                case ESignalType.Item:
                    countText.gameObject.transform.SetParent(UIRoot.instance.uiGame.itemPicker.iconImage.transform);
                    break;

                case ESignalType.Recipe:
                    countText.gameObject.transform.SetParent(UIRoot.instance.uiGame.recipePicker.iconImage.transform);
                    break;

                case ESignalType.Signal:
                    countText.gameObject.transform.SetParent(UIRoot.instance.uiGame.signalPicker.iconImage.transform);
                    break;
            }
            countText.gameObject.transform.localScale = Vector3.one;
            countText.text = "";
        }

        public static void OnClose()
        {
            if (countText != null)
            {
                countText.gameObject.SetActive(false);
            }
        }

        public static void OnDestory()
        {
            GameObject.DestroyImmediate(countText);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIItemPicker), nameof(UIItemPicker.TestMouseIndex))]
        internal static void TestMouseIndex(UIItemPicker __instance)
        {
            if (__instance.hoveredIndex >= 0)
            {
                if (countText != null && countText.isActiveAndEnabled)
                {
                    int id = __instance.protoArray[__instance.hoveredIndex]?.ID ?? 0;
                    SetValue(__instance.hoveredIndex, id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIRecipePicker), nameof(UIRecipePicker.TestMouseIndex))]
        internal static void TestMouseIndex(UIRecipePicker __instance)
        {
            if (__instance.hoveredIndex >= 0)
            {
                if (countText != null && countText.isActiveAndEnabled)
                {
                    int id = __instance.protoArray[__instance.hoveredIndex]?.ID ?? 0;
                    SetValue(__instance.hoveredIndex, id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISignalPicker), nameof(UISignalPicker.TestMouseIndex))]
        internal static void TestMouseIndex(UISignalPicker __instance)
        {
            if (__instance.hoveredIndex >= 0)
            {
                if (countText != null && countText.isActiveAndEnabled)
                {
                    int id = __instance.signalArray[__instance.hoveredIndex];
                    SetValue(__instance.hoveredIndex, id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIItemPicker), nameof(UIItemPicker.OnTypeButtonClick))]
        [HarmonyPatch(typeof(UIRecipePicker), nameof(UIRecipePicker.OnTypeButtonClick))]
        [HarmonyPatch(typeof(UISignalPicker), nameof(UISignalPicker.OnTypeButtonClick))]
        internal static void OnTypeButtonClick()
        {
            if (countText != null && countText.isActiveAndEnabled)
            {
                countText.text = "";
            }
        }

        private static void SetValue(int hoveredIndex, int protoId)
        {
            int col = hoveredIndex % 12;
            int row = hoveredIndex / 12;
            filterIds.TryGetValue(protoId, out int count);
            if (count < 10000)
                countText.text = count.ToString();
            else if (count < 10000 * 1000)
                countText.text = string.Format("{0:F1}K", count / 1000f);
            else
                countText.text = string.Format("{0:F1}M", count / 1000f / 1000f);
            countText.rectTransform.localPosition = new Vector2(col * 46 + 41, -row * 46 - 15); // (col * 46 + 46, -row * 46 - 45)
        }

    }
}

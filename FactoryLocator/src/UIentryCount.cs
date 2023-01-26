using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryLocator
{
    public class UIentryCount
    {
        public static bool EnableAll { get; set; } = true;

        static Text countText;
        static Dictionary<int, int> filterIds;
        static Text[] countArray;
        const int ARRAYLENGTH = 84;

        public static void OnOpen(ESignalType signalType, Dictionary<int, int> filters)
        {
            // This need to call before OnTypeButtonClick
            filterIds = filters;

            Init(ref countText, signalType, 0);
            if (EnableAll)
            {
                countText.gameObject.SetActive(false);
                if (countArray == null)
                    countArray = new Text[ARRAYLENGTH];
                for (int i = 0; i < ARRAYLENGTH; i++)
                    Init(ref countArray[i], signalType, i);
            }
            else
            {
                countText.gameObject.SetActive(true);
            }
        }

        public static void OnClose()
        {
            if (countText != null)
            {
                countText.gameObject.SetActive(false);
            }
            if (countArray != null)
            {
                for (int i = 0; i < ARRAYLENGTH; i++)
                    countArray[i].gameObject.SetActive(false);
            }
        }

        public static void OnDestory()
        {
            GameObject.DestroyImmediate(countText);
            if (countArray != null)
                for (int i = 0; i < ARRAYLENGTH; i++)
                    GameObject.DestroyImmediate(countArray[i]);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIItemPicker), nameof(UIItemPicker.TestMouseIndex))]
        static void TestMouseIndex(UIItemPicker __instance)
        {
            if (countText != null && countText.isActiveAndEnabled)
            {
                if (__instance.hoveredIndex >= 0)
                {
                    int id = __instance.protoArray[__instance.hoveredIndex]?.ID ?? 0;
                    SetPosition(countText, __instance.hoveredIndex);
                    SetNumber(countText, id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIRecipePicker), nameof(UIRecipePicker.TestMouseIndex))]
        static void TestMouseIndex(UIRecipePicker __instance)
        {
            if (countText != null && countText.isActiveAndEnabled)
            {
                if (__instance.hoveredIndex >= 0)
                {
                    int id = __instance.protoArray[__instance.hoveredIndex]?.ID ?? 0;
                    SetPosition(countText, __instance.hoveredIndex);
                    SetNumber(countText, id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISignalPicker), nameof(UISignalPicker.TestMouseIndex))]
        static void TestMouseIndex(UISignalPicker __instance)
        {
            if (countText != null && countText.isActiveAndEnabled)
            {
                if (__instance.hoveredIndex >= 0)
                {
                    int id = __instance.signalArray[__instance.hoveredIndex];
                    SetPosition(countText, __instance.hoveredIndex);
                    SetNumber(countText, id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIItemPicker), nameof(UIItemPicker.OnTypeButtonClick))]
        static void OnTypeButtonClick1(UIItemPicker __instance)
        {
            if (countText != null && countText.isActiveAndEnabled)
            {
                countText.text = "";
            }
            if (EnableAll && countArray != null)
            {
                for (int i = 0; i < ARRAYLENGTH; i++)
                {
                    int id = __instance.protoArray[i]?.ID ?? -1;
                    SetNumber(countArray[i], id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIRecipePicker), nameof(UIRecipePicker.OnTypeButtonClick))]
        static void OnTypeButtonClick2(UIRecipePicker __instance)
        {
            if (countText != null && countText.isActiveAndEnabled)
            {
                countText.text = "";
            }
            if (EnableAll && countArray != null)
            {
                for (int i = 0; i < ARRAYLENGTH; i++)
                {
                    int id = __instance.protoArray[i]?.ID ?? -1;
                    SetNumber(countArray[i], id);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UISignalPicker), nameof(UISignalPicker.OnTypeButtonClick))]
        static void OnTypeButtonClick3(UISignalPicker __instance)
        {
            if (countText != null && countText.isActiveAndEnabled)
            {
                countText.text = "";
            }
            if (EnableAll && countArray != null)
            {
                for (int i = 0; i < ARRAYLENGTH; i++)
                {
                    int id = __instance.signalArray[i];
                    SetNumber(countArray[i], id);
                }
            }
        }

        public static void Init(ref Text text, ESignalType signalType, int index)
        {
            if (text == null)
                text = Object.Instantiate(UIRoot.instance.uiGame.warningWindow.itemPrefab.countText);

            switch (signalType)
            {
                case ESignalType.Item:
                    text.gameObject.transform.SetParent(UIRoot.instance.uiGame.itemPicker.iconImage.transform);
                    break;

                case ESignalType.Recipe:
                    text.gameObject.transform.SetParent(UIRoot.instance.uiGame.recipePicker.iconImage.transform);
                    break;

                case ESignalType.Signal:
                    text.gameObject.transform.SetParent(UIRoot.instance.uiGame.signalPicker.iconImage.transform);
                    break;
            }
            text.gameObject.transform.localScale = Vector3.one;
            text.text = "";
            SetPosition(text, index);
        }

        private static void SetPosition(Text text, int hoveredIndex)
        {
            int col = hoveredIndex % 12;
            int row = hoveredIndex / 12;
            text.rectTransform.localPosition = new Vector2(col * 46 + 41, -row * 46 - 15); // (col * 46 + 46, -row * 46 - 45)
        }

        private static void SetNumber(Text text, int protoId)
        {
            text.gameObject.SetActive(filterIds.TryGetValue(protoId, out int count));
            if (count < 10000)
                text.text = count.ToString();
            else if (count < 10000 * 1000)
                text.text = string.Format("{0:F1}K", count / 1000f);
            else
                text.text = string.Format("{0:F1}M", count / 1000f / 1000f);
        }
    }
}

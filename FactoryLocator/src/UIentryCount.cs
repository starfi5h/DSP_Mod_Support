using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryLocator
{
    public class UIentryCount
    {
        public static int ItemCol { get; set; } = 14;
        public static int RecipeCol { get; set; } = 14;
        public static int SignalCol { get; set; } = 14;

        static Dictionary<int, int> filterIds;
        static Text[] countArray;
        const int ARRAYLENGTH = 112; //8*14

        public static void OnOpen(ESignalType signalType, Dictionary<int, int> filters)
        {
            // This need to call before OnTypeButtonClick
            filterIds = filters;

            if (countArray == null)
                countArray = new Text[ARRAYLENGTH];
            for (int i = 0; i < ARRAYLENGTH; i++)
                Init(ref countArray[i], signalType, i);
        }

        public static void OnClose()
        {
            if (countArray != null)
            {
                for (int i = 0; i < ARRAYLENGTH; i++)
                    countArray[i].gameObject.SetActive(false);
            }
        }

        public static void OnDestory()
        {
            if (countArray != null)
                for (int i = 0; i < ARRAYLENGTH; i++)
                    GameObject.DestroyImmediate(countArray[i]);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIItemPicker), nameof(UIItemPicker.OnTypeButtonClick))]
        static void OnTypeButtonClick1(UIItemPicker __instance)
        {
            if (countArray != null)
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
            if (countArray != null)
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
            if (countArray != null)
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
            SetPosition(text, index, signalType);
        }

        private static void SetPosition(Text text, int hoveredIndex, ESignalType signalType)
        {
            int maxCol = 14;
            switch (signalType)
            {
                case ESignalType.Item: maxCol = ItemCol; break;
                case ESignalType.Recipe: maxCol = RecipeCol; break;
                case ESignalType.Signal: maxCol = SignalCol; break;
            }
            int col = hoveredIndex % maxCol;
            int row = hoveredIndex / maxCol;
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

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryLocator
{
    public class UIentryCount
    {
        public static int ItemCol { get; set; } = 14;
        public static int RecipeCol { get; set; } = 14;
        public static int SignalCol { get; set; } = 14;
        public static bool Active { get; private set; }

        static Dictionary<int, int> filterIds; // protoId => count
        static Dictionary<int, Color> colorMap; // protoId => color
        static Text[] countArray;
        const int ARRAYLENGTH = 112; //8*14

        public static void OnOpen(ESignalType signalType, Dictionary<int, int> filters, Dictionary<int, Color> numberColors = null)
        {
            // This need to call before OnTypeButtonClick
            filterIds = filters;
            colorMap = numberColors;

            if (countArray == null)
                countArray = new Text[ARRAYLENGTH];
            for (int i = 0; i < ARRAYLENGTH; i++)
                Init(ref countArray[i], signalType, i);
            Active = true;
            UIItemPicker.showAll = true; // Show all item including not researched one
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIItemPicker), nameof(UIItemPicker._OnClose))]
        [HarmonyPatch(typeof(UIRecipePicker), nameof(UIRecipePicker._OnClose))]
        [HarmonyPatch(typeof(UISignalPicker), nameof(UISignalPicker._OnClose))]
        public static void OnClose()
        {
            if (Active && countArray != null)
            {
                // originalColor: 0.9906 0.5897 0.3691 0.7059
                var originalColor = UIRoot.instance.uiGame.warningWindow.itemPrefab.countText.color;
                for (int i = 0; i < ARRAYLENGTH; i++)
                {
                    countArray[i].gameObject.SetActive(false);
                    countArray[i].color = originalColor;
                }
            }
            Active = false;
            colorMap = null;
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
            if (Active && countArray != null)
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
            if (Active && countArray != null)
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
            if (Active && countArray != null)
            {
                for (int i = 0; i < ARRAYLENGTH; i++)
                {
                    int id = __instance.signalArray[i];
                    SetNumber(countArray[i], id);
                }
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UISignalPicker), nameof(UISignalPicker._OnUpdate))]
        public static IEnumerable<CodeInstruction> UISignalPicker_OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Remove base._Close() so it doesn't close when clicking on area outside of the picking window
                var codeMacher = new CodeMatcher(instructions).End()
                    .MatchBack(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "_Close")
                    )
                    .SetAndAdvance(OpCodes.Nop, null)
                    .SetAndAdvance(OpCodes.Nop, null);

                return codeMacher.InstructionEnumeration();
            }
            catch (System.Exception e)
            {
                Log.Warn("Transpiler UISignalPicker._OnUpdate error");
                Log.Warn(e);
                return instructions;
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
            // Set number string value
            if (filterIds.TryGetValue(protoId, out int count))
            {
                text.gameObject.SetActive(true);
                if (count < 10000)
                    text.text = count.ToString();
                else if (count < 10000000)
                    text.text = string.Format("{0:F1}K", count / 1000f);
                else
                    text.text = string.Format("{0:F1}M", count / 1000f / 1000f);
            }
            else
            {
                text.gameObject.SetActive(false);
            }
            // Set number color
            if (colorMap != null && colorMap.TryGetValue(protoId, out Color color)) text.color = color;
        }
    }
}

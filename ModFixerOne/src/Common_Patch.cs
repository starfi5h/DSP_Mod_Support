using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace ModFixerOne
{
    public static class Common_Patch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameData), nameof(GameData.Import))]
        public static void CheckAferImport()
        {
            if (GameMain.history.constructionDroneMovement > 4)
            {
                Plugin.Log.LogWarning($"Drone Task Points fix: {GameMain.history.constructionDroneMovement} => 4");
                GameMain.history.constructionDroneMovement = 4;
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Localization), nameof(Localization.NotifyLanguageChange))]
        public static void SwitchLanguage()
        {
            try
            {
                var field = AccessTools.Field(typeof(Localization), "lang");
                if (field != null)
                {
                    object enumValue = Enum.ToObject(field.FieldType, Localization.isZHCN ? 0 : 1);
                    field.SetValue(null, enumValue);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(UIOptionWindow), "_Init")]
        public static void CopyUIComponents(UIOptionWindow __instance)
        {
            var fi = AccessTools.Field(typeof(UIOptionWindow), "fullscreenComp");
            if (fi == null) return;
            UIToggle toggle = GameObject.Instantiate(__instance.vsyncComp, __instance.vsyncComp.transform.parent);
            toggle.transform.localPosition = new Vector3(-300, -300);
            fi.SetValue(__instance, toggle);
            Plugin.Log.LogDebug("Copy UIOptionWindow.vsyncComp to fullscreenComp");
        }

        public static IEnumerable<CodeInstruction> UIInventory_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace uiGame.inventory with uiGame.inventoryWindow
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "inventory"))
                    .Repeat(matcher => {
                        matcher.SetAndAdvance(OpCodes.Ldfld, AccessTools.Field(typeof(UIGame), "inventoryWindow"));
                        }
                    );
                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("UIInventory_Transpiler fail!");
#if DEBUG
                Plugin.Log.LogWarning(e);
#endif
                return instructions;
            }
        }
    }
}

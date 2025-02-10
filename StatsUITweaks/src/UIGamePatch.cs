using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StatsUITweaks
{
    public class UIGamePatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnUpdate))]
        public static IEnumerable<CodeInstruction> OnUpdate_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // 使物流總控面板在打開背包時(E)仍然保持顯示
            // Replace this.ShutControlPanelWindow() in `if (Input.GetKeyDown(KeyCode.E) && !VFInput.inputing && flag)`
            try
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "ShutControlPanelWindow")
                    )
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(UIGamePatch), nameof(ShutControlPanelWindow)));

                return codeMatcher.InstructionEnumeration();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("Transpiler UIGame._OnUpdate error!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }

        static void ShutControlPanelWindow(UIGame uIGame)
        {
            uIGame.TogglePlayerInventory();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame.On_Shift_P_Switch))]
        public static IEnumerable<CodeInstruction> On_Shift_P_Switch_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // 使功能面板在打開儀表板時仍然保持顯示
            // Remove this.ShutAllFunctionWindow()
            try
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "ShutAllFunctionWindow")
                    )
                    .SetAndAdvance(OpCodes.Pop, null);

                return codeMatcher.InstructionEnumeration();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("Transpiler UIGame.On_Shift_P_Switch error!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }
    }
}

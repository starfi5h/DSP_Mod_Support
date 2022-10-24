using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ModFixerOne
{
    public static class Common_Patch
    {
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

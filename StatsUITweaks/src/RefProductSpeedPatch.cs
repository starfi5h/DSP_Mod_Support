using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StatsUITweaks
{
    public class RefProductSpeedPatch
    {
        // 套用至每個呼叫FactoryProductionStat.AddRefProductSpeed的函式
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ProductionExtraInfoCalculator), nameof(ProductionExtraInfoCalculator.CalculateFactory))]
        [HarmonyPatch(typeof(UIReferenceSpeedTip), nameof(UIReferenceSpeedTip.AddEntryDataWithFactory))]
        public static IEnumerable<CodeInstruction> IncUsed_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // 無論機器是否運轉，都套用增產劑設定
            // Change: ptr.incUsed
            // To:     true
            try
            {
                CodeMatcher codeMatcher = new CodeMatcher(instructions)
                    .MatchForward( false,
                        new CodeMatch(OpCodes.Ldloc_S),
                        new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "incUsed")
                    )
                    .Repeat(matcher =>
                        matcher.SetAndAdvance(OpCodes.Nop, null)
                        .SetAndAdvance(OpCodes.Ldc_I4_1, null)
                    );

                return codeMatcher.InstructionEnumeration();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("RefProductSpeedPatch.IncUsed_Transpiler error!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }
    }
}

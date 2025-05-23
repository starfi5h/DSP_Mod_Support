using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StatsUITweaks
{
    public class RefProductSpeedPatch
    {
        static float minerLimit = 14400;

        public static void Init(Harmony harmony, bool forceInc, int limit)
        {
            if (!forceInc && limit < 0) return;

            // 套用至每個呼叫FactoryProductionStat.AddRefProductSpeed的函式
            var target1 = AccessTools.Method(typeof(ProductionExtraInfoCalculator), nameof(ProductionExtraInfoCalculator.CalculateFactory));
            var target2 = AccessTools.Method(typeof(UIReferenceSpeedTip), nameof(UIReferenceSpeedTip.AddEntryDataWithFactory));

            if (forceInc)
            {
                var transpiler = new HarmonyMethod(AccessTools.Method(typeof(RefProductSpeedPatch), nameof(IncUsed_Transpiler)));
                harmony.Patch(target1, null, null, transpiler);
                harmony.Patch(target2, null, null, transpiler);
            }
            if (limit >= 0)
            {
                minerLimit = limit;
                var transpiler = new HarmonyMethod(AccessTools.Method(typeof(RefProductSpeedPatch), nameof(MinerLimit_Transpiler)));
                harmony.Patch(target1, null, null, transpiler);
                harmony.Patch(target2, null, null, transpiler);
            }
        }

        static IEnumerable<CodeInstruction> IncUsed_Transpiler(IEnumerable<CodeInstruction> instructions)
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

        static IEnumerable<CodeInstruction> MinerLimit_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // 為抽水機和油井增加上限minerLimit


            try
            {
                var methodMin = AccessTools.Method(typeof(System.Math), nameof(System.Math.Min), new System.Type[] { typeof(float), typeof(float) });
                var minerSpeed = AccessTools.Field(typeof(MinerComponent), nameof(MinerComponent.speed));
                var oilSpeedMultiplier = AccessTools.Field(typeof(VeinData), nameof(VeinData.oilSpeedMultiplier));
                var minerLimit = AccessTools.Field(typeof(RefProductSpeedPatch), nameof(RefProductSpeedPatch.minerLimit));
                var codeMatcher = new CodeMatcher(instructions);

                // 處理case EMinerType.Oil (switch case被重新調整至前方)
                // Change: num16 = (float)(3600.0 / (double)ptr3.period * num13 * (double)ptr3.speed * (double)veinPool[ptr3.veins[0]].amount * (double)VeinData.oilSpeedMultiplier);
                // To:     num16 = Math.Min((float)(3600.0 / (double)ptr3.period * num13 * (double)ptr3.speed * (double)veinPool[ptr3.veins[0]].amount * (double)VeinData.oilSpeedMultiplier), minerLimit);
                codeMatcher.MatchForward(true,
                        new CodeMatch(OpCodes.Ldsfld, oilSpeedMultiplier),
                        new CodeMatch(OpCodes.Conv_R8),
                        new CodeMatch(OpCodes.Mul),
                        new CodeMatch(OpCodes.Conv_R4),
                        new CodeMatch(OpCodes.Stloc_S)
                    )
                    .Insert(
                        new CodeInstruction(OpCodes.Ldsfld, minerLimit),
                        new CodeInstruction(OpCodes.Call, methodMin)
                    );

                // 處理case EMinerType.Water
                // Change: num16 = (float)(3600.0 / (double)ptr3.period * num13 * (double)ptr3.speed);
                // To:     num16 = Math.Min((float)(3600.0 / (double)ptr3.period * num13 * (double)ptr3.speed), minerLimit);
                codeMatcher.MatchForward(true,
                        new CodeMatch(OpCodes.Ldfld, minerSpeed),
                        new CodeMatch(OpCodes.Conv_R8),
                        new CodeMatch(OpCodes.Mul),
                        new CodeMatch(OpCodes.Conv_R4),
                        new CodeMatch(OpCodes.Stloc_S)
                    )
                    .Insert(
                        new CodeInstruction(OpCodes.Ldsfld, minerLimit),
                        new CodeInstruction(OpCodes.Call, methodMin)
                    );

                return codeMatcher.InstructionEnumeration();
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogWarning("RefProductSpeedPatch.MinerLimit_Transpiler error!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }

    }
}

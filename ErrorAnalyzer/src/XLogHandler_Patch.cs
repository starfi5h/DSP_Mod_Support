using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using static System.Reflection.Emit.OpCodes;

namespace ErrorAnalyzer
{
    [HarmonyPatch(typeof(XLogHandler))]
    internal class XLogHandler_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("HandleLogThreaded")]
        static IEnumerable<CodeInstruction> HandleLogThreaded_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codeMatcher = new CodeMatcher(instructions);

                // Temporary fix bug in 0.10.33.27024
                // Change: logString = logString.Substring(0, logString.Length - num4);
                // To:     logString = logString.Substring(0, num4);
                codeMatcher.MatchForward(false, 
                    new CodeMatch(Ldarg_0),
                    new CodeMatch(Ldc_I4_0),
                    new CodeMatch(Ldarg_0),
                    new CodeMatch(i => i.opcode == Callvirt && ((MethodInfo)i.operand).Name == "get_Length"),
                    new CodeMatch(i => i.IsLdloc()),
                    new CodeMatch(Sub),
                    new CodeMatch(i => i.opcode == Callvirt && ((MethodInfo)i.operand).Name == "Substring")                    
                );
                if (codeMatcher.IsValid)
                {
                    codeMatcher.Advance(2)
                        .RemoveInstructions(2)
                        .Advance(1)
                        .RemoveInstruction();
                }
                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("HandleLogThreaded_Transpiler error!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace ErrorAnalyzer
{
    [HarmonyPatch(typeof(StackTrace))]
    internal class StackTrace_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("AddFrames")]
        static IEnumerable<CodeInstruction> AddFrames_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                var codeMatcher = new CodeMatcher(instructions);

                // Change: frame.GetInternalMethodName()
                // To:     AppendLineOrIL(frame.GetInternalMethodName(), frame)
                codeMatcher.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt 
                    && ((MethodInfo)i.operand).Name == "GetInternalMethodName"));
                if (codeMatcher.IsValid)
                {
                    var loadFrame = codeMatcher.InstructionAt(-1);
                    codeMatcher.Advance(1)
                        .Insert(
                            loadFrame,
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StackTrace_Patch), nameof(AppendLineOrIL)))
                        );
                }

                // Change: frame.GetFileLineNumber()
                // To:     frame.GetLineOrIL()
                codeMatcher.MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt 
                    && ((MethodInfo)i.operand).Name == "GetFileLineNumber"),
                    new CodeMatch(OpCodes.Box));
                if (codeMatcher.IsValid)
                {
                    // ILline mod load after this and will target GetFileLineNumber and Box
                    // So in here use pop to discard the original result and replace with our function
                    var loadFrame = codeMatcher.InstructionAt(-1);
                    codeMatcher.Advance(2)
                        .Insert(
                            new CodeInstruction(OpCodes.Pop),
                            loadFrame,
                            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StackTrace_Patch), nameof(GetLineOrIL)))
                        );
                }

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("AddFrames_Transpiler error!");
                Plugin.Log.LogWarning(e);
                return instructions;
            }
        }

        // Show IL offset for warpper dynamic-method
        static string AppendLineOrIL(string currentText, StackFrame stackFrame)
        {
            return currentText + " (at " + GetLineOrIL(stackFrame) + ")";
        }

        // First get the debug line number (C#)
        // If that is not available, return the IL offset (JIT might change it a bit)
        static string GetLineOrIL(StackFrame stackFrame)
        {
            int fileLineNumber = stackFrame.GetFileLineNumber();
            if (fileLineNumber == -1 || fileLineNumber == 0)
            {
                return "IL_" + stackFrame.GetILOffset().ToString("X4");
            }
            return fileLineNumber.ToString();
        }
    }
}

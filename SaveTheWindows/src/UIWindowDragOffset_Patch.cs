using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace SaveTheWindows
{
    public class UIWindowDragOffset_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UIWindowDrag), nameof(UIWindowDrag.Update))]
        public static IEnumerable<CodeInstruction> UIWindowDrag_Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // Replace Vector2 vector4 = UIRoot.WorldToScreenPoint(vector2) & Vector2 vector5 = UIRoot.WorldToScreenPoint(vector3);
                var codeMacher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "WorldToScreenPoint"))
                    .RemoveInstruction()
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UIWindowDragOffset_Patch), nameof(WorldToScreenPoint_Min)))
                    )
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "WorldToScreenPoint"))
                    .RemoveInstruction()
                    .InsertAndAdvance(
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(UIWindowDragOffset_Patch), nameof(WorldToScreenPoint_Max)))
                    );

                return codeMacher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Transpiler UIWindowDrag.Update error");
                Plugin.Log.LogError(e);
                return instructions;
            }
        }

        static Vector2 WorldToScreenPoint_Min(Vector3 worldPos, UIWindowDrag window)
        {
            return UIRoot.WorldToScreenPoint(worldPos) + new Vector2(window.refTrans.rect.width * 3 / 4, window.refTrans.rect.height - 60f);
        }

        static Vector2 WorldToScreenPoint_Max(Vector3 worldPos, UIWindowDrag window)
        {
            return UIRoot.WorldToScreenPoint(worldPos) - new Vector2(window.refTrans.rect.width * 3 / 4, window.refTrans.rect.height - 60f);
        }
    }
}

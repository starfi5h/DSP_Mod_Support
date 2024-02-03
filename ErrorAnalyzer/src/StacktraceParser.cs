using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ErrorAnalyzer
{
    // Some code are from WhatTheBreak mod, credit to Therzok
    // https://github.com/Therzok/dsp_modding/blob/main/src/WhatTheBreak/WhatTheBreakPlugin.cs

    class StacktraceParser
    {
        static bool IsChecked = false;
        static Dictionary<string, List<MethodBase>> patchMap = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFatalErrorTip), "_OnClose")]
        public static void OnClose_Postfix()
        {
            IsChecked = false;
            patchMap = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFatalErrorTip), "_OnOpen")]
        public static void OnOpen_Postfix(UIFatalErrorTip __instance)
        {
            if (!IsChecked)
            {
                if (patchMap == null)
                {
                    patchMap = new();
                    foreach (MethodBase patchedMethod in PatchProcessor.GetAllPatchedMethods())
                    {
                        string key = patchedMethod.DeclaringType.FullName;
                        if (!patchMap.TryGetValue(key, out List<MethodBase> methods))
                        {
                            methods = new List<MethodBase>();
                            patchMap[key] = methods;
                        }
                        methods.Add(patchedMethod);
                    }
                    Plugin.Log.LogDebug("Patched type: " + patchMap.Count);
                }

                List<MethodBase> modifiedMethods = new();
                ParseStackTraceLines(__instance.errorLogText.text,
                (typeName, methodName) =>
                {
                    //Log.Warn($"[{typeName}] [{methodName}]");
                    if (patchMap.TryGetValue(typeName, out var list))
                    {
                        foreach (var methodBase in list)
                        {
                            if (methodBase.Name == methodName)
                            {
                                modifiedMethods.Add(methodBase);
                            }
                        }
                    }

                });

                // Add related patches to errer text and extend window
                __instance.errorLogText.text += GetResultString(modifiedMethods);
                __instance.rectTrans.sizeDelta = new Vector2(__instance.rectTrans.sizeDelta.x, __instance.errorLogText.preferredHeight + 45f);
                __instance.errorLogText.rectTransform.sizeDelta = new Vector2(__instance.errorLogText.rectTransform.sizeDelta.x, __instance.errorLogText.preferredHeight + 2f);
                IsChecked = true;
            }
        }

        private static void ParseStackTraceLines(string source, Action<string, string> validate)
        {
            foreach (string original in source.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int end = original.IndexOf('(');
                if (end == -1)
                    continue;

                int typeEnd = original.LastIndexOf('.', end);
                if (typeEnd == -1 || (end - typeEnd - 2) <= 0)
                    continue;

                //Log.Debug(original + $" {typeEnd} {end}");

                string typeString;
                if (original.StartsWith("  at "))
                    typeString = original.Substring(5, typeEnd - 5);
                else
                    typeString = original.Substring(0, typeEnd);
                string methodString = original.Substring(typeEnd + 1, end - typeEnd - 2);

                validate(typeString, methodString);
            }
        }

        private static string GetResultString(List<MethodBase> modifiedMethods)
        {
            StringBuilder sb = new();
            foreach (var method in modifiedMethods)
            {
                var patchInfo = PatchProcessor.GetPatchInfo(method);
                PatchesToString(sb, method.Name, "Prefix", patchInfo.Prefixes);
                PatchesToString(sb, method.Name, "Postfix", patchInfo.Postfixes);
                PatchesToString(sb, method.Name, "Transpiler", patchInfo.Transpilers);
                Plugin.Log.LogDebug(method.Name + " owners:" + patchInfo.Owners.Count);
            }

            if (sb.Length > 0)
            {
                sb.Insert(0, "\n== Mod patches on the stack ==\n");
            }

            return sb.ToString();
        }

        private static void PatchesToString(StringBuilder sb, string name, string prefix, ReadOnlyCollection<Patch> patches)
        {
            foreach (var patch in patches)
            {
                if (IsWhitelist(name, patch))
                    continue;

                sb.Append(name)
                  .Append("(")
                  .Append(prefix)
                  .Append("): ");
                if (prefix != "Transpiler")
                    sb.AppendLine(patch.PatchMethod.FullDescription());
                else
                {
                    sb.AppendLine(patch.PatchMethod.FullDescription()
                        .Replace("System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>", "var")
                        .Replace("System.Reflection.Emit.ILGenerator", "var"));
                }
            }
        }

        private static bool IsWhitelist(string name, Patch patch)
        {
            /*
            if (name == "FixedUpdate")
            {
                string declaringType = patch.PatchMethod.DeclaringType.ToString();
                if (declaringType == "BulletTime.GameMain_Patch")
                    return true;
                if (declaringType == "NebulaPatcher.Patches.Transpiler.GameMain_Transpiler")
                    return true;
            }
            */
            return false;
        }

        /*
        static bool flag;
        [HarmonyPostfix]
        //[HarmonyPatch(typeof(UIEscMenu), "OnButton1Click")]
        [HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        public static void TestError()
        {
            if (flag)
                return;
            flag = true;
            int a = 0;
            int b = 1 / a;
        }
        */
        
    }
}

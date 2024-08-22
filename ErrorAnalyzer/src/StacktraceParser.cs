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

    public static class StacktraceParser
    {
        public static bool ShowAllPatches { get; set; } = false;

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
            if (IsChecked) return;
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
                Plugin.Log.LogDebug("Patched type count: " + patchMap.Count);
            }

            List<string> callStackTypeNames = new();
            List<string> callStackMethodNames = new();
            ParseStackTraceLines(__instance.errorLogText.text,
            (typeName, methodName) =>
            {
                callStackTypeNames.Add(typeName);
                callStackMethodNames.Add(methodName);
            });

            // Add related patches to errer text and extend window
            string resultString = GetResultString(callStackTypeNames, callStackMethodNames);
            Plugin.Log.LogInfo(resultString);
            __instance.errorLogText.text += resultString;
            __instance.rectTrans.sizeDelta = new Vector2(__instance.rectTrans.sizeDelta.x, __instance.errorLogText.preferredHeight + 45f);
            __instance.errorLogText.rectTransform.sizeDelta = new Vector2(__instance.errorLogText.rectTransform.sizeDelta.x, __instance.errorLogText.preferredHeight + 2f);
            IsChecked = true;
        }

        private static void ParseStackTraceLines(string source, Action<string, string> validate)
        {
            foreach (string line in source.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int end = line.IndexOf('(');
                if (end == -1) continue;

                int typeEnd = line.LastIndexOf('.', end);
                if (typeEnd == -1 || (end - typeEnd - 2) <= 0) continue;

                string typeString = line.StartsWith("  at ")
                    ? line.Substring(5, typeEnd - 5)
                    : line.Substring(0, typeEnd);
                string methodString = line.Substring(typeEnd + 1, end - typeEnd - 2);

                validate(typeString, methodString);
            }
        }

        private static string GetResultString(List<string> stackTypeNames, List<string> stackMethodNames)
        {
            if (stackTypeNames.Count == 0) return "";

            List<MethodBase> onStackModMethods = new();
            //List<MethodBase> firstClassModifiedMethods = null;
            for (int i = 0; i < stackTypeNames.Count; i++)
            {
                //Plugin.Log.LogDebug($"[{stackTypeNames[i]}] {stackMethodNames[i]}");

                if (i != 0 && (stackTypeNames[i] == "GameData" && stackMethodNames[i] == "GameTick"))
                {
                    // There are many mods hook on GameData.GameTick, so skip to avoid confusing
                    if (!ShowAllPatches) break;
                }
                if (patchMap.TryGetValue(stackTypeNames[i], out var list))
                {
                    foreach (var methodBase in list)
                    {
                        if (methodBase.Name == stackMethodNames[i])
                        {
                            onStackModMethods.Add(methodBase);
                        }
                    }
                    if (i == 0)
                    {
                        //firstClassModifiedMethods = list;
                        Plugin.Log.LogInfo($"{stackTypeNames[i]} modified methods count = " + list.Count);
                    }
                }
            }

            StringBuilder sb = new();
            foreach (var method in onStackModMethods)
            {
                var patchInfo = PatchProcessor.GetPatchInfo(method);
                PatchesToString(sb, method.Name, "Prefix", patchInfo.Prefixes);
                PatchesToString(sb, method.Name, "Postfix", patchInfo.Postfixes);
                PatchesToString(sb, method.Name, "Transpiler", patchInfo.Transpilers);
                Plugin.Log.LogDebug(method.Name + " owners:" + patchInfo.Owners.Count);
            }

            if (sb.Length > 0)
            {
                sb.Insert(0, "\n[== Mod patches on the stack ==]\n");
            }
            /* Abort 棄案
            if (firstClassModifiedMethods?.Count > 0)
            {
                HashSet<string> modFullTypeNames = new();
                sb.AppendLine($"\n[== Mod patches to {stackTypeNames[0]} ==]");
                foreach (var method in firstClassModifiedMethods)
                {
                    var patchInfo = PatchProcessor.GetPatchInfo(method);
                    GetNames(modFullTypeNames, patchInfo.Prefixes);
                    GetNames(modFullTypeNames, patchInfo.Postfixes);
                    GetNames(modFullTypeNames, patchInfo.Transpilers);
                }
                foreach (string name in modFullTypeNames)
                {
                    sb.AppendLine(name);
                }
            }
            */
            return sb.ToString();
        }

        private static void GetNames(HashSet<string> modNameSpaces, ReadOnlyCollection<Patch> patches)
        {
            foreach (var patch in patches)
            {
                string name = patch.PatchMethod.DeclaringType.FullName;
                if (!string.IsNullOrEmpty(name)) modNameSpaces.Add(name);
            }
        }

        private static void PatchesToString(StringBuilder sb, string name, string prefix, ReadOnlyCollection<Patch> patches)
        {
            // Format: static void ModMethod(); OriginalMethod (Prefix)
            foreach (var patch in patches)
            {
                if (IsWhitelist(name, patch))
                    continue;

                if (prefix != "Transpiler")
                    sb.Append(patch.PatchMethod.FullDescription());
                else
                {
                    sb.Append(patch.PatchMethod.FullDescription()
                        .Replace("System.Collections.Generic.IEnumerable<HarmonyLib.CodeInstruction>", "var")
                        .Replace("System.Reflection.Emit.ILGenerator", "var"));
                }
                sb.Replace("static ", ""); // the PatchMethod is always static function

                sb.Append("; ")
                  .Append(name)
                  .Append("(")
                  .Append(prefix)
                  .AppendLine(")");
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

#if DEBUG

        static bool flag;
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(UIEscMenu), "OnButton1Click")]
        //[HarmonyPatch(typeof(GameData), nameof(GameData.GameTick))]
        public static void TestError()
        {
            if (flag)
                return;
            flag = true;
            int a = 0;
            int b = 1 / a;
        }
#endif

    }
}

using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ErrorAnalyzer
{
    internal class UIErrorEnhancer
    {
        // Some code are from WhatTheBreak mod, credit to Therzok
        // https://github.com/Therzok/dsp_modding/blob/main/src/WhatTheBreak/WhatTheBreakPlugin.cs


        static bool isChecked = false;
        static BepInExPluginIdentifier bepInExPluginIdentifier;
        static HarmonyPatcherMapper harmonyPatcherMapper;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFatalErrorTip), "_OnClose")]
        public static void OnClose_Postfix()
        {
            isChecked = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIFatalErrorTip), "_OnOpen")]
        public static void OnOpen_Postfix(UIFatalErrorTip __instance)
        {
            if (isChecked) return;

            // Process the stack trace text and generate analysis result string
            string errorLogText = __instance.errorLogText.text;
            string resultString = ParseAndAnalysis(errorLogText, Plugin.ShowFullStack.Value);            
            errorLogText = StackParser.CleanStacktrace(errorLogText);
            Plugin.Log.LogInfo(resultString);

            // Add related patches to errer text and extend window
            __instance.errorLogText.text = errorLogText + resultString;
            __instance.rectTrans.sizeDelta = new Vector2(__instance.rectTrans.sizeDelta.x, __instance.errorLogText.preferredHeight + 45f);
            __instance.errorLogText.rectTransform.sizeDelta = new Vector2(__instance.errorLogText.rectTransform.sizeDelta.x, __instance.errorLogText.preferredHeight + 2f);
            isChecked = true;
        }

        public static string ParseAndAnalysis(string errorLog, bool showAllPatches = false)
        {
            if (harmonyPatcherMapper == null) harmonyPatcherMapper = new();
            if (bepInExPluginIdentifier == null) bepInExPluginIdentifier = new();

            string resultString = "";
            List<(string typeName, string methodName)> stackframes = StackParser.ParseStackTraceLines(errorLog);
            if (stackframes.Count == 0) return resultString;

            string firstTypeName;
            string patchesDescription = "";
            var patchesAssemblies = new HashSet<Assembly>();
            var toTypeAssemblies = new HashSet<Assembly>();

            // Process the first function on the stack trace
            {
                // Check if the first function is the mod function
                firstTypeName = stackframes[0].typeName;
                var asm = bepInExPluginIdentifier.GetAssembly(stackframes[0].typeName);
                if (asm != null)
                {
                    patchesAssemblies.Add(asm);
                }
                // Check if there is any in-line harmony patch to the first function
                var methodBase = harmonyPatcherMapper.GetModMethod(stackframes[0].typeName, stackframes[0].methodName);
                if (methodBase != null)
                {
                    patchesAssemblies.UnionWith(harmonyPatcherMapper.GetModAssembliesFromMethod(methodBase));
                    patchesDescription += harmonyPatcherMapper.GetPatchesDescription(methodBase);
                }
                // If above two conidtion fail, check if there are other mods patch to the same type (e.g. StationComponent)
                if (patchesAssemblies.Count == 0)
                {
                    toTypeAssemblies = harmonyPatcherMapper.GetModAssembliesFromType(stackframes[0].typeName);
                }
            }

            // Process the rest of the functions on stack trace
            {
                bool shouldSkipPatches = false;
                for (int i = 1; i < stackframes.Count; i++)
                {
                    string typeName = stackframes[i].typeName;
                    string methodName = stackframes[i].methodName;

                    // Check if the current function is the mod function
                    var asm = bepInExPluginIdentifier.GetAssembly(typeName);
                    if (asm != null)
                    {
                        patchesAssemblies.Add(asm);
                    }

                    if (!showAllPatches)
                    {
                        if (shouldSkipPatches) continue;

                        // There are many mods hook on VFPreload.InvokeOnLoadWorkEnded
                        if (typeName == "VFPreload") shouldSkipPatches = true;

                        // There are many mods hook on GameMain.Begin and GameMain.End
                        if (typeName == "GameMain") shouldSkipPatches = true;

                        // Stop when reaching common update loop entry
                        if (methodName == "GameTick" && (typeName == "GameData" || typeName == "PlanetFactory")) shouldSkipPatches = true;

                        // Stop when reaching common update loop entry (MMS)
                        if (methodName == "ProcessFrame" && typeName == "ThreadManager") shouldSkipPatches = true;

                        // Stop when reaching common update loop entry
                        if (methodName == "LogicFrame" && typeName == "GameLogic") shouldSkipPatches = true;

                        if (shouldSkipPatches) continue;
                    }

                    var methodBase = harmonyPatcherMapper.GetModMethod(typeName, methodName);
                    if (methodBase != null)
                    {
                        patchesAssemblies.UnionWith(harmonyPatcherMapper.GetModAssembliesFromMethod(methodBase));
                        patchesDescription += harmonyPatcherMapper.GetPatchesDescription(methodBase);
                        shouldSkipPatches = true; // Only getting the first in-line patches
                    }
                }
            }

            if (toTypeAssemblies.Count > 0)
            {
                resultString += $"\n[== Mods to {firstTypeName} ==]: ";
                foreach (var asm in toTypeAssemblies)
                {
                    resultString += $"[{asm.GetName().Name}]";
                }
            }
            if (patchesAssemblies.Count > 0)
            {
                resultString += "\n[== Mods on stack trace ==]: ";
                foreach (var asm in patchesAssemblies)
                {
                    resultString += $"[{asm.GetName().Name}]";
                }
                resultString += "\n" + patchesDescription;
            }

            UpdateExtraTitleString(patchesAssemblies);

            return resultString;
        }

        private static void UpdateExtraTitleString(HashSet<Assembly> relevantAssemblies)
        {
            UIFatalErrorTip_Patch.ExtraTitleString = relevantAssemblies.Count > 0 ? "possible candidates: " : "";
            foreach (var asm in relevantAssemblies)
            {
                var pluginInfos = bepInExPluginIdentifier.GetPluginInfoList(asm);
                foreach (var pluginInfo in pluginInfos)
                {
                    string pluginName = pluginInfo.Metadata.Name;
                    string pluginVersion = pluginInfo.Metadata.Version.ToString();
                    UIFatalErrorTip_Patch.ExtraTitleString += $"[{pluginName}{pluginVersion}]";
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil;

namespace ModFixerOne
{
    // Preloader learned from https://github.com/BepInEx/BepInEx.MultiFolderLoader

    public static class Preloader
    {
        public static ManualLogSource logSource = Logger.CreateLogSource("ModFixerOne Preloader");      
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll", "UnityEngine.CoreModule.dll" };
        public static IEnumerable<string> Guids; //Chainloader.PluginInfos will only added after successful load

        public static void Patch(AssemblyDefinition assembly)
        {
            try
            {
                if (assembly.Name.Name == "Assembly-CSharp")
                {
                    ModifyMainGame(assembly);
                }
                else if (assembly.Name.Name == "UnityEngine.CoreModule")
                {
                    TypeForwardInput(assembly);
                }
                else
                {
                    logSource.LogWarning("Unexpect assembly: " + assembly.Name.Name);
                }
            }
            catch (Exception ex)
            {
                logSource.LogError("Error when patching assembly " + assembly.Name.Name);
                logSource.LogError(ex);
            }
        }

        public static void Finish()
        {
            RemoveProcessFiler();
        }

        private static void TypeForwardInput(AssemblyDefinition assembly)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "DSPGAME_Data", "Managed", "UnityEngine.InputLegacyModule.dll")))
            {
                logSource.LogInfo("Skip type forward due to UnityEngine.InputLegacyModule doesn't exist");
                return;
            }

            var module = assembly.MainModule;
            // Create a reference to the target assembly (UnityEngine.InputLegacyModule)
            var targetAssemblyRef = new AssemblyNameReference("UnityEngine.InputLegacyModule", new Version(0, 0, 0, 0));
            // Add the assembly reference if it doesn't already exist
            if (!module.AssemblyReferences.Any(ar => ar.Name == targetAssemblyRef.Name))
            {
                module.AssemblyReferences.Add(targetAssemblyRef);
            }

            // Create the type forward
            var exportedType = new ExportedType("UnityEngine", "Input", module, targetAssemblyRef)
            {
                Attributes = TypeAttributes.Public | TypeAttributes.Forwarder,
                // The Forwarder flag is important - it marks this as a type forwarder
            };

            // Add the exported type to the module
            module.ExportedTypes.Add(exportedType);
            logSource.LogInfo("Type forward for UnityEngine.Input: UnityEngine.CoreModule => UnityEngine.InputLegacyModule");
        }

        private static void ModifyMainGame(AssemblyDefinition assembly)
        {
            try
            {
                // Add field: string StationComponent.name as dummy field
                assembly.MainModule.GetType("StationComponent").AddFied("name", assembly.MainModule.TypeSystem.String);
                logSource.LogDebug("string StationComponent.name");
                
                // Add method: void PlanetTransport.RefreshTraffic(int) to call PlanetTransport.RefreshStationTraffic(int)
                Injection.RefreshTraffic(assembly);
                logSource.LogDebug("void PlanetTransport.RefreshTraffic(int)");
                
                // Add enum Language { zhCN, enUS, frFR, Max } and Localization.language
                var enumType = Injection.Language(assembly);
                logSource.LogDebug("enum Language { zhCN, enUS, frFR, Max }");
                logSource.LogDebug("public static Language Localization.get_language()");

                // Add method StringTranslate.Translate(this string s, Language _ = null) to call Localization.Translate(this string s)
                Injection.StringTranslate(assembly, enumType);
                logSource.LogDebug("public static string StringTranslate.Translate(this string s)");
                
                // Add StringProto
                Injection.StringProto(assembly);
                logSource.LogDebug("public StringProto");

                // 0.10.33 update

                // TryAdd method: void GameData.GameTick(long time)
                if (Injection.GameDataGameTick(assembly))
                    logSource.LogDebug("void GameData.GameTick(long)");

                // TODO: Fix crash when patching the following
                // TryAdd field: UIToggle UIOptionWindow.fullscreenComp
                // if (Injection.UIOptionWindowfullscreenComp(assembly))
                //    logSource.LogDebug("UIToggle UIOptionWindow.fullscreenComp");
            }
            catch (Exception e)
            {
                logSource.LogError("Error when patching!");
                logSource.LogError(e);
            }
        }

#pragma warning disable IDE0001

        private static void RemoveProcessFiler()
        {
            try
            {
                var harmony = new Harmony("ModFixerOnePreloader");
                harmony.Patch(
                    AccessTools.Method(typeof(BepInEx.Bootstrap.TypeLoader), nameof(BepInEx.Bootstrap.TypeLoader.FindPluginTypes))
                        .MakeGenericMethod(typeof(PluginInfo)),
                    null,
                    new HarmonyMethod(AccessTools.Method(typeof(Preloader), nameof(PostFindPluginTypes))));

                harmony.Patch(
                    AccessTools.Method(typeof(BepInEx.Utility), nameof(Utility.TopologicalSort))
                        .MakeGenericMethod(typeof(string)),
                    null, new HarmonyMethod(AccessTools.Method(typeof(Preloader), nameof(PostTopologicalSort))));
            }
            catch (Exception e)
            {
                logSource.LogError("Remove process filter & change load order fail!");
                logSource.LogError(e);
            }
        }

        private static void PostFindPluginTypes(Dictionary<string, List<PluginInfo>> __result)
        {
            try
            {
                int count = 0;
                foreach (var infos in __result.Values)
                {
                    foreach (var info in infos)
                    {
                        if (info.Processes.Any())
                        {
                            //logSource.LogDebug($"Remove process filter from {info.Metadata.Name}");
                            AccessTools.Property(typeof(PluginInfo), nameof(PluginInfo.Processes)).SetValue(info, new List<BepInProcess>());
                            count++;
                        }
                    }
                }
                logSource.LogDebug($"Remove process filter from {count} plugins.");
            }
            catch (Exception e)
            {
                logSource.LogWarning("Can't remove process filter!");
                logSource.LogWarning(e);
            }
        }
    
        private static void PostTopologicalSort(ref IEnumerable<string> __result)
        {
            var newList = __result.Where(item => item != "starfi5h.plugin.ModFixerOne");
            if (newList.Count() != __result.Count())
            {
                __result = new string[] { "starfi5h.plugin.ModFixerOne" }.Concat(newList);
                logSource.LogDebug("Move ModFixerOne to first plugin to load.");
            }
            Guids = __result;
        }
    }
}

using System;
using System.Collections.Generic;
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
        public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

        public static void Patch(AssemblyDefinition assembly)
        {
            ModifyMainGame(assembly);
        }

        public static void Finish()
        {
            RemoveProcessFiler();
        }

        private static void ModifyMainGame(AssemblyDefinition assembly)
        {
            try
            {
                // Add field: UIStorageGrid UIGame.inventory as dummy field
                assembly.MainModule.GetType("UIGame").AddFied("inventory", assembly.MainModule.GetType("UIStorageGrid"));
                logSource.LogDebug("UIStorageGrid UIGame.inventory");

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

                // For XGP that is still in 0.10.28.21014, add the following new methods
                // Add method: UIStatisticsWindow.AddFactoryStatGroup
                // Add method: UIStatisticsWindow.ComputeDisplayProductEntries
                if (Injection.UIStatisticsWindow_Patch(assembly))
                {
                    logSource.LogDebug("UIStatisticsWindow.AddFactoryStatGroup");
                    logSource.LogDebug("UIStatisticsWindow.ComputeDisplayProductEntries");
                }
            }
            catch (Exception e)
            {
                logSource.LogError("Error when patching!");
                logSource.LogError(e);
            }
        }

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
            }
            catch (Exception e)
            {
                logSource.LogError("Remove process filter fail!");
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
    }
}
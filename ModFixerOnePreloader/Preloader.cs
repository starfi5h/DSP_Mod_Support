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
            AddMemeber(assembly);
        }

        public static void Finish()
        {
            RemoveProcessFiler();
        }

        private static void AddMemeber(AssemblyDefinition assembly)
        {
            try
            {
                var gameModule = assembly.MainModule;

                // Add field: UIStorageGrid UIGame.inventory
                gameModule.GetType("UIGame").AddFied("inventory", gameModule.GetType("UIStorageGrid"));

                // Add method: void PlanetTransport.RefreshTraffic(int)
                gameModule.GetType("PlanetTransport").AddMethod("RefreshTraffic", gameModule.TypeSystem.Void, new TypeReference[] { gameModule.TypeSystem.Int32 });
            }
            catch (Exception e)
            {
                logSource.LogError("Add UIStorageGrid UIGame.inventory fail!");
                logSource.LogError(e);
            }
        }

        public static void AddFied(this TypeDefinition typeDefinition, string fieldName, TypeReference fieldType)
        {
            var newField = new FieldDefinition(fieldName, FieldAttributes.Public, fieldType);
            typeDefinition.Fields.Add(newField);
            logSource.LogDebug("Add " + newField);
        }

        public static void AddMethod(this TypeDefinition typeDefinition, string methodName, TypeReference returnType, TypeReference[] parmeterTypes)
        {
            var newMethod = new MethodDefinition(methodName, MethodAttributes.Public, returnType);
            foreach (var p in parmeterTypes)
                newMethod.Parameters.Add(new ParameterDefinition(p));
            typeDefinition.Methods.Add(newMethod);
            logSource.LogDebug("Add " + newMethod);
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
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ModFixerOne.Mods
{
    public static class PersonalLogistics
    {
        public const string NAME = "PersonalLogistics";
        public const string GUID = "semarware.dysonsphereprogram.PersonalLogistics";
        public const string VERSION = "2.9.10";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            if (pluginInfo.Metadata.Version.ToString() != VERSION)
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                // replace uiGame.inventory with uiGame.inventoryWindow
                var transplier = new HarmonyMethod(typeof(Common_Patch).GetMethod("UIInventory_Transpiler"));
                var postfix = new HarmonyMethod(typeof(PersonalLogistics).GetMethod("Postfix"));

                var classType = assembly.GetType("PersonalLogistics.Scripts.RecycleWindow");
                var methodInfo = AccessTools.Method(classType, "Update");
                harmony.Patch(methodInfo, null, null, transplier);
                methodInfo = AccessTools.Method(classType, "AddShowRecycleCheck");
                harmony.Patch(methodInfo, null, null, transplier);

                classType = assembly.GetType("PersonalLogistics.Scripts.RequesterWindow");
                methodInfo = AccessTools.Method(classType, "Update");
                harmony.Patch(methodInfo, null, null, transplier);

                classType = assembly.GetType("PersonalLogistics.PersonalLogisticsPlugin");
                methodInfo = AccessTools.Method(classType, "InitUi");
                harmony.Patch(methodInfo, null, postfix, transplier);

                if (GameConfig.gameVersion.Build >= 15033) //DSP version 0.9.27.15033
                {
                    classType = assembly.GetType("PersonalLogistics.PlayerInventory.TrashHandler");
                    methodInfo = AccessTools.Method(classType, "ProcessTasks");
                    harmony.Patch(methodInfo, null, null, new HarmonyMethod(typeof(PersonalLogistics).GetMethod(nameof(RemoveTrash_Transpiler))));
                }

                Plugin.Log.LogInfo($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} - Fail! Last target version: {VERSION}");
                Fixer_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Plugin.Log.LogDebug(e);
            }
        }

        public static void Postfix()
        {
            Plugin.Log.LogDebug("init");
        }

        public static IEnumerable<CodeInstruction> RemoveTrash_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace: container.RemoveTrash(index);
                // to:      GameMain.data.trashSystem.RemoveTrash(index);
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "RemoveTrash"))
                    .SetInstruction(
                        Transpilers.EmitDelegate<Action<TrashContainer, int>>(
                            (_, index) =>
                            {
                                GameMain.data.trashSystem.RemoveTrash(index);
                            }
                        )
                    );
                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("RemoveTrash_Transpiler fail!");
#if DEBUG
                Plugin.Log.LogWarning(e);
#endif
                return instructions;
            }
        }
    }
}

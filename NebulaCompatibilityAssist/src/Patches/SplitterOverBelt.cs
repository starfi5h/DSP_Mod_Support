using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NebulaCompatibilityAssist.Patches
{
    public static class SplitterOverBelt
    {
        public const string NAME = "SplitterOverBelt";
        public const string GUID = "com.hetima.dsp.SplitterOverBelt";
        public const string VERSION = "1.1.3";

        private static List<int> removingBeltEids;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;
            removingBeltEids = new List<int>();

            try
            {
                // Make reconnect of belts work on client
                Type classType = assembly.GetType("SplitterOverBelt.SplitterOverBelt");
                harmony.Patch(AccessTools.Method(classType, "DeleteConfusedBelts"), null, null,
                    new HarmonyMethod(typeof(SplitterOverBelt).GetMethod("DeleteConfusedBelts_Transpiler")));
                harmony.Patch(AccessTools.Method(classType, "ConnectBelts"), null,
                    new HarmonyMethod(typeof(SplitterOverBelt).GetMethod("ConnectBelts_Postfix")));
                harmony.Patch(AccessTools.Method(classType, "ValidateBelt2"),
                    new HarmonyMethod(typeof(SplitterOverBelt).GetMethod("ValidateBelt2_Prefix")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static IEnumerable<CodeInstruction> DeleteConfusedBelts_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace : tool.actionBuild.DoDismantleObject(e.id);
                // with    : RecordAndDismantleObject(tool.actionBuild, e.id);
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "DoDismantleObject"))
                    .Repeat(matcher => matcher
                            .SetAndAdvance(OpCodes.Call, typeof(SplitterOverBelt).GetMethod(nameof(RecordAndDismantleObject)))
                    );

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Warn("DeleteConfusedBelts_Transpiler fail!");
                Log.Dev(e);
                return instructions;
            }
        }

        public static bool RecordAndDismantleObject(PlayerAction_Build action_Build, int entityId)
        {
            // Recorde entityId that will be removed on clients
            removingBeltEids.Add(entityId);
            return action_Build.DoDismantleObject(entityId);
        }

        public static void ConnectBelts_Postfix()
        {
            // Clean up record after reconnection are all done
            removingBeltEids.Clear();
        }

        public static bool ValidateBelt2_Prefix(BuildTool_Click tool, EntityData entityData, out bool validBelt, out bool isOutput)
        {
            isOutput = false;
            validBelt = false;

            int objId = entityData.id;
            bool hasOutput = false;
            bool hasInput = false;
            for (int i = 0; i < 4; i++)
            {
                tool.factory.ReadObjectConn(objId, i, out bool isOutput2, out int otherId, out int _);
                // Due to DoDismantleObject will not take effect immediately on clients, additional test for removingBeltEids are required
                if (otherId != 0 && !removingBeltEids.Contains(otherId))
                {
                    if (isOutput2)
                    {
                        hasInput = true;
                    }
                    else
                    {
                        hasOutput = true;
                    }
                }
            }
            if (hasInput == hasOutput)
            {
                validBelt = false;
            }
            else if (hasInput)
            {
                isOutput = true;
                validBelt = true;
            }
            else if (hasOutput)
            {
                isOutput = false;
                validBelt = true;
            }

            return false;
        }
    }
}

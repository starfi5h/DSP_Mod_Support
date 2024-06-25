using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NebulaCompatibilityAssist.Patches
{
    public static class UXAssist_Patch
    {
        public const string NAME = "UXAssist";
        public const string GUID = "org.soardev.uxassist";
        public const string VERSION = "1.0.26";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                Type classType = assembly.GetType("UXAssist.PlanetFunctions");

                // 行星工廠 - 快速拆除所有建築
                harmony.Patch(AccessTools.Method(classType, "DismantleAll"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(DismantleAll_Prefix))));

                // 行星工廠 - 初始化本行星
                harmony.Patch(AccessTools.Method(classType, "RecreatePlanet"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(RecreatePlanet_Prefix))));

                // 行星工廠 - 快速建造轨道采集器
                harmony.Patch(AccessTools.Method(classType, "BuildOrbitalCollectors"), null, null,
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(BuildOrbitalCollectors_Transpiler))));

                classType = assembly.GetType("UXAssist.DysonSpherePatch");

                // 戴森球 - 初始化戴森球/快速拆除戴森壳
                harmony.Patch(AccessTools.Method(classType, "InitCurrentDysonSphere"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(DismantleAll_Prefix))));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Warn(e);
            }
        }

        public static void DismantleAll_Prefix()
        {
            if (NebulaModAPI.IsMultiplayerActive && GameMain.localPlanet != null)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_UXA_Packet(NC_UXA_Packet.EType.DismantleAll, GameMain.localPlanet.id, NebulaModAPI.MultiplayerSession.LocalPlayer.Id));
            }
        }

        public static bool RecreatePlanet_Prefix()
        {
            if (!NebulaModAPI.IsMultiplayerActive) return true;

            var title = "Unavailable";
            var message = "This function is not usable in multiplayer mode.\n该功能在多人游戏模式下不可用";
            UIMessageBox.Show(title, message, "确定".Translate(), 3);
            return false;
        }

        public static IEnumerable<CodeInstruction> BuildOrbitalCollectors_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace AddPrebuildDataWithComponents with custom one
                var codeMatcher = new CodeMatcher(instructions)
                    .End()
                    .MatchBack(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "AddPrebuildDataWithComponents"))
                    .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(UXAssist_Patch), nameof(AddPrebuildDataWithComponents)));

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Warn("BuildOrbitalCollectors_Transpiler fail!");
                Log.Warn(e);
                return instructions;
            }
        }

        static int AddPrebuildDataWithComponents(PlanetFactory factory, PrebuildData prebuild)
        {
            if (NebulaModAPI.IsMultiplayerActive)
            {
                var packet = new NC_UXA_Packet(NC_UXA_Packet.EType.BuildOrbitalCollector, GameMain.localPlanet.id, NebulaModAPI.MultiplayerSession.LocalPlayer.Id);
                packet.Value1 = prebuild.pos.x;
                packet.Value2 = prebuild.pos.y;
                packet.Value3 = prebuild.pos.z;
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(packet);
            }
            return factory.AddPrebuildDataWithComponents(prebuild);
        }

        public static void InitCurrentDysonSphere_Prefix(int index)
        {
            var star = GameMain.localStar;
            if (star == null) return;
            if (NebulaModAPI.IsMultiplayerActive)
            {
                var packet = new NC_UXA_Packet
                {
                    Type = NC_UXA_Packet.EType.InitDysonSphere,
                    Value1 = star.index, // dysonIndex
                    Value2 = index // layerId (-1 => remove all)
                };
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }
        }
    }
}

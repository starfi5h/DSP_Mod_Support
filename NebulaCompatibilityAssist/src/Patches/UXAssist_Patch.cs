﻿using HarmonyLib;
using NebulaAPI;
using NebulaAPI.GameState;
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
        public const string VERSION = "1.3.2";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                Type classType = assembly.GetType("UXAssist.Functions.PlanetFunctions");

                // 行星工廠 - 快速拆除所有建築
                harmony.Patch(AccessTools.Method(classType, "DismantleAll"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(DismantleAll_Prefix))));

                // 行星工廠 - 初始化本行星
                harmony.Patch(AccessTools.Method(classType, "RecreatePlanet"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(RecreatePlanet_Prefix))));

                // 行星工廠 - 快速建造轨道采集器
                harmony.Patch(AccessTools.Method(classType, "BuildOrbitalCollectors"), null, null,
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(BuildOrbitalCollectors_Transpiler))));

                classType = assembly.GetType("UXAssist.Functions.DysonSphereFunctions");

                // 戴森球 - 初始化戴森球/快速拆除戴森壳
                harmony.Patch(AccessTools.Method(classType, "InitCurrentDysonLayer"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(InitCurrentDysonLayer_Prefix))));

                classType = assembly.GetType("UXAssist.Patches.LogisticsPatch+LogisticsConstrolPanelImprovement");

                // 物流系統改進 - 在控制台物流塔清單中右鍵點選物品圖示快速設定為篩選條件
                harmony.Patch(AccessTools.Method(classType, "OnStationEntryItemIconRightClick"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(OnStationEntryItemIconRightClick_Prefix))));


                classType = assembly.GetType("UXAssist.Patches.LogisticsPatch+AutoConfigLogistics");

                // 自動配置物流站
                harmony.Patch(AccessTools.Method(classType, "DoConfigStation"),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(DoConfigStation_Prefix))),
                    new HarmonyMethod(typeof(UXAssist_Patch).GetMethod(nameof(DoConfigStation_Postfix))));

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
                var packet = new NC_UXA_Packet(NC_UXA_Packet.EType.BuildOrbitalCollector, GameMain.localPlanet.id, NebulaModAPI.MultiplayerSession.LocalPlayer.Id)
                {
                    Value1 = prebuild.pos.x,
                    Value2 = prebuild.pos.y,
                    Value3 = prebuild.pos.z
                };
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(packet);
            }
            return factory.AddPrebuildDataWithComponents(prebuild);
        }

        public static void InitCurrentDysonLayer_Prefix(StarData star, int layerId)
        {
            if (star == null) return;
            if (NebulaModAPI.IsMultiplayerActive)
            {
                var packet = new NC_UXA_Packet
                {
                    Type = NC_UXA_Packet.EType.InitDysonSphere,
                    Value1 = star.index, // dysonIndex
                    Value2 = layerId // layerId (-1 => remove all)
                };
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }
        }

        public static bool OnStationEntryItemIconRightClick_Prefix(UIControlPanelStationEntry stationEntry, int slot)
        {
            if (stationEntry.factory != null) return true; // Vanilla entry
            // In MP client, the remote entry will have null factory

            var itemId = 0;
            switch(slot)
            {
                case 0: itemId = stationEntry.storageItem0.itemButton.tips.itemId; break;
                case 1: itemId = stationEntry.storageItem1.itemButton.tips.itemId; break;
                case 2: itemId = stationEntry.storageItem2.itemButton.tips.itemId; break;
                case 3: itemId = stationEntry.storageItem3.itemButton.tips.itemId; break;
                case 4: itemId = stationEntry.storageItem4.itemButton.tips.itemId; break;
            }
            if (itemId == 0) return false;
            var filterPanel = UIRoot.instance.uiGame.controlPanelWindow.filterPanel;
            var filter = filterPanel.GetCurrentFilter();
            if (filter.itemsFilter is { Length: 1 } && filter.itemsFilter[0] == itemId) return false;
            filter.itemsFilter = new int[1] { itemId };
            filterPanel.SetNewFilter(filter);
            filterPanel.RefreshFilterUI();
            UIRoot.instance.uiGame.controlPanelWindow.DetermineFilterResults();
            return false;
        }

        public static bool DoConfigStation_Prefix()
        {
            if (!NebulaModAPI.IsMultiplayerActive)
                return true;

            // Apply StationConfig if author (the drone owner) is local player
            IFactoryManager factoryManager = NebulaModAPI.MultiplayerSession.Factories;
            return factoryManager.PacketAuthor == NebulaModAPI.MultiplayerSession.LocalPlayer.Id
                || (NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost && factoryManager.PacketAuthor == NebulaModAPI.AUTHOR_NONE);
        }

        public static void DoConfigStation_Postfix(PlanetFactory __0, StationComponent __1, bool __runOriginal)
        {
            if (NebulaModAPI.IsMultiplayerActive && __runOriginal)
            {
                PlanetFactory factory = __0;
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationConfig(__1, factory));
                NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(
                    new NC_StationShipCount(__1, factory.planetId));
            }
        }
    }
}

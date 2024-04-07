using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using NebulaModel.Packets.Factory.Assembler;
using NebulaWorld;
using System;
using System.Reflection;

#pragma warning disable IDE0018 // 內嵌變數宣告

namespace NebulaCompatibilityAssist.Patches
{
    public static class AssemblerVerticalConstruction
    {
        private const string NAME = "AssemblerVerticalConstruction";
        private const string GUID = "lltcggie.DSP.plugin.AssemblerVerticalConstruction";
        private const string VERSION = "1.1.4";

        private static IModCanSave Save;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {                
                Save = pluginInfo.Instance as IModCanSave;
                NC_Patch.OnLogin += SendRequest;
                NC_ModSaveRequest.OnReceive += (guid, conn) =>
                {
                    if (guid != GUID) return;
                    conn.SendPacket(new NC_ModSaveData(GUID, Export()));
                };
                NC_ModSaveData.OnReceive += (guid, bytes) =>
                {
                    if (guid != GUID) return;
                    Import(bytes);
                };
                
                Type classType = assembly.GetType("AssemblerVerticalConstruction.AssemblerPatches");
                harmony.Patch(AccessTools.Method(classType, "SyncAssemblerFunctions"), 
                    new HarmonyMethod(typeof(AssemblerVerticalConstruction).GetMethod("SyncAssemblerFunctions_Prefix")));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static void SendRequest()
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ModSaveRequest(GUID));
            }
        }

        public static void SendData()
        {
            if (NebulaModAPI.IsMultiplayerActive)
            {
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ModSaveData(GUID, Export()));
            }
        }

        public static byte[] Export()
        {
            if (Save != null)
            {
                using var p = NebulaModAPI.GetBinaryWriter();
                Save.Export(p.BinaryWriter);
                return p.CloseAndGetBytes();
            }
            else
            {
                return new byte[0];
            }
        }

        public static void Import(byte[] bytes)
        {
            if (Save != null)
            {
                Log.Dev($"AssemblerVerticalConstruction import data");
                using var p = NebulaModAPI.GetBinaryReader(bytes);
                Save.Import(p.BinaryReader);
            }
        }

        public static bool SyncAssemblerFunctions_Prefix(FactorySystem factorySystem, Player player, int assemblerId)
        {
            // Boradcast recipeId changes to other players

            var _this = factorySystem;
            int entityId = _this.assemblerPool[assemblerId].entityId;
            if (entityId == 0)
            {
                return false;
            }

            int num = entityId;
            do
            {
                int num3;
                _this.factory.ReadObjectConn(num, PlanetFactory.kMultiLevelInputSlot, out _, out num3, out _);
                num = num3;
                if (num > 0)
                {
                    int assemblerId2 = _this.factory.entityPool[num].assemblerId;
                    if (assemblerId2 > 0 && _this.assemblerPool[assemblerId2].id == assemblerId2)
                    {
                        if (_this.assemblerPool[assemblerId].recipeId > 0)
                        {
                            if (_this.assemblerPool[assemblerId2].recipeId != _this.assemblerPool[assemblerId].recipeId)
                            {
                                _this.TakeBackItems_Assembler(player, assemblerId2);
                                var recipeId = _this.assemblerPool[assemblerId].recipeId;
                                _this.assemblerPool[assemblerId2].SetRecipe(recipeId, _this.factory.entitySignPool);
                                Multiplayer.Session.Network.SendPacketToLocalStar(
                                    new AssemblerRecipeEventPacket(factorySystem.planet.id, assemblerId2, recipeId));
                            }
                        }
                        else if (_this.assemblerPool[assemblerId2].recipeId != 0)
                        {
                            _this.TakeBackItems_Assembler(player, assemblerId2);
                            _this.assemblerPool[assemblerId2].SetRecipe(0, _this.factory.entitySignPool);
                            Multiplayer.Session.Network.SendPacketToLocalStar(
                                new AssemblerRecipeEventPacket(factorySystem.planet.id, assemblerId2, 0));
                        }
                    }
                }
            }
            while (num != 0);

            num = entityId;
            do
            {
                int num3;
                _this.factory.ReadObjectConn(num, PlanetFactory.kMultiLevelOutputSlot, out _, out num3, out _);
                num = num3;
                if (num > 0)
                {
                    int assemblerId3 = _this.factory.entityPool[num].assemblerId;
                    if (assemblerId3 > 0 && _this.assemblerPool[assemblerId3].id == assemblerId3)
                    {
                        if (_this.assemblerPool[assemblerId].recipeId > 0)
                        {
                            if (_this.assemblerPool[assemblerId3].recipeId != _this.assemblerPool[assemblerId].recipeId)
                            {
                                _this.TakeBackItems_Assembler(_this.factory.gameData.mainPlayer, assemblerId3);
                                var recipeId = _this.assemblerPool[assemblerId].recipeId;
                                _this.assemblerPool[assemblerId3].SetRecipe(recipeId, _this.factory.entitySignPool);
                                Multiplayer.Session.Network.SendPacketToLocalStar(
                                    new AssemblerRecipeEventPacket(factorySystem.planet.id, assemblerId3, recipeId));
                            }
                        }
                        else if (_this.assemblerPool[assemblerId3].recipeId != 0)
                        {
                            _this.TakeBackItems_Assembler(_this.factory.gameData.mainPlayer, assemblerId3);
                            _this.assemblerPool[assemblerId3].SetRecipe(0, _this.factory.entitySignPool);
                            Multiplayer.Session.Network.SendPacketToLocalStar(
                                new AssemblerRecipeEventPacket(factorySystem.planet.id, assemblerId3, 0));
                        }
                    }
                }
            }
            while (num != 0);

            return false;
        }
    }
}

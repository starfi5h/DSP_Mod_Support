using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;

namespace NebulaCompatibilityAssist.Patches
{
    public static class Dustbin_Patch
    {
        private const string NAME = "Dustbin";
        private const string GUID = "org.soardev.dustbin";
        private const string VERSION = "1.2.1";
        private static bool installed = false;
        private static IModCanSave Save;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                Save = pluginInfo.Instance as IModCanSave;
                // (warp in separate functions to avoid refering invaild type)
                RegEvent();
                harmony.PatchAll(typeof(Dustbin_Patch));
                installed = true;

                Log.Info($"{NAME} - OK");
                NC_Patch.RequriedPlugins += " +" + NAME;
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        public static void OnDestory()
        {
            if (installed) UnregEvent();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIStorageGrid), nameof(UIStorageGrid.OnStorageSizeChanged))]
        private static void UIStorageWindow_OnStorageDataChanged()
        {
            // Due to storage window is empty when open in client, the size will change after receiving data from host
            Dustbin.StoragePatch._lastStorageId = -1; // Refresh UI to reposition button on client side
        }

        private static void RegEvent()
        {
            // Sync all mod data when client load PlanetFactory
            NebulaModAPI.OnPlanetLoadFinished += OnPlanetFactoryRequest;
            NC_ModSaveData.OnReceive += (guid, bytes) =>
            {
                if (guid != GUID) return;
                Import(bytes);
            };
            // Sync onClick event
            Dustbin.StoragePatch._storageDustbinCheckBox.OnChecked += OnCheckStorage;
            Dustbin.TankPatch._tankDustbinCheckBox.OnChecked += OnCheckTank;
        }

        private static void UnregEvent()
        {
            // NC_ModSaveData.OnReceive will reset when plugin is reloaded
            NebulaModAPI.OnPlanetLoadFinished -= OnPlanetFactoryRequest;
            Dustbin.StoragePatch._storageDustbinCheckBox.OnChecked -= OnCheckStorage;
            Dustbin.TankPatch._tankDustbinCheckBox.OnChecked -= OnCheckTank;
        }

        private static void OnCheckStorage()
        {
            if (!NebulaModAPI.IsMultiplayerActive) return;

            var window = UIRoot.instance.uiGame.storageWindow;
            int storageId = window.storageId;
            if (storageId <= 0) return;
            int planetId = window.factory.planetId;
            bool enable = Dustbin.StoragePatch._storageDustbinCheckBox.Checked;
            NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(new NC_DustbinEvent(planetId, storageId, 0, enable));
            //Log.Debug($"[Dustbin] Planet {planetId} - storage {storageId} set to {enable}");
        }

        private static void OnCheckTank()
        {
            if (!NebulaModAPI.IsMultiplayerActive) return;

            var window = UIRoot.instance.uiGame.tankWindow;
            int tankId = window.tankId;
            if (tankId <= 0) return;
            int planetId = window.factory.planetId;
            bool enable = Dustbin.TankPatch._tankDustbinCheckBox.Checked;
            NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(new NC_DustbinEvent(planetId, 0, tankId, enable));
            //Log.Debug($"[Dustbin] Planet {planetId} - tank {tankId} set to {enable}");
        }

        public static void OnPlanetFactoryRequest(int planetId)
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
                NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_DustbinEvent(planetId, -1, -1, false));
        }

        public static void OnEventReceive(NC_DustbinEvent packet, INebulaConnection conn)
        {
            PlanetFactory factory = GameMain.galaxy.PlanetById(packet.PlanetId)?.factory;
            if (factory == null) return;

            if (packet.StorageId > 0)
            {
                Dustbin.StoragePatch._lastStorageId = -1; // Reset lastId to refersh UI
                var storagePool = factory.factoryStorage.storagePool;
                if (storagePool[packet.StorageId] is not Dustbin.StorageComponentWithDustbin comp) return;
                comp.IsDusbin = packet.Enable;
                //Log.Debug($"[Dustbin] Planet {packet.PlanetId} - storage {packet.StorageId} set to {packet.Enable}");
            }
            if (packet.TankId > 0)
            {
                Dustbin.TankPatch.lastTankId = -1; // Reset lastId to refersh UI
                Dustbin.TankPatch.tankIsDustbin[packet.PlanetId / 100][packet.PlanetId % 100][packet.TankId] = packet.Enable;
                //Log.Debug($"[Dustbin] Planet {packet.PlanetId} - storage {packet.TankId} set to {packet.Enable}");
            }
            if (packet.StorageId == -1 && packet.TankId == -1)
            {
                conn.SendPacket(new NC_ModSaveData(GUID, Export(factory)));
            }
        }

        public static byte[] Export(PlanetFactory factory)
        {
            int planetId = factory.planetId;
            var storageIds = new List<int>();
            var tankIds = new List<int>();

            var storagePool = factory.factoryStorage.storagePool;
            for (int i = 1; i < factory.factoryStorage.storageCursor; i++)
            {
                if (storagePool[i] != null && storagePool[i].id == i)
                {
                    if (storagePool[i] is Dustbin.StorageComponentWithDustbin comp && comp.IsDusbin)
                        storageIds.Add(i);
                }
            }
            
            var tankIsDustbin = Dustbin.TankPatch.tankIsDustbin[planetId / 100]?[planetId % 100];
            if (tankIsDustbin != null)
            {
                var tankPool = factory.factoryStorage.tankPool;
                for (int i = 1; i < factory.factoryStorage.tankCursor; i++)
                {
                    if (tankPool[i].id == i && tankIsDustbin[i])
                    {
                        tankIds.Add(i);
                    }
                }
            }

            using var p = NebulaModAPI.GetBinaryWriter();
            using var w = p.BinaryWriter;
            w.Write(planetId);
            w.Write(storageIds.Count);
            foreach (var storageId in storageIds)
                w.Write(storageId);
            w.Write(tankIds.Count);
            foreach (var tankId in tankIds)
                w.Write(tankId);
            return p.CloseAndGetBytes();
        }

        public static void Import(byte[] bytes)
        {
            using var p = NebulaModAPI.GetBinaryReader(bytes);
            using var r = p.BinaryReader;
            int planetId = r.ReadInt32();
            PlanetFactory factory = GameMain.galaxy.PlanetById(planetId)?.factory;
            if (factory == null) return;

            int count = r.ReadInt32();
            var storagePool = factory.factoryStorage.storagePool;
            for (int i = 0; i < count; i++)
            {
                int id = r.ReadInt32();
                if (storagePool[id] is Dustbin.StorageComponentWithDustbin comp)
                {
                    comp.IsDusbin = true;
                }
            }

            count = r.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int id = r.ReadInt32();
                Dustbin.TankPatch.tankIsDustbin[planetId/100][planetId%100][id] = true;
            }
        }
    }
}

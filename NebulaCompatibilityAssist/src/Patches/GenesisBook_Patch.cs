using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Hotfix;
using NebulaCompatibilityAssist.Packets;
using NebulaModel.Packets.Factory.Storage;
using NebulaPatcher.Patches.Dynamic;
using NebulaWorld;
using ProjectGenesis.Patches.Logic.QuantumStorage;
using ProjectGenesis.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class GenesisBook_Patch
    {
        public const string NAME = "GenesisBook";
        public const string GUID = "org.LoShin.GenesisBook";
        public const string VERSION = "3.0.10";

        const int QuantumStorageSize = 80;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var _))
                return;

            try
            {
                harmony.PatchAll(typeof(Warper));
                harmony.PatchAll(typeof(StorageComponent_Patch));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Debug(e);
            }
        }

        static class Warper
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(UIStorageWindow_Patch), nameof(UIStorageWindow_Patch.OnStorageIdChange_Prefix))]
            public static bool OnStorageIdChange_Prefix(ref bool __result)
            {
                // Skip original Nebula storage box content syncing
                __result = true;
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SyncNewQuantumStorageData), nameof(SyncNewQuantumStorageData.Sync))]
            [HarmonyPatch(typeof(SyncRemoveQuantumStorageData), nameof(SyncRemoveQuantumStorageData.Sync))]
            public static bool Block()
            {
                // Those two functions are already on the Nebula call path, so additional syncing is not needed
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(QuantumStoragePatches), nameof(QuantumStoragePatches.QuantumStorageOrbitChange))]
            public static bool QuantumStorageOrbitChange(int planetId, int storageId, int orbitId)
            {
                Log.Debug($"QuantumStorageOrbitChange p{planetId} s{storageId} o{orbitId}");

                if (!QuantumStoragePatches.QuantumStorageIds.TryGetValue(planetId, out List<QuantumStorageData> datas)) return false;

                int index = datas.FindIndex(i => i.StorageId == storageId);
                if (index < 0) return false;

                // For client, skip if the factory is not loaded
                var factoryStorage = GameMain.galaxy.PlanetById(planetId)?.factory?.factoryStorage;
                if (factoryStorage == null) return false;

                factoryStorage.storagePool[storageId] = QuantumStoragePatches._components[orbitId - 1];
                datas[index] = new QuantumStorageData(storageId, orbitId);

                var window = UIRoot.instance.uiGame.storageWindow;
                if (GameMain.localPlanet?.id == planetId && window.factory == GameMain.localPlanet.factory && window.storageId == storageId)
                {
                    // Set orbitId for orbitPicker and update storage grid
                    window.OnStorageIdChange();
                }
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(UIStorageWindow), nameof(UIStorageWindow.OnStorageIdChange))]
            public static void OnStorageIdChange_Prefix(UIStorageWindow __instance)
            {
                if (!NebulaModAPI.IsMultiplayerActive || !NebulaModAPI.MultiplayerSession.IsClient) return;
                if (!__instance.active || __instance.factory == null || __instance.storageId == 0) return;

                int planetId = __instance.factory.planetId;
                int storageId = __instance.storageId;
                if (!QuantumStoragePatches.QuantumStorageIds.TryGetValue(planetId, out List<QuantumStorageData> datas)) return;
                int index = datas.FindIndex(i => i.StorageId == storageId);
                if (index < 0) return;

                // Request latest storage content from server
                Log.Debug($"StroageBoxRequest p{planetId} s{storageId} o{datas[index].OrbitId}");
                var packet = new NC_GB_Packet(NC_GB_Packet.EType.StroageBoxRequest, planetId, datas[index].OrbitId, Array.Empty<byte>());
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(QuantumStoragePatches), nameof(QuantumStoragePatches.OnOrbitPickerButtonClick))]
            static void OnOrbitPickerButtonClick_Postfix(int orbitId)
            {
                var window = UIRoot.instance.uiGame.storageWindow;
                if (!window.active || window.factory == null) return;

                if (NebulaModAPI.IsMultiplayerActive)
                {
                    // Broadcast the orbit change to all other players that may have factory loaded
                    NebulaModAPI.MultiplayerSession.Network.SendPacketToLocalStar(new SyncQuantumStorageOrbitChangeData(window.factory.planetId, window.storageId, orbitId));
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(QuantumStoragePatches), nameof(QuantumStoragePatches.ImportPlanetQuantumStorage))]
            static void ImportPlanetQuantumStorage_Postfix()
            {
                // This part is load after the factory data is loaded in client (Import_PatchMethod)
                // So we need to direct the storagepool (Import_PatchMethod)
                Log.Debug("ImportPlanetQuantumStorage assign");

                if (GameMain.localPlanet?.factory == null) return;
                int planetId = GameMain.localPlanet?.id ?? 0;
                if (!QuantumStoragePatches.QuantumStorageIds.TryGetValue(planetId, out var list)) return;

                var requestedOrbits = new HashSet<int>();
                var stroagePool = GameMain.localPlanet.factory.factoryStorage.storagePool;
                foreach (var data in list)
                {
                    stroagePool[data.StorageId] = QuantumStoragePatches._components[data.OrbitId - 1];
                    if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.IsClient)
                    {
                        // Request latest storage content from server
                        if (requestedOrbits.Contains(data.OrbitId)) continue;
                        requestedOrbits.Add(data.OrbitId);
                        Log.Debug($"StroageBoxRequest p{planetId} s{data.StorageId} o{data.OrbitId}");
                        var packet = new NC_GB_Packet(NC_GB_Packet.EType.StroageBoxRequest, planetId, data.OrbitId, Array.Empty<byte>());
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);                        
                    }
                }
            }
        }

        [HarmonyPatch(typeof(StorageComponent))]
        internal class StorageComponent_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(StorageComponent.AddItem),
                new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) },
                new[]
                {
            ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal,
            ArgumentType.Out
                })]
            public static void AddItem_Prefix(StorageComponent __instance, int itemId, int count, int startIndex, int length, int inc)
            {
                int storageId = GetQuantumStorageId(__instance);
                if (storageId == 0) return;

                HandleUserInteraction(
                    new StorageSyncRealtimeChangePacket(storageId, StorageSyncRealtimeChangeEvent.AddItem2, 
                    itemId, count, startIndex, length, inc));
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(StorageComponent.AddItemStacked))]
            public static void AddItemStacked_Prefix(StorageComponent __instance, int itemId, int count, int inc)
            {
                int storageId = GetQuantumStorageId(__instance);
                if (storageId == 0) return;

                HandleUserInteraction(
                    new StorageSyncRealtimeChangePacket(storageId, StorageSyncRealtimeChangeEvent.AddItemStacked,
                    itemId, count, inc));
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(StorageComponent.TakeItemFromGrid))]
            public static void TakeItemFromGrid_Prefix(StorageComponent __instance, int gridIndex, ref int itemId, ref int count)
            {
                int storageId = GetQuantumStorageId(__instance);
                if (storageId == 0) return;

                HandleUserInteraction(
                    new StorageSyncRealtimeChangePacket(storageId, StorageSyncRealtimeChangeEvent.TakeItemFromGrid,
                    gridIndex, itemId, count, 0));
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(StorageComponent.SetBans))]
            public static void SetBans_Postfix(StorageComponent __instance, int _bans)
            {
                int storageId = GetQuantumStorageId(__instance);
                if (storageId == 0) return;

                HandleUserInteraction(new StorageSyncSetBansPacket(storageId, GameMain.data.localPlanet.id, _bans));
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(StorageComponent.Sort))]
            public static void Sort_Postfix(StorageComponent __instance)
            {
                int storageId = GetQuantumStorageId(__instance);
                if (storageId == 0) return;

                HandleUserInteraction(new StorageSyncSortPacket(storageId, GameMain.data.localPlanet.id));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StorageComponent), nameof(StorageComponent.SetFilter))]
            private static void SetFilter_Postfix(StorageComponent __instance, int gridIndex, int filterId)
            {
                int storageId = GetQuantumStorageId(__instance);
                if (storageId == 0) return;

                HandleUserInteraction(new StorageSyncSetFilterPacket(storageId, GameMain.data.localPlanet.id, gridIndex, filterId, __instance.type));
            }

            private static int GetQuantumStorageId(StorageComponent __instance)
            {
                if (__instance.id != 0 || __instance.size != QuantumStorageSize) return 0;

                if (Multiplayer.IsActive && !Multiplayer.Session.Storage.IsIncomingRequest &&
                    Multiplayer.Session.Storage.IsHumanInput && GameMain.data.localPlanet != null)
                {
                    var stroagePool = GameMain.localPlanet.factory.factoryStorage.storagePool;
                    int length = GameMain.localPlanet.factory.factoryStorage.storageCursor;
                    for (int i = 1; i < length; i++)
                    {
                        if (stroagePool[i] == __instance)
                        {
                            return i;
                        }
                    }
                }
                return 0;
            }

            private static void HandleUserInteraction<T>(T packet) where T : class, new()
            {
                Log.Debug(packet);

                if (Multiplayer.Session.LocalPlayer.IsHost)
                {
                    // Assume storage is on the local planet, send to all clients in local star system who may have the factory loaded
                    Multiplayer.Session.Network.SendPacketToStar(packet, GameMain.localStar.id);
                }
                else
                {
                    Multiplayer.Session.Network.SendPacket(packet);
                }
            }
        }
    }
}

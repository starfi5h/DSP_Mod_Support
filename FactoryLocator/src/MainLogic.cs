using CommonAPI.Systems;
using System.Collections.Generic;
using UnityEngine;

namespace FactoryLocator
{
    public class MainLogic
    {
        public int SignalId { get; set; } = 401;

        private readonly List<PlanetFactory> factories = new();
        private readonly Dictionary<int, int> filterIds = new();
        private readonly List<int> planetIds = new();
        private readonly List<Vector3> localPos = new();
        private readonly List<int> detailIds = new();

        public int SetFactories(StarData star, PlanetData planet)
        {
            factories.Clear();
            if (star != null)
            {
                foreach (var p in star.planets)
                {
                    if (p?.factory != null)
                        factories.Add(p.factory);
                }
            }
            else
            {
                if (planet?.factory != null)
                    factories.Add(planet.factory);
            }

            var ratios = new List<float>();
            var counts = new List<int>();
            foreach (var factory in factories)
            {
                if (factory.powerSystem != null)
                {
                    for (int i = 1; i < factory.powerSystem.netCursor; i++)
                    {
                        PowerNetwork powerNetwork = factory.powerSystem.netPool[i];
                        if (powerNetwork != null && powerNetwork.id == i)
                        {
                            ratios.Add((float)powerNetwork.consumerRatio);
                            counts.Add(powerNetwork.consumers.Count);
                        }
                    }
                }
            }
            Plugin.mainWindow.SetStatusTipText(ratios.ToArray(), counts.ToArray());

#if DEBUG
            string s = $"SetFactories {factories.Count}:";
            foreach (var f in factories)
                s += f.planetId + " ";
            Log.Debug(s);
#endif

            return factories.Count;
        }
        
        public void PickBuilding(int _)
        {
            RefreshBuilding(-1);
            UIentryCount.OnOpen(ESignalType.Item, filterIds);
            UIItemPickerExtension.Popup(new Vector2(-300f, 250f), OnBuildingPickReturn, itemProto => filterIds.ContainsKey(itemProto.ID));
            UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(2);
        }

        public void OnBuildingPickReturn(ItemProto itemProto)
        {
            if (itemProto == null) // Return by ESC
                return;
            int itemId = itemProto.ID;
            RefreshBuilding(itemId);
            WarningSystemPatch.AddWarningData(SignalId, itemId, planetIds, localPos);
            UIentryCount.OnClose();
        }

        public void PickVein(int _)
        {
            RefreshVein(-1);
            UIentryCount.OnOpen(ESignalType.Item, filterIds);
            UIItemPickerExtension.Popup(new Vector2(-300f, 250f), OnVeinPickReturn, true, itemProto => filterIds.ContainsKey(itemProto.ID));
            UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(1);
        }

        public void OnVeinPickReturn(ItemProto itemProto)
        {
            if (itemProto == null) // Return by ESC
                return;
            int itemId = itemProto.ID;
            RefreshVein(itemId);
            WarningSystemPatch.AddWarningData(SignalId, itemId, planetIds, localPos);
            UIentryCount.OnClose();
        }

        public void PickAssembler(int _)
        {
            RefreshAssemblers(-1);
            UIentryCount.OnOpen(ESignalType.Recipe, filterIds);
            UIRecipePickerExtension.Popup(new Vector2(-300f, 250f), OnAssemblerPickReturn, recipeProto => filterIds.ContainsKey(recipeProto.ID));
        }

        public void OnAssemblerPickReturn(RecipeProto recipeProto)
        {
            if (recipeProto == null) // Return by ESC
                return;
            int recipeId = recipeProto.ID;
            RefreshAssemblers(recipeId);
            WarningSystemPatch.AddWarningData(SignalId, SignalProtoSet.SignalId(ESignalType.Recipe, recipeId), planetIds, localPos);
            UIentryCount.OnClose();
        }

        public void PickWarning(int _)
        {
            RefreshSignal(-1);
            UIentryCount.OnOpen(ESignalType.Signal, filterIds);
            UISignalPickerExtension.Popup(new Vector2(-300f, 250f), OnWarningPickReturn, signalId => filterIds.ContainsKey(signalId));
            UIRoot.instance.uiGame.signalPicker.OnTypeButtonClick(1);
        }

        public void OnWarningPickReturn(int signalId)
        {
            if (signalId <= 0) // Return by ESC
                return;
            RefreshSignal(signalId);
            WarningSystemPatch.AddWarningData(SignalId, signalId, planetIds, localPos, detailIds);
            UIentryCount.OnClose();
        }

        public void PickStorage(int _)
        {
            RefreshStorage(-1);
            UIentryCount.OnOpen(ESignalType.Item, filterIds);
            UIItemPickerExtension.Popup(new Vector2(-300f, 250f), OnStoragePickReturn, itemProto => filterIds.ContainsKey(itemProto.ID));
        }

        public void OnStoragePickReturn(ItemProto itemProto)
        {
            if (itemProto == null) // Return by ESC
                return;
            int itemId = itemProto.ID;
            RefreshStorage(itemId);
            WarningSystemPatch.AddWarningData(SignalId, itemId, planetIds, localPos);
            UIentryCount.OnClose();
        }

        public void PickStation(int _)
        {
            RefreshStation(-1);
            UIentryCount.OnOpen(ESignalType.Item, filterIds);
            UIItemPickerExtension.Popup(new Vector2(-300f, 250f), OnStationPickReturn, itemProto => filterIds.ContainsKey(itemProto.ID));
        }

        public void OnStationPickReturn(ItemProto itemProto)
        {
            if (itemProto == null) // Return by ESC
                return;
            int itemId = itemProto.ID;
            RefreshStation(itemId);
            WarningSystemPatch.AddWarningData(SignalId, itemId, planetIds, localPos);
            UIentryCount.OnClose();
        }

        // Internal functions

        public void RefreshBuilding(int itemId)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();

            foreach (var factory in factories)
            {
                for (int id = 0; id < factory.entityCursor; id++)
                {
                    if (id == factory.entityPool[id].id)
                    {
                        if (itemId == -1)
                        {
                            int key = factory.entityPool[id].protoId;
                            if (filterIds.ContainsKey(key))
                                ++filterIds[key];
                            else
                                filterIds[key] = 1;
                        }
                        else
                        {
                            if (itemId == factory.entityPool[id].protoId)
                            {
                                localPos.Add(factory.entityPool[id].pos + factory.entityPool[id].pos.normalized * 0.5f);
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshVein(int itemId)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();

            EVeinType veinTypeByItemId = EVeinType.None;
            if (itemId > 0)
                veinTypeByItemId = LDB.veins.GetVeinTypeByItemId(itemId);

            foreach (var factory in factories)
            {
                for (int id = 1; id < factory.veinGroups.Length; id++)
                {
                    if (factory.veinGroups[id].type != EVeinType.None)
                    {
                        if (itemId == -1)
                        {
                            int key;
                            if (factory.veinGroups[id].type == EVeinType.Oil)
                            {
                                // Special case for item Crude Oil
                                key = 1007;
                            }
                            else
                            {
                                key = LDB.veins.Select((int)factory.veinGroups[id].type).MiningItem;
                            }
                            if (filterIds.ContainsKey(key))
                                ++filterIds[key];
                            else
                                filterIds[key] = 1;
                        }
                        else
                        {
                            if (veinTypeByItemId == factory.veinGroups[id].type)
                            {
                                localPos.Add(factory.veinGroups[id].pos.normalized * (factory.planet.realRadius + 0.5f));
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshAssemblers(int recipeId)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();

            foreach (var factory in factories)
            {
                for (int id = 1; id < factory.factorySystem.assemblerCursor; id++)
                {
                    if (id == factory.factorySystem.assemblerPool[id].id)
                    {
                        if (recipeId == -1)
                        {
                            int key = factory.factorySystem.assemblerPool[id].recipeId;
                            if (filterIds.ContainsKey(key))
                                ++filterIds[key];
                            else
                                filterIds[key] = 1;
                        }
                        else
                        {
                            if (recipeId == factory.factorySystem.assemblerPool[id].recipeId)
                            {
                                ref EntityData entity = ref factory.entityPool[factory.factorySystem.assemblerPool[id].entityId];
                                localPos.Add(entity.pos + entity.pos.normalized * 0.5f);
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshSignal(int signalId)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();
            detailIds.Clear();

            foreach (var factory in factories)
            {
                for (int id = 1; id < factory.entityCursor; id++)
                {
                    if (id == factory.entityPool[id].id)
                    {
                        if (signalId == -1)
                        {
                            if (factory.entitySignPool[id].signType > 0)
                            {
                                int key = (int)factory.entitySignPool[id].signType + 500;
                                if (filterIds.ContainsKey(key))
                                    ++filterIds[key];
                                else
                                    filterIds[key] = 1;
                            }
                        }
                        else
                        {
                            if (signalId == (int)factory.entitySignPool[id].signType + 500)
                            {
                                localPos.Add(factory.entityPool[id].pos + factory.entityPool[id].pos.normalized * 0.5f);
                                detailIds.Add(factory.entityPool[id].protoId);
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshStorage(int itemId)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();

            foreach (var factory in factories)
            {
                for (int id = 1; id < factory.factoryStorage.storageCursor; id++)
                {
                    StorageComponent storage = factory.factoryStorage.storagePool[id];
                    if (storage != null && storage.id == id)
                    {
                        if (itemId == -1)
                        {
                            for (int i = 0; i < storage.size; i++)
                            {
                                if (storage.grids[i].count > 0)
                                {
                                    int key = storage.grids[i].itemId;
                                    if (!filterIds.ContainsKey(key))
                                        filterIds[key] = storage.grids[i].count;
                                    else
                                        filterIds[key] += storage.grids[i].count;
                                }
                            }
                        }
                        else
                        {
                            bool flag = false;
                            for (int i = 0; i < storage.size; i++)
                            {
                                if (storage.grids[i].itemId == itemId && storage.grids[i].count > 0)
                                {
                                    ref EntityData entity = ref factory.entityPool[storage.entityId];
                                    localPos.Add(entity.pos + entity.pos.normalized * 0.5f);
                                    planetIds.Add(factory.planetId);
                                    flag = true;
                                }
                                if (flag)
                                    break;
                            }
                        }
                    }
                }
                for (int id = 1; id < factory.factoryStorage.tankCursor; id++)
                {
                    ref TankComponent tank = ref factory.factoryStorage.tankPool[id];
                    if (tank.id == id)
                    {
                        if (itemId == -1)
                        {
                            int key = tank.fluidId;
                            if (filterIds.ContainsKey(key))
                                filterIds[key] += tank.fluidCount;
                            else
                                filterIds[key] = tank.fluidCount;
                        }
                        else
                        {
                            if (itemId == tank.fluidId)
                            {
                                ref EntityData entity = ref factory.entityPool[tank.entityId];
                                localPos.Add(entity.pos + entity.pos.normalized * 0.5f);
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshStation(int itemId)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();

            foreach (var factory in factories)
            {
                for (int id = 1; id < factory.transport.stationCursor; id++)
                {
                    StationComponent station = factory.transport.stationPool[id];
                    if (station != null && station.id == id && station.storage != null)
                    {
                        if (itemId == -1)
                        {
                            for (int i = 0; i < station.storage.Length; i++)
                            {
                                int key = station.storage[i].itemId;
                                if (filterIds.ContainsKey(key))
                                    filterIds[key] += station.storage[i].count;
                                else
                                    filterIds[key] = station.storage[i].count;
                            }
                        }
                        else
                        {
                            bool flag = false;
                            for (int i = 0; i < station.storage.Length; i++)
                            {
                                if (station.storage[i].itemId == itemId)
                                {
                                    ref EntityData entity = ref factory.entityPool[station.entityId];
                                    localPos.Add(entity.pos + entity.pos.normalized * 0.5f);
                                    planetIds.Add(factory.planetId);
                                    flag = true;
                                }
                                if (flag)
                                    break;
                            }
                        }
                    }
                }
            }
        }

    }
}

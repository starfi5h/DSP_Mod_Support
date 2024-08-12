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

        private int state; // Internal state to record the last catagory
        private readonly List<int> networkIds = new();
        private readonly HashSet<int> tmp_ids = new();

        public int SetFactories(StarData star, PlanetData planet)
        {
            state = 0;
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
            var consumersCount = new List<int>();
            networkIds.Clear();
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
                            consumersCount.Add(powerNetwork.consumers.Count);
                            networkIds.Add(i);
                        }
                    }
                }
            }
            if (Plugin.mainWindow.active && Plugin.mainLogic == this)
            {
                Plugin.mainWindow.SetStatusTipText(ratios.ToArray(), consumersCount.ToArray());
                Plugin.mainWindow.SetPowerNetworkDropdownList(factories.Count == 1 ? networkIds : null);
            }

#if DEBUG
            string s = $"SetFactories {factories.Count}:";
            foreach (var f in factories)
                s += f.planetId + " ";
            Log.Debug(s);
#endif

            return factories.Count;
        }

        public void PickBuilding(int networkId)
        {
            state = networkId;
            RefreshBuilding(-1, state);
            UIentryCount.OnOpen(ESignalType.Item, filterIds);
            UIItemPickerExtension.Popup(new Vector2(-300f, 250f), OnBuildingPickReturn, itemProto => filterIds.ContainsKey(itemProto.ID));
            UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(2);
        }

        public void OnBuildingPickReturn(ItemProto itemProto)
        {
            if (itemProto == null) // Return by ESC
                return;
            int itemId = itemProto.ID;
            RefreshBuilding(itemId, state);
            WarningSystemPatch.AddWarningData(SignalId, itemId, planetIds, localPos);
            UIentryCount.OnClose();
        }

        public void PickVein(int mode)
        {
            state = mode;
            RefreshVein(-1, state);
            UIentryCount.OnOpen(ESignalType.Item, filterIds);
            UIItemPickerExtension.Popup(new Vector2(-300f, 250f), OnVeinPickReturn, true, itemProto => filterIds.ContainsKey(itemProto.ID));
            UIRoot.instance.uiGame.itemPicker.OnTypeButtonClick(1);
        }

        public void OnVeinPickReturn(ItemProto itemProto)
        {
            if (itemProto == null) // Return by ESC
                return;
            int itemId = itemProto.ID;
            RefreshVein(itemId, state);
            WarningSystemPatch.AddWarningData(SignalId, itemId, planetIds, localPos);
            UIentryCount.OnClose();
        }

        public void PickAssembler(int mode)
        {
            state = mode;
            RefreshAssemblers(-1, state);
            UIentryCount.OnOpen(ESignalType.Recipe, filterIds);
            UIRecipePickerExtension.Popup(new Vector2(-300f, 250f), OnAssemblerPickReturn, recipeProto => filterIds.ContainsKey(recipeProto.ID));
        }

        public void OnAssemblerPickReturn(RecipeProto recipeProto)
        {
            if (recipeProto == null) // Return by ESC
                return;
            int recipeId = recipeProto.ID;
            RefreshAssemblers(recipeId, state);
            WarningSystemPatch.AddWarningData(SignalId, SignalProtoSet.SignalId(ESignalType.Recipe, recipeId), planetIds, localPos);
            UIentryCount.OnClose();
        }

        public void PickWarning(int _)
        {
            state = 0;
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
            state = 0;
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

        public void PickStation(int mode)
        {
            state = mode;
            RefreshStation(-1, state);
            UIentryCount.OnOpen(ESignalType.Item, filterIds);
            UIItemPickerExtension.Popup(new Vector2(-300f, 250f), OnStationPickReturn, itemProto => filterIds.ContainsKey(itemProto.ID));
        }

        public void OnStationPickReturn(ItemProto itemProto)
        {
            if (itemProto == null) // Return by ESC
                return;
            int itemId = itemProto.ID;
            RefreshStation(itemId, state);
            WarningSystemPatch.AddWarningData(SignalId, itemId, planetIds, localPos);
            UIentryCount.OnClose();
        }

        // Internal functions

        public void RefreshBuilding(int itemId, int comboIndex = 0)
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
                        ref var entity = ref factory.entityPool[id];
                        if (comboIndex != 0 && comboIndex <= networkIds.Count && networkIds[comboIndex - 1] != GetPowerNetworkId(factory, in entity))
                        {
                            continue;
                        }

                        if (itemId == -1) // picking
                        {
                            int key = entity.protoId;
                            if (filterIds.ContainsKey(key))
                                ++filterIds[key];
                            else
                                filterIds[key] = 1;
                        }
                        else
                        {
                            if (itemId == entity.protoId)
                            {
                                localPos.Add(entity.pos + entity.pos.normalized * 0.5f);
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }
            }
        }

        public static int GetPowerNetworkId(PlanetFactory factory, in EntityData entity)
        {
            if (entity.powerConId > 0)
            {
                return factory.powerSystem.consumerPool[entity.powerConId].networkId;
            }
            if (entity.powerNodeId > 0)
            {
                return factory.powerSystem.nodePool[entity.powerNodeId].networkId;
            }
            if (entity.powerGenId > 0)
            {
                return factory.powerSystem.genPool[entity.powerGenId].networkId;
            }
            if (entity.powerExcId > 0)
            {
                return factory.powerSystem.excPool[entity.powerExcId].networkId;
            }
            if (entity.powerAccId > 0)
            {
                return factory.powerSystem.accPool[entity.powerAccId].networkId;
            }
            return 0;
        }

        public void RefreshVein(int itemId, int mode = 0)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();

            EVeinType veinTypeByItemId = EVeinType.None;
            if (itemId > 0)
                veinTypeByItemId = LDB.veins.GetVeinTypeByItemId(itemId);


            foreach (var factory in factories)
            {
                if (mode != 0)
                {
                    tmp_ids.Clear();
                    for (int id = 1; id < factory.veinPool.Length; id++)
                    {
                        if (factory.veinPool[id].id == id && factory.veinPool[id].minerCount != 0)
                        {
                            tmp_ids.Add(factory.veinPool[id].groupIndex);
                        }
                    }
                }

                for (int id = 1; id < factory.veinGroups.Length; id++)
                {
                    if (factory.veinGroups[id].type != EVeinType.None)
                    {
                        if (mode == 1 && !tmp_ids.Contains(id)) continue; // planned
                        if (mode == 2 && tmp_ids.Contains(id)) continue;  // unplanned

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

        public void RefreshAssemblers(int recipeId, int mode = 0)
        {
            filterIds.Clear();
            localPos.Clear();
            planetIds.Clear();

            foreach (var factory in factories)
            {
                for (int id = 1; id < factory.factorySystem.assemblerCursor; id++)
                {
                    ref var assembler = ref factory.factorySystem.assemblerPool[id];
                    if (id == assembler.id)
                    {
                        if (mode != 0 && mode != GetAssemblerMode(in assembler))
                            continue;

                        if (recipeId == -1)
                        {
                            int key = assembler.recipeId;
                            if (filterIds.ContainsKey(key))
                                ++filterIds[key];
                            else
                                filterIds[key] = 1;
                        }
                        else
                        {
                            if (recipeId == assembler.recipeId)
                            {
                                ref EntityData entity = ref factory.entityPool[assembler.entityId];
                                localPos.Add(entity.pos + entity.pos.normalized * 0.5f);
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }

                // For labs in production mode
                for (int id = 1; id < factory.factorySystem.labCursor; id++)
                {
                    ref var lab = ref factory.factorySystem.labPool[id];
                    if (id == lab.id)
                    {
                        if (mode != 0 && mode != GetLabMode(in lab))
                            continue;

                        if (recipeId == -1)
                        {
                            int key = lab.recipeId;
                            if (filterIds.ContainsKey(key))
                                ++filterIds[key];
                            else
                                filterIds[key] = 1;
                        }
                        else
                        {
                            if (recipeId == lab.recipeId)
                            {
                                ref EntityData entity = ref factory.entityPool[lab.entityId];
                                localPos.Add(entity.pos + entity.pos.normalized * 0.5f);
                                planetIds.Add(factory.planetId);
                            }
                        }
                    }
                }
            }
        }

        public static int GetAssemblerMode(in AssemblerComponent assembler)
        {
            if (assembler.replicating)
            {
                return 0;
            }
            else
            {
                if (assembler.time >= assembler.timeSpend) //产物堆积 Product overflow
                {
                    return 2;
                }
                for (int j = 0; j < assembler.requireCounts.Length; j++)
                {
                    if (assembler.served[j] < assembler.requireCounts[j])
                    {
                        return 1; //缺少原材料	Lack of material
                    }
                }
                return 3; // 其他?
            }
        }

        public static int GetLabMode(in LabComponent lab)
        {
            if (lab.replicating)
            {
                return 0;
            }
            else
            {
                if (lab.time >= lab.timeSpend) //产物堆积 Product overflow
                {
                    return 2;
                }
                for (int j = 0; j < lab.requireCounts.Length; j++)
                {
                    if (lab.served[j] < lab.requireCounts[j])
                    {
                        return 1; //缺少原材料	Lack of material
                    }
                }
                return 3; // 其他?
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

        public void RefreshStation(int itemId, int mode = 0)
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
                        if (mode == 1) // Local (PLS, Miner Mk.2)
                        {
                            if (station.gid > 0) continue;
                        }
                        else if (mode == 2) // Interstellar (ILS, collectors)
                        {
                            if (station.gid <= 0) continue;
                        }

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

using DSP_Battle;
using HarmonyLib;
using NebulaAPI;
using NebulaWorld;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSP_Battle_Patch
    {
        private const string NAME = "DSP_Battle";
        private const string GUID = "com.ckcz123.DSP_Battle";
        private const string VERSION = "2.2.8";
        private static bool installed = false;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                NebulaModAPI.OnPlayerJoinedGame += Warper.OnPlayerJoinedGame;
                NC_ModSaveData.OnReceive += Warper.ImportData;
                harmony.PatchAll(typeof(Warper));
                harmony.PatchAll(typeof(Warper_RemoteBuildings));

                Log.Info($"{NAME} - OK");
                NC_Patch.RequriedPlugins += " +" + NAME;
                installed = true;
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
            if (installed)
            {
                NebulaModAPI.OnPlayerJoinedGame -= Warper.OnPlayerJoinedGame;
            }
        }

        // All types in DSP_Battle should contain in Warper class
        public static class Warper
        {
            private static bool isIncomingPacket = false;

            private static void ShowMessageInChat(string text)
            {
                Hotfix.ChatManager.ShowMessageInChat("[Battle] " + text);
            }

            #region Sync battle wave stage

            public static void ExportData(int stage, int playerId)
            {
                if (NebulaModAPI.IsMultiplayerActive)
                {
                    using var p = NebulaModAPI.GetBinaryWriter();
                    var w = p.BinaryWriter;

                    w.Write(stage);                    
                    int seed = EnemyShips.random.Next(); 
                    EnemyShips.random = new System.Random(seed); // Sync random seed
                    w.Write(seed);
                    DSP_Battle.Configs.Export(w);
                    if (stage == 0 || stage == 3)
                    {
                        // The current impelmentation don't sync stuff created during battle
                        //EnemyShips.Export(w);
                        //Cannon.Export(w);
                        //MissileSilo.Export(w);
                        UIAlert.Export(w);
                        WormholeProperties.Export(w);
                        StarCannon.Export(w);
                        ShieldGenerator.Export(w);
                        Droplets.Export(w);
                        Rank.Export(w);
                        Relic.Export(w);
                        UIBattleStatistics_Export(w);
                        StarFortress.Export(w);
                    }
                    else if (stage == 2)
                    {
                        EnemyShips.Export(w);
                        Warper_RemoteBuildings.Export(w);
                    }


                    byte[] data = p.CloseAndGetBytes();
                    if (playerId >= 0)
                    {
                        // Send to target player
                        var player = NebulaModAPI.MultiplayerSession.Network.PlayerManager.GetPlayerById((ushort)playerId);
                        player.SendPacket(new NC_ModSaveData(GUID, data));
                    }
                    else if (playerId == -1)
                    {
                        // Send to all players
                        NebulaModAPI.MultiplayerSession.Network.SendPacket(new NC_ModSaveData(GUID, data));
                    }
                }
            }

            public static void ImportData(string guid, byte[] bytes)
            {
                if (guid != GUID)
                    return;

                using var p = NebulaModAPI.GetBinaryReader(bytes);
                var r = p.BinaryReader;
                isIncomingPacket = true;

                int stage = r.ReadInt32();
                int seed = r.ReadInt32();
                EnemyShips.random = new System.Random(seed); // Sync random seed
                DSP_Battle.Configs.Import(r);
                if (stage == 0 || stage == 3)
                {
                    // The current impelmentation don't sync stuff created during battle
                    EnemyShips.IntoOtherSave();
                    Cannon.IntoOtherSave();
                    MissileSilo.IntoOtherSave();
                    UIAlert.Import(r);
                    WormholeProperties.Import(r);
                    StarCannon.Import(r);
                    ShieldGenerator.Import(r);
                    Droplets.Import(r);
                    Rank.Import(r);
                    Relic.Import(r);
                    UIBattleStatistics_Import(r);
                    StarFortress.Import(r);
                }
                else if (stage == 2)
                {
                    EnemyShips.Import(r);
                    Warper_RemoteBuildings.Import(r);
                }

                WaveStages.ResetCargoAccIncTable(DSP_Battle.Configs.extraSpeedEnabled && Rank.rank >= 5);
                if (stage == 0) // Login first time
                {
                    UIBattleStatistics.InitAll();
                    UIBattleStatistics.InitSelectDifficulty();
                    EnemyShipUIRenderer.Init();
                    EnemyShipRenderer.Init();
                    BattleProtos.ReCheckTechUnlockRecipes();
                    BattleBGMController.InitWhenLoad();

                    UIAlert.UIRoot_OnGameBegin();
                }
                else if (stage == 1) // Wave generated
                {
                    ShowUIDialog1();
                }
                else if (stage == 2) // Wave start
                {
                    StarFortress.cannonChargeProgress = 600; // 战斗开始默认光矛充能完毕
                }
                else if (stage == 3) // Wave end
                {
                    ShowUIDialog3();
                }

                isIncomingPacket = false;
            }

            public static void OnPlayerJoinedGame(IPlayerData playerData)
            {
                ExportData(0, playerData.PlayerId);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(WaveStages), nameof(WaveStages.UpdateWaveStage0))]
            static void UpdateWaveStage0_Postfix()
            {
                if (DSP_Battle.Configs.nextWaveState == 1) // moving to the next stage
                {
                    if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                        ExportData(1, -1); // Host send wave generated signal and mod data to all clients

                    RecyleDroplets(); //強制回收運行中的水滴
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(WaveStages), nameof(WaveStages.UpdateWaveStage2))]
            static bool UpdateWaveStage2_Prefix()
            {
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
                {
                    return isIncomingPacket; // In client, only update if it is tirgger by host
                }
                return true;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(WaveStages), nameof(WaveStages.UpdateWaveStage2))]
            static void UpdateWaveStage2_Postfix()
            {
                if (DSP_Battle.Configs.nextWaveState == 3) // moving to the next stage
                {
                    if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                        ExportData(2, -1); // Host send wave start signal to sync random seed
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(WaveStages), nameof(WaveStages.UpdateWaveStage3))]
            static void UpdateWaveStage3_Postfix()
            {
                if (DSP_Battle.Configs.nextWaveState == 0) // moving to the next stage
                {
                    if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                        ExportData(3, -1); // Host send wave end signal and mod data to all clients
                }
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(WaveStages), nameof(WaveStages.UpdateWaveStage3))]
            static IEnumerable<CodeInstruction> UpdateWaveStage3_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    // replace : EnemyShips.ships.Count == 0
                    // with    : EnemyShips.ships.Count == 0 && IsHost
                    var codeMatcher = new CodeMatcher(instructions)
                        .MatchForward(true,
                            new CodeMatch(i => i.opcode == OpCodes.Ldsfld && ((FieldInfo)i.operand).Name == "ships"),
                            new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "get_Count")
                        )
                        .Advance(1)
                        .Insert(
                            Transpilers.EmitDelegate<Func<int, int>>
                            (
                                (count) =>
                                {
                                    // Don't let client exit stage 3 itself
                                    if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
                                        return 1; // dummy value greater than 0
                                    return count;
                                }
                            )
                        );
                    return codeMatcher.InstructionEnumeration();
                }
                catch (Exception e)
                {
                    Log.Warn("UpdateWaveStage3_Transpiler fail!");
                    Log.Dev(e);
                    return instructions;
                }
            }

            #endregion

            #region Sync battle Statistics UI

            public static void UIBattleStatistics_Export(BinaryWriter w)
            {
                w.Write(UIBattleStatistics.totalEnemyEliminated);
                w.Write(UIBattleStatistics.totalEnemyGen);
                w.Write(UIBattleStatistics.alienMatrixGain);
                w.Write(UIBattleStatistics.battleTime);
                w.Write(UIBattleStatistics.totalEnemyEliminated);
                w.Write(UIBattleStatistics.totalDamage);
                w.Write(UIBattleStatistics.stationLost);
                w.Write(UIBattleStatistics.othersLost);
                w.Write(UIBattleStatistics.resourceLost);
            }

            public static void UIBattleStatistics_Import(BinaryReader r)
            {
                UIBattleStatistics.totalEnemyEliminated = r.ReadInt32();
                UIBattleStatistics.totalEnemyGen = r.ReadInt32();
                UIBattleStatistics.alienMatrixGain = r.ReadInt32();
                UIBattleStatistics.battleTime = r.ReadInt64();
                UIBattleStatistics.totalEnemyEliminated = r.ReadInt32();
                UIBattleStatistics.totalDamage = r.ReadInt64();
                UIBattleStatistics.stationLost = r.ReadInt64();
                UIBattleStatistics.othersLost = r.ReadInt64();
                UIBattleStatistics.resourceLost = r.ReadInt64();
            }

            static void ShowUIDialog1()
            {
                if (DSP_Battle.Configs.nextWaveElite == 1)
                    UIDialogPatch.ShowUIDialog("下一波精英攻击即将到来！".Translate(), 
                        string.Format("做好防御提示精英".Translate(), GameMain.galaxy.stars[DSP_Battle.Configs.nextWaveStarIndex].displayName));
                else
                    UIDialogPatch.ShowUIDialog("下一波攻击即将到来！".Translate(),
                        string.Format("做好防御提示".Translate(), GameMain.galaxy.stars[DSP_Battle.Configs.nextWaveStarIndex].displayName));
            }

            static void ShowUIDialog3()
            {
                // [COPY] In WaveStages.UpdateWaveStage3(long time)
                RemoveEntities.distroyedStation.Clear();

                long rewardBase = 5 * 60 * 60;
                long extraSpeedFrame = 0;
                if (UIBattleStatistics.totalEnemyGen > 0)
                {
                    double percent = Math.Min(1.0, 1.0 * UIBattleStatistics.totalEnemyEliminated / UIBattleStatistics.totalEnemyGen);
                    extraSpeedFrame = (long)(percent * rewardBase);
                }
                extraSpeedFrame += 3600 * (Rank.rank / 2);

                string rewardByRank = string.Format("奖励提示0".Translate(), extraSpeedFrame / 60);
                if (Rank.rank >= 7)
                {
                    rewardByRank = string.Format("奖励提示7".Translate(), extraSpeedFrame / 60);
                }
                else if (Rank.rank >= 5)
                {
                    rewardByRank = string.Format("奖励提示5".Translate(), extraSpeedFrame / 60);
                }
                else if (Rank.rank >= 3)
                {
                    rewardByRank = string.Format("奖励提示3".Translate(), extraSpeedFrame / 60);
                }
                string dropMatrixStr = "掉落的异星矩阵".Translate() + ": " + UIBattleStatistics.alienMatrixGain.ToString();
                UIDialogPatch.ShowUIDialog("战斗已结束！".Translate(),
                    "战斗时间".Translate() + ": " + string.Format("{0:00}:{1:00}", new object[] { UIBattleStatistics.battleTime / 60 / 60, UIBattleStatistics.battleTime / 60 % 60 }) + "; " +
                    "歼灭敌人".Translate() + ": " + UIBattleStatistics.totalEnemyEliminated.ToString("N0") + "; " +
                    "输出伤害".Translate() + ": " + UIBattleStatistics.totalDamage.ToString("N0") + "; " +
                    "损失物流塔".Translate() + ": " + UIBattleStatistics.stationLost.ToString("N0") + "; " +
                    "损失其他建筑".Translate() + ": " + UIBattleStatistics.othersLost.ToString("N0") + "; " +
                    "损失资源".Translate() + ": " + UIBattleStatistics.resourceLost.ToString("N0") + "; " +
                    dropMatrixStr + "." +
                    "\n\n<color=#c2853d>" + rewardByRank + "</color>\n\n" +
                    "查看更多战斗信息".Translate()
                    );

                // Only let host choose relic for battle balance
                //if (Configs.nextWaveElite == 1 || (Configs.totalWave <= 1 && Relic.GetRelicCount() == 0)) 
                //    Relic.PrepareNewRelic(); // 精英波次结束后给予遗物选择，第一次接敌完成也给遗物
                BattleBGMController.SetWaveFinished();
            }

            #endregion

            #region Sync Configs event (nextWaveFrameIndex, difficulty)

            [HarmonyPrefix, HarmonyPatch(typeof(DspBattlePlugin), nameof(DspBattlePlugin.Update))]
            static void Update_Prefix(ref long __state)
            {
                __state = DSP_Battle.Configs.nextWaveFrameIndex;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(DspBattlePlugin), nameof(DspBattlePlugin.Update))]
            static void Update_Postfix(long __state)
            {
                if (NebulaModAPI.IsMultiplayerActive)
                {
                    if (__state != DSP_Battle.Configs.nextWaveFrameIndex)
                    {
                        SendConfig();
                    }
                }
            }

            [HarmonyPrefix, HarmonyPatch(typeof(UIBattleStatistics), nameof(UIBattleStatistics.OnDifficultyChange))]
            static bool OnDifficultyChange_Prefix()
            {
                if (UIBattleStatistics.difficultyComboBox.itemIndex - 1 == DSP_Battle.Configs.difficulty)
                {
                    return false;
                }
                UIMessageBox.Show("调整难度标题".Translate(), string.Format("调整难度警告".Translate(), UIBattleStatistics.difficultyComboBox.text), "否".Translate(), "是".Translate(), 1, new UIMessageBox.Response(UIBattleStatistics.InitSelectDifficulty), delegate ()
                {
                    DSP_Battle.Configs.difficulty = UIBattleStatistics.difficultyComboBox.itemIndex - 1;
                    UIBattleStatistics.InitSelectDifficulty();
                    UIMessageBox.Show("设置成功！".Translate(), string.Format("难度设置成功".Translate(), UIBattleStatistics.difficultyComboBox.text), "确定".Translate(), 1);
                    if (NebulaModAPI.IsMultiplayerActive) SendConfig();
                });
                return false;
            }

            public static void SendConfig()
            {
                using var p = NebulaModAPI.GetBinaryWriter();
                var w = p.BinaryWriter;
                DSP_Battle.Configs.Export(w);
                var packet = new NC_BattleEvent(NC_BattleEvent.EType.Configs, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, p.CloseAndGetBytes());
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }

            public static void SyncConfig(BinaryReader r, ushort playerId)
            {
                int difficulty = DSP_Battle.Configs.difficulty;
                DSP_Battle.Configs.Import(r);

                if (difficulty != DSP_Battle.Configs.difficulty)
                {
                    string text = "Difficulty changed by player " + playerId + ": " + difficulty + "->" + DSP_Battle.Configs.difficulty;
                    ShowMessageInChat(text);
                }                
            }

            #endregion

            #region Sync RemoveEntities event

            [HarmonyPrefix, HarmonyPatch(typeof(EnemyShips), nameof(EnemyShips.OnShipLanded))]
            static bool OnShipLanded_Prefix(EnemyShip ship)
            {
                if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                    return true;

                // Client only put the station into distroyedStation to remove it from target
                // The real building removing is performed by Host
                Log.Debug("=========> Ship " + ship.shipIndex.ToString() + " landed at station " + ship.shipData.otherGId.ToString());
                RemoveEntities.distroyedStation[ship.shipData.otherGId] = 0;
                ship.state = EnemyShip.State.distroyed;
                ship.shipData.inc--;
                if (ship.shipData.inc > 0)
                {
                    // Set the landed ship state to tempoary disable, until host give new target
                    //ship.state = EnemyShip.State.active;
                }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(RemoveEntities), nameof(RemoveEntities.Add))]
            static void RemoveEntitiesAdd(EnemyShip ship, StationComponent station)
            {
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    using var p = NebulaModAPI.GetBinaryWriter();
                    var w = p.BinaryWriter;
                    w.Write(ship.shipData.planetB);
                    w.Write(station.entityId);
                    w.Write(ship.damageRange);
                    w.Write(station.id);
                    var packet = new NC_BattleEvent(NC_BattleEvent.EType.RemoveEntities, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, p.CloseAndGetBytes());
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);

                    PlanetData planet = GameMain.galaxy.PlanetById(ship.shipData.planetB);
                    ShowMessageInChat(string.Format("station-{0} is attacked on {1}".Translate(), station.id, planet.displayName));
                }
            }

            public static void SyncRemoveEntities(BinaryReader r)
            {
                int planetId = r.ReadInt32();
                int entityId = r.ReadInt32();
                int damageRange = r.ReadInt32();
                int id = r.ReadInt32();

                PlanetData planet = GameMain.galaxy.PlanetById(planetId);
                ShowMessageInChat(string.Format("station-{0} is attacked on {1}".Translate(), id, planet.displayName));
                PlanetFactory factory = planet.factory;
                if (factory == null)
                    return;
                Vector3 stationPos = factory.entityPool[entityId].pos;

                if (!RemoveEntities.pendingDestroyedEntities.ContainsKey(planetId))
                    RemoveEntities.pendingDestroyedEntities.Add(planetId, new List<Tuple<Vector3, int>>());
                RemoveEntities.pendingDestroyedEntities[planetId].Add(new Tuple<Vector3, int>(stationPos, damageRange));
            }

            #endregion

            #region Sync StarCannon StartAiming event

            [HarmonyPostfix, HarmonyPatch(typeof(StarCannon), nameof(StarCannon.StartAiming))]
            static void StartAiming_Postfix()
            {
                if (NebulaModAPI.IsMultiplayerActive && !isIncomingPacket)
                {
                    var packet = new NC_BattleEvent(NC_BattleEvent.EType.StarCannonStartAiming, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, new byte[0]);
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                }
            }

            public static void SyncStartAiming(ushort playerId)
            {
                ShowMessageInChat("star cannon activate by player ".Translate() + playerId);
                isIncomingPacket = true;
                StarCannon.StartAiming();
                isIncomingPacket = false;
            }

            #endregion

            #region Sync Relic event

            [HarmonyPostfix, HarmonyPatch(typeof(Relic), nameof(Relic.AddRelic))]
            static void AddRelic_Postfix(int type, int num, int __result)
            {
                if (__result != 1 || !NebulaModAPI.IsMultiplayerActive) return; // Selection didn't success or in single player mode

                ShowMessageInChat("add relic ".Translate() + ("遗物名称" + type + "-" + num).Translate().Replace("\n", " "));
                if (!isIncomingPacket)
                {
                    using var p = NebulaModAPI.GetBinaryWriter();
                    var w = p.BinaryWriter;
                    w.Write(type);
                    w.Write(num);
                    var packet = new NC_BattleEvent(NC_BattleEvent.EType.AddRelic, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, p.CloseAndGetBytes());
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                }                
            }

            [HarmonyPrefix, HarmonyPatch(typeof(Relic), nameof(Relic.AskRemoveRelic))] //[COPY]
            static bool AskRemoveRelic_Prefix(int removeType, int removeNum)
            {
                if (!NebulaModAPI.IsMultiplayerActive) return true; // Don't overwirte in single player
                                
                if (removeType > 4 || removeNum > 30)
                {
                    UIMessageBox.Show("Failed".Translate(), "Failed. Unknown relic.".Translate(), "确定".Translate(), 1);
                    Relic.RegretRemoveRelic();
                    return false;
                }
                if (!Relic.HaveRelic(removeType, removeNum))
                {
                    UIMessageBox.Show("Failed".Translate(), "Failed. Relic not have.".Translate(), "确定".Translate(), 1);
                    Relic.RegretRemoveRelic();
                    return false;
                }
                UIMessageBox.Show("删除遗物确认标题".Translate(), string.Format("删除遗物确认警告".Translate(), ("遗物名称" + removeType + "-" + removeNum).Translate().Split(new char[]
                {
                '\n'
                })[0]), "否".Translate(), "是".Translate(), 1, new UIMessageBox.Response(Relic.RegretRemoveRelic), delegate ()
                {
                    RemoveRelic(removeType, removeNum); // Modify
                    UIRelic.CloseSelectionWindow();
                    UIRelic.RefreshSlotsWindowUI(false);
                    UIRelic.HideSlots();
                });
                return false;
            }

            static void RemoveRelic(int removeType, int removeNum)
            {
                Relic.relics[removeType] = (Relic.relics[removeType] ^ 1 << removeNum);
                if (!NebulaModAPI.IsMultiplayerActive) return;

                ShowMessageInChat("remove relic ".Translate() + ("遗物名称" + removeType + "-" + removeNum).Translate().Replace("\n", " "));
                if (!isIncomingPacket)
                {
                    using var p = NebulaModAPI.GetBinaryWriter();
                    var w = p.BinaryWriter;
                    w.Write(removeType);
                    w.Write(removeNum);
                    var packet = new NC_BattleEvent(NC_BattleEvent.EType.RemoveRelic, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, p.CloseAndGetBytes());
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                }
            }

            public static void SyncAddRelic(BinaryReader r)
            {
                int type = r.ReadInt32();
                int num = r.ReadInt32();

                isIncomingPacket = true;
                Relic.AddRelic(type, num);
                UIRelic.RefreshSlotsWindowUI();
                UIRelic.HideSlots();
                isIncomingPacket = false;
            }

            public static void SyncRemoveRelic(BinaryReader r)
            {
                int type = r.ReadInt32();
                int num = r.ReadInt32();

                isIncomingPacket = true;
                RemoveRelic(type, num);
                UIRelic.RefreshSlotsWindowUI();
                UIRelic.HideSlots();
                isIncomingPacket = false;
            }

            #endregion

            #region Replace distance calculation to ship.distanceToTarget

            [HarmonyPrefix, HarmonyPatch(typeof(UIBattleStatistics), nameof(UIBattleStatistics.RegisterIntercept))]
            static bool RegisterIntercept(EnemyShip ship, ref double distance)
            {
                try
                {
                    if (distance < 0)
                    {
                        distance = ship.distanceToTarget; // MOD
                    }
                    Interlocked.Exchange(ref UIBattleStatistics.minInterceptDis, distance < UIBattleStatistics.minInterceptDis ? distance : UIBattleStatistics.minInterceptDis);
                    UIBattleStatistics.allInterceptDis.AddItem(distance);
                }
                catch (Exception) { }
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(UIAlert), nameof(UIAlert.RefreshBattleProgress))] //[COPY]
            static bool RefreshBattleProgress()
            {
                if (!NC_Patch.IsClient) return true;

                int curState = DSP_Battle.Configs.nextWaveState;
                try
                {
                    if ((UIAlert.lastState != 3 && curState == 3) || (curState == 3 && UIAlert.totalDistance == 1 && UIAlert.totalStrength == 1))
                    {
                        UIBattleStatistics.RegisterEnemyGen(); //注册敌人生成信息
                        UIAlert.totalStrength = 0;
                        foreach (var shipIndex in EnemyShips.ships.Keys)
                        {
                            var ship = EnemyShips.ships[shipIndex];

                            UIAlert.totalStrength += ship.hp;
                        }
                    }
                    if (curState != 3 || UIAlert.totalStrength < 1) UIAlert.totalStrength = 1;
                    if (curState != 3 || UIAlert.totalDistance <= 0) UIAlert.totalDistance = 1;
                    if (UIAlert.lastState == 3 && curState != 3)
                    {
                        UIAlert.totalDistance = 1;
                        UIAlert.totalStrength = 1;
                        UIAlert.elimPointRatio = 1.0f;
                        UIAlert.elimProgRT.sizeDelta = new Vector2(0, 12);
                        UIAlert.invaProgRT.sizeDelta = new Vector2(0, 12);
                    }
                    if (curState == 3) //要刷新进度条
                    {
                        double curTotalDistance = 0; // (MOD) 不管距離了, 綠條/紅條只表示總血量
                        double curTotalStrength = 0; 
                        foreach (var shipIndex in EnemyShips.ships.Keys)
                        {
                            var ship = EnemyShips.ships[shipIndex];
                            curTotalStrength += ship.hp;
                        }
                        double elimPoint = (UIAlert.totalStrength - curTotalStrength) * 1.0 / UIAlert.totalStrength;
                        double invaPoint = Mathf.Min((float)((UIAlert.totalDistance - curTotalDistance) * 1.0 / UIAlert.totalDistance), (float)(1 - elimPoint));
                        if (invaPoint < 0) invaPoint = 0;

                        double totalPoint = elimPoint + invaPoint;
                        if (totalPoint <= 0)
                        {
                            UIAlert.elimProgRT.sizeDelta = new Vector2(0, 5);
                            UIAlert.invaProgRT.sizeDelta = new Vector2(0, 5);
                        }
                        else
                        {
                            float leftProp = (float)(elimPoint / totalPoint);
                            UIAlert.elimProgRT.sizeDelta = new Vector2(996 * leftProp * UIAlert.elimPointRatio, 5);
                            UIAlert.invaProgRT.sizeDelta = new Vector2(996 * (1 - leftProp * UIAlert.elimPointRatio), 5);
                        }
                    }
                }
                catch (Exception) { }

                UIAlert.lastState = curState;
                return false;
            }

            #endregion

            #region Sync EnemyShip state

            [HarmonyPrefix, HarmonyPatch(typeof(EnemyShips), nameof(EnemyShips.RemoveShip))]
            static bool RemoveShip()
            {
                if (NC_Patch.IsClient)
                {
                    // Let ship destoryed in client stay until host send packet
                    return isIncomingPacket;
                }
                return true;
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(EnemyShip), nameof(EnemyShip.BeAttacked))]
            static IEnumerable<CodeInstruction> BeAttacked_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
            {
                try
                {
                    // 在客戶端當敵船要被擊落時, 留下一層血皮。直到主機通知再移除
                    // Insert : if (NC_Patch.IsClient) { this.hp = 1; return result; }
                    // Before : UIBattleStatistics.RegisterEliminate(this.intensity, 1);
                    var codeMatcher = new CodeMatcher(instructions, iLGenerator)
                        .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(UIBattleStatistics), nameof(UIBattleStatistics.RegisterEliminate))))
                        .MatchBack(false, new CodeMatch(OpCodes.Ldarg_0))
                        .Insert(new CodeInstruction(OpCodes.Nop))
                        .CreateLabel(out var label)
                        .Insert(
                            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(NC_Patch), nameof(NC_Patch.IsClient))),
                            new CodeInstruction(OpCodes.Brfalse_S, label),
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Ldc_I4_1),
                            new CodeInstruction(OpCodes.Stfld, AccessTools.Field(typeof(EnemyShip), nameof(EnemyShip.hp))),
                            new CodeInstruction(OpCodes.Ldloc_S, (byte)6),
                            new CodeInstruction(OpCodes.Ret)
                        );

                    return codeMatcher.InstructionEnumeration();
                }
                catch (Exception e)
                {
                    Log.Warn("BeAttacked_Transpiler fail!");
                    Log.Dev(e);
                    return instructions;
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(EnemyShip), nameof(EnemyShip.FindAnotherStation))]
            static void OnTargetChange(EnemyShip __instance)
            {
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    Log.Dev($"[Battle] ship retarget [{__instance.shipIndex}]:{__instance.state} P{__instance.shipData.planetB} HP:{__instance.hp}");
                    SendEnemyShipState(__instance);
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(EnemyShips), nameof(EnemyShips.OnShipDestroyed))]
            static void OnShipDestroyed(EnemyShip ship)
            {
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    Log.Dev($"[Battle] ship destory [{ship.shipIndex}]:{ship.state} P{ship.shipData.planetB} HP: {ship.hp}");
                    SendEnemyShipState(ship);
                }
            }

            static void SendEnemyShipState(EnemyShip ship)
            {
                using var p = NebulaModAPI.GetBinaryWriter();
                var w = p.BinaryWriter;
                w.Write(ship.shipIndex);
                w.Write((int)ship.state);

                // Revive
                w.Write(ship.countDown);
                w.Write(ship.hp);
                w.Write(ship.shipData.inc);

                // FindAnotherStation
                w.Write(ship.shipData.stage);
                w.Write(ship.shipData.otherGId);
                w.Write(ship.shipData.planetB);
                
                // Positions
                w.Write(ship.shipData.direction);
                w.Write((float)ship.shipData.uPos.x);
                w.Write((float)ship.shipData.uPos.y);
                w.Write((float)ship.shipData.uPos.z);
                Vector3 eular = ship.shipData.uRot.eulerAngles;
                w.Write(eular.x);
                w.Write(eular.y);
                w.Write(eular.z);
                w.Write(ship.shipData.uSpeed);
                w.Write(ship.shipData.uAngularSpeed);

                var packet = new NC_BattleEvent(NC_BattleEvent.EType.EnemyShipState, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, p.CloseAndGetBytes());
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }

            public static void SyncEnemyShipState(BinaryReader r)
            {
                int shipIndex = r.ReadInt32();
                EnemyShip.State state = (EnemyShip.State)r.ReadInt32();

                try
                {
                    if (EnemyShips.ships.TryGetValue(shipIndex, out var ship))
                    {
                        ship.state = state;
                        ship.countDown = r.ReadInt32();
                        ship.hp = r.ReadInt32();
                        ship.shipData.inc = r.ReadInt32();

                        Log.Dev($"[Battle] ship[{ship.shipIndex}]:{ship.state} HP:{ship.hp} INC:{ship.shipData.inc}");

                        if (ship.state == EnemyShip.State.distroyed)
                        {
                            AfterShipDestoryed(ship);
                            EnemyShips.ships.TryRemove(ship.shipIndex, out _);
                            return;
                        }
                        else if (ship.state == EnemyShip.State.uninitialized)
                        {
                            // EnemyShip.Revive
                            AfterShipDestoryed(ship);
                            int num = DSP_Battle.Configs.enemyIntensity2TypeMap[ship.intensity];
                            ship.damageRange = DSP_Battle.Configs.enemyRange[num];
                            ship.intensity = DSP_Battle.Configs.enemyIntensity[num];
                            ship.isFiring = false;
                            ship.fireStart = 0L;
                            ship.isBlockedByShield = false;
                            ship.forceDisplacementTime = 0;
                        }

                        ship.shipData.stage = r.ReadInt32();
                        ship.shipData.otherGId = r.ReadInt32();
                        ship.shipData.planetB = r.ReadInt32();

                        Log.Dev($"[Battle] ship[{ship.shipIndex}]:{ship.state} HP:{ship.hp} INC:{ship.shipData.inc} Target:{ship.targetStation?.planetId}");

                        ship.shipData.direction = r.ReadInt32();
                        ship.shipData.uPos = new VectorLF3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        ship.shipData.uRot = Quaternion.Euler(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        ship.shipData.uSpeed = r.ReadSingle();
                        ship.shipData.uAngularSpeed = r.ReadSingle();
                    }
                }
                catch
                {
                    Log.Warn($"[Battle] error when updating ship {shipIndex}:{state}");
                }
            }

            static void AfterShipDestoryed(EnemyShip ship) // [COPY] EnemyShip.BeAttacked if (hp <= 0)的部分
            {
                try
                {
                    UIBattleStatistics.RegisterEliminate(ship.intensity); //记录某类型的敌人被摧毁
                    UIBattleStatistics.RegisterIntercept(ship); //记录拦截距离
                    // relic 0-2 新版女神之泪充能效果
                    if (Relic.HaveRelic(0, 2) && Relic.relic0_2Version == 1 && Relic.relic0_2CanActivate >= 1 && Relic.relic0_2Charge < Relic.relic0_2MaxCharge && DSP_Battle.Configs.nextWaveElite <= 0)
                    {
                        Interlocked.Add(ref Relic.relic0_2Charge, 1);
                        UIRelic.RefreshTearOfGoddessSlotTips();
                    }
                    Rank.AddExp(ship.intensity * 10); //获得经验

                    double dropExpectation = ship.intensity * 1.0 / DSP_Battle.Configs.nextWaveIntensity * DSP_Battle.Configs.nextWaveMatrixExpectation;
                    if (UIBattleStatistics.alienMatrixGain >= DSP_Battle.Configs.nextWaveMatrixExpectation) dropExpectation *= 0.1; // 对于精英波次，如果已经获得了等同于期望以上的矩阵，接下来的获得量只有10%。可以通过存读档刷新，但是不改了，就好像sl玩家想这样就这样吧
                    int dropItemId = 8032;
                    if (Relic.HaveRelic(3, 1)) dropExpectation *= 1.3; // relic1-3 窃法之刃获得额外掉落
                    if (GameMain.history.TechUnlocked(1924)) // 由于异星矩阵有用，用于随机遗物，所以这里改了
                    {
                        //dropExpectation *= 50;
                        //dropItemId = 8033;
                    }
                    if (dropExpectation > 1) //期望超过1的部分必然掉落
                    {
                        int guaranteed = (int)dropExpectation;
                        dropExpectation -= guaranteed;

                        GameMain.mainPlayer.TryAddItemToPackage(dropItemId, guaranteed, 0, true);
                        Utils.UIItemUp(dropItemId, guaranteed, 180);
                        UIBattleStatistics.RegisterAlienMatrixGain(guaranteed);
                    }
                    if (Utils.RandDouble() < dropExpectation) //根据概率决定是否掉落
                    {
                        GameMain.mainPlayer.TryAddItemToPackage(dropItemId, 1, 0, true);
                        Utils.UIItemUp(dropItemId, 1, 180);
                        UIBattleStatistics.RegisterAlienMatrixGain(1);
                    }
                    //relic 0-10 水滴击杀加伤害 MOD:(水滴在客戶端純粹動畫效果, 因此不改)
                    // relic 2-14 每次击杀有概率获得黑棒或者翘曲器 概率为（5+0.1*舰船强度）%
                    if (Relic.HaveRelic(2, 14) && Relic.Verify(0.05 + 0.001 * ship.intensity))
                    {
                        if (Utils.RandInt(0, 2) == 0)
                        {
                            GameMain.mainPlayer.TryAddItemToPackage(1803, 1, 0, true);
                            Utils.UIItemUp(1803, 1, 200);
                        }
                        else
                        {
                            GameMain.mainPlayer.TryAddItemToPackage(1210, 1, 0, true);
                            Utils.UIItemUp(1210, 1, 200);
                        }
                    }
                    // relic3-3 掘墓人击杀敌舰给沙子
                    if (Relic.HaveRelic(3, 3))
                    {
                        GameMain.mainPlayer.SetSandCount(GameMain.mainPlayer.sandCount + 500 * (int)Math.Sqrt(ship.intensity));
                    }

                    // relic0-0吞噬者效果
                    if (Relic.HaveRelic(0, 0))
                    {
                        Relic.AutoBuildMegaStructure(-1, 12 * ship.intensity);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn("AfterShipDestoryed error!");
                    Log.Warn(e);
                }
            }

            #endregion


            #region Sync starFortress

            [HarmonyPrefix, HarmonyPatch(typeof(UIStarFortress), nameof(UIStarFortress.SetModuleNum))]
            static void SetModuleNum_Prefix(int index, ref int __state)
            {
                if (UIStarFortress.curDysonSphere == null) return;
                int starIndex = UIStarFortress.curDysonSphere.starData.index;
                if (starIndex < StarFortress.moduleMaxCount.Count && index < StarFortress.moduleMaxCount[starIndex].Count)
                {
                    __state = StarFortress.moduleMaxCount[starIndex][index];
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(UIStarFortress), nameof(UIStarFortress.SetModuleNum))]
            static void SetModuleNum_Prefix(int index, int __state)
            {
                if (UIStarFortress.curDysonSphere == null) return;
                int starIndex = UIStarFortress.curDysonSphere.starData.index;
                if (starIndex < StarFortress.moduleMaxCount.Count && index < StarFortress.moduleMaxCount[starIndex].Count)
                {
                    int value = StarFortress.moduleMaxCount[starIndex][index];
                    if (value != __state && NebulaModAPI.IsMultiplayerActive)
                    {
                        // 在moduleMaxCount的值更改後廣播修改後的值
                        // TODO: 同步拆除的確認窗口
                        SendStarFortressSetModuleNum(starIndex, index, value);
                    }
                }
            }

            static void SendStarFortressSetModuleNum(int starIndex, int index, int value)
            {
                using var p = NebulaModAPI.GetBinaryWriter();
                var w = p.BinaryWriter;
                w.Write(starIndex);
                w.Write(index);
                w.Write(value);

                var packet = new NC_BattleEvent(NC_BattleEvent.EType.StarFortressSetModuleNum, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, p.CloseAndGetBytes());
                NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
            }

            public static void SyncStarFortressSetModuleNum(BinaryReader r)
            {
                int starIndex = r.ReadInt32();
                int index = r.ReadInt32();
                int value = r.ReadInt32();
                if (starIndex < StarFortress.moduleMaxCount.Count && index < StarFortress.moduleMaxCount[starIndex].Count)
                {
                    StarFortress.moduleMaxCount[starIndex][index] = value;
                    Log.Debug($"StarFortressSetModuleNum : [{starIndex}][{starIndex}] = {value}");

                    if (starIndex < GameMain.data.dysonSpheres.Length)
                    {
                        var curDysonSphere = GameMain.data.dysonSpheres[starIndex];
                        StarFortress.ReCalcData(ref curDysonSphere);
                        UIStarFortress.RefreshAll();
                    }
                }
            }

            #endregion

            #region Droplet 遠程水滴 + 機制修改

            static int lastWaveStarIndex;

            [HarmonyPrefix, HarmonyPatch(typeof(RendererSphere), "RSphereGameTick")]
            static bool RSphereGameTick(long time)
            {
                if (RendererSphere.enemySpheres.Count <= 0) RendererSphere.InitAll();
                if (DSP_Battle.Configs.nextWaveState == 3)
                    lastWaveStarIndex = DSP_Battle.Configs.nextWaveStarIndex;
                else if (DSP_Battle.Configs.nextWaveState != 0)
                    lastWaveStarIndex = -1;

                if (lastWaveStarIndex >= 0)
                {
                    RendererSphere.enemySpheres[lastWaveStarIndex].swarm.GameTick(time); //維持原邏輯
                    RendererSphere.dropletSpheres[lastWaveStarIndex].swarm.GameTick(time); //計算遠程水滴
                    //Log.SlowLog(lastWaveStarIndex);
                }
                return false;
            }

            [HarmonyPostfix, HarmonyPatch(typeof(StarmapCamera), "OnPostRender")]
            static void DrawPatch2(StarmapCamera __instance)
            {
                if (__instance.uiStarmap.viewStarSystem != null && !UIStarmap.isChangingToMilkyWay && DysonSphere.renderPlace == ERenderPlace.Starmap)
                {
                    RendererSphere.dropletSpheres[__instance.uiStarmap.viewStarSystem.index].DrawPost();
                }
            }


            static ushort[] dropletOwners = new ushort[21];

            [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
            public static void OnGameBegin()
            {                
                dropletOwners = new ushort[Droplets.dropletPool.Length]; //重置水滴擁有者
            }

            [HarmonyPostfix, HarmonyPatch(typeof(Droplet), nameof(Droplet.Launch))]
            static void Droplet_Launch_Postfix(Droplet __instance, bool __result)
            {
                if (NebulaModAPI.IsMultiplayerActive && __result) // 成功發射時, 廣播給其他玩家
                {
                    var packet = new NC_BattleEvent(NC_BattleEvent.EType.DropletLaunch, NebulaModAPI.MultiplayerSession.LocalPlayer.Id, new byte[0]);
                    NebulaModAPI.MultiplayerSession.Network.SendPacket(packet);
                    dropletOwners[__instance.dropletIndex] = 0; // 本地id = 0
                    Log.Debug($"Droplet[{__instance.dropletIndex}] launch from local");
                }
            }

            public static void SyncDropletLaunch(ushort playerId)
            {
                Log.Dev($"Recv droplet launch from remote{playerId}");
                for (int i = 0; i < Droplets.maxDroplet; i++)
                {                    
                    if (Droplets.dropletPool[i].state <= 0) // 找到一個空的或待命的水滴並嘗試發射
                    {
                        Droplet droplet = Droplets.dropletPool[i];
                        droplet.swarmIndex = DSP_Battle.Configs.nextWaveStarIndex;
                        if (DropletRemote_CreateBulltes(playerId, droplet))
                        {
                            droplet.state = 1;//起飞
                            dropletOwners[i] = playerId; // 其他人的id從1開始
                            ShowMessageInChat(string.Format("Player {0} launch droplet[{1}]", playerId, i));
                            Log.Debug($"Droplet[{i}] launch from remote[{playerId}]");
                            return;
                        }
                        else //没创建成功，很可能是因为swarm为null
                        {
                            continue;
                        }
                    }
                }
                Log.Warn($"Droplet launch from remote[{playerId}]: N/A");
            }

            static bool TryGetMechaPositions(ushort playerId, ref int localPlanetId, ref VectorLF3 UPos)
            {
                // player.Movement.rootTransform.localPosition的值不太對, 暫且忽略當地座標處理
                using (Multiplayer.Session.World.GetRemotePlayersModels(out var remotePlayersModels))
                {
                    if (remotePlayersModels.TryGetValue(playerId, out var player))
                    {
                        localPlanetId = player.Movement.localPlanetId;
                        UPos = player.Movement.absolutePosition;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            static bool DropletRemote_CreateBulltes(ushort playerId, Droplet droplet)
            {
                DysonSwarm swarm = droplet.GetSwarm();
                if (swarm == null) return false;

                int planetId = -1;
                VectorLF3 beginUPos = VectorLF3.zero; // 以遠端玩家的uPostion當作水滴起始位置
                Vector3 beginLPos = Vector3.zero;
                if (!TryGetMechaPositions(playerId, ref planetId, ref beginUPos))
                {
                    Log.Warn($"Can't find player{playerId} in DropletRemote_CreateBulltes");
                    return false;
                }
                VectorLF3 endUPos = beginUPos + VectorLF3.one * 300; //遠端玩家在太空時, 隨便挑一個方向發射
                if (planetId != -1) //遠端玩家在星球上時, 以法向量的方向發射
                {
                    VectorLF3 local = (beginUPos - GameMain.galaxy.astrosData[planetId].uPos);
                    if (local.normalized != VectorLF3.zero) 
                    {
                        endUPos = beginUPos + local.normalized * 300;
                        beginLPos = local; 
                    }
                }

                float newMaxt = (float)((endUPos - beginUPos).magnitude / DSP_Battle.Configs.dropletSpd * 200);
                for (int i = 0; i < droplet.bulletIds.Length; i++)
                {
                    if (i > 0) //起飞阶段只渲染一个
                    {
                        beginUPos = new VectorLF3(0, 0, 0);
                        endUPos = new VectorLF3(1, 2, 3);
                        beginLPos = new Vector3(9999, 9998, 9997);
                    }
                    droplet.bulletIds[i] = swarm.AddBullet(new SailBullet
                    {
                        maxt = newMaxt,
                        lBegin = beginLPos,
                        uEndVel = new Vector3(1, 1, 1),
                        uBegin = beginUPos, //起飞过程不加random
                        uEnd = endUPos
                    }, 1);
                    swarm.bulletPool[droplet.bulletIds[i]].state = 0;
                }
                return true;
            }

            [HarmonyTranspiler, HarmonyPatch(typeof(Droplets), nameof(Droplets.GameData_GameTick))]
            static IEnumerable<CodeInstruction> Droplets_GameData_GameTick_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                try
                {
                    // replace : Droplets.dropletPool[j].Update(true);
                    // to      : DropletRemote_Update(Droplets.dropletPool[j], true);
                    var codeMatcher = new CodeMatcher(instructions)
                        .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "Update"))
                        .Repeat(matcher => {
                            matcher.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(Warper), nameof(DropletRemote_Update)));
                        });
                    return codeMatcher.InstructionEnumeration();
                }
                catch (Exception e)
                {
                    Log.Warn("Droplets_GameData_GameTick_Transpiler fail!");
                    Log.Dev(e);
                    return instructions;
                }
            }

            public static void DropletRemote_Update(Droplet droplet, bool _) //新的Update, 允許遠端水滴 + 待命狀態
            {
                const float tickT = 0.016666668f;
                if (droplet.state <= 0) return;
                if (droplet.swarmIndex < 0)
                {
                    droplet.state = 0;
                    return;
                }
                DysonSwarm swarm = droplet.GetSwarm();

                if (swarm == null)
                {
                    droplet.state = 0;
                    return;
                }
                if (swarm.bulletPool.Length <= droplet.bulletIds[0])
                {
                    droplet.state = 0;
                    return;
                }

                if (DSP_Battle.Configs.nextWaveState == 1) //在和平階段,強制回收水滴
                {
                    for (int i = 0; i < droplet.bulletIds.Length; i++)
                    {
                        if (swarm.bulletPool.Length <= droplet.bulletIds[i])
                            continue;
                        swarm.RemoveBullet(droplet.bulletIds[i]);
                    }
                    droplet.swarmIndex = -1;
                    droplet.state = 0; //水滴數量共享
                    Log.Debug($"Droplet[{droplet.dropletIndex}]: teleport to mecha[{dropletOwners[droplet.dropletIndex]}]");
                    return;
                }

                if (dropletOwners[droplet.dropletIndex] == 0 && droplet.state <= 3 ) // (MOD)本地發出的水滴且追敵中才会消耗能量
                {
                    Droplets.ForceConsumeMechaEnergy(Droplets.energyConsumptionPerTick);
                }

                if (droplet.state == 1) //刚起飞
                {
                    float lastT = swarm.bulletPool[droplet.bulletIds[0]].t;
                    float lastMaxt = swarm.bulletPool[droplet.bulletIds[0]].maxt;
                    if (lastMaxt - lastT <= 0.035f) //进入太空索敌阶段
                    {
                        droplet.state = 2; //不需要在此刷新当前位置，因为state2每帧开头都刷新
                    }

                }
                else if (droplet.state == 2) //追敌中
                {
                    //如果原目标不存在了，尝试寻找新目标，如果找不到目标，设定为回机甲状态（4）
                    if (!EnemyShips.ships.ContainsKey(droplet.targetShipIndex) || EnemyShips.ships[droplet.targetShipIndex].state != EnemyShip.State.active)
                    {
                        if (!droplet.FindNextNearTarget()) //重新尋找目標
                        {
                            DropletRemote_Return(droplet);
                            return;
                        }
                    }
                    
                    VectorLF3 enemyUPos = EnemyShips.ships[droplet.targetShipIndex].uPos + Utils.RandPosDelta(droplet.randSeed) * 200f;
                    VectorLF3 newBegin = droplet.GetCurrentUPos();
                    VectorLF3 newEnd = (enemyUPos - newBegin).normalized * Droplet.exceedDis + enemyUPos;
                    float newMaxt = (float)((newEnd - newBegin).magnitude / DSP_Battle.Configs.dropletSpd);
                    double realSpd = DSP_Battle.Configs.dropletSpd;
                    if (Rank.rank >= 8 || DSP_Battle.Configs.developerMode) // 水滴快速接近
                    {
                        double warpRushDist = (enemyUPos - newBegin).magnitude - Droplet.exceedDis;
                        if (warpRushDist > Droplets.warpRushDistThr && Droplets.warpRushCharge[droplet.dropletIndex] >= Droplets.warpRushNeed)
                        {
                            Droplets.warpRushCharge[droplet.dropletIndex] = -5;
                            realSpd = 12 * warpRushDist;
                        }
                        else if (Droplets.warpRushCharge[droplet.dropletIndex] < 0)
                        {
                            int phase = -Droplets.warpRushCharge[droplet.dropletIndex];
                            realSpd = 60 / phase * warpRushDist;
                        }
                    }
                    droplet.RetargetAllBullet(newBegin, newEnd, droplet.bulletIds.Length, Droplet.maxPosDelta, Droplet.maxPosDelta, realSpd);
                    //判断击中，如果距离过近
                    if ((newBegin - enemyUPos).magnitude < 500 || newMaxt <= Droplet.exceedDis * 1.0 / DSP_Battle.Configs.dropletSpd + 0.035f)
                    {
                        int damage = DSP_Battle.Configs.dropletAtk;
                        if (!NC_Patch.IsClient) // (MOD)不是聯機客戶端才計算傷害
                        {
                            if (Rank.rank >= 10) damage = 5 * DSP_Battle.Configs.dropletAtk;
                            if (Relic.HaveRelic(0, 10))
                            {
                                damage += (Relic.BonusDamage(Droplets.bonusDamage, 1) - Droplets.bonusDamage);
                                UIBattleStatistics.RegisterDropletAttack(EnemyShips.ships[droplet.targetShipIndex].BeAttacked(damage, DamageType.droplet, true));
                            }
                            else
                            {
                                UIBattleStatistics.RegisterDropletAttack(EnemyShips.ships[droplet.targetShipIndex].BeAttacked(damage, DamageType.droplet));
                            }
                        }
                        droplet.state = 3; //击中后继续冲过目标，准备转向的阶段
                    }
                }
                else if (droplet.state == 3) //刚刚击中敌船，正准备转向
                {
                    float lastT = swarm.bulletPool[droplet.bulletIds[0]].t;
                    float lastMaxt = swarm.bulletPool[droplet.bulletIds[0]].maxt;
                    VectorLF3 newBegin = droplet.GetCurrentUPos();
                    if (lastMaxt - lastT <= 0.035) //到头了，执行转向/重新索敌
                    {
                        bool continueAttack = false;
                        if (EnemyShips.ships.ContainsKey(droplet.targetShipIndex) && EnemyShips.ships[droplet.targetShipIndex].state == EnemyShip.State.active)
                            continueAttack = true;
                        else if (droplet.FindNextTarget())
                            continueAttack = true;

                        if (continueAttack)
                        {
                            droplet.FindNextNearTarget(); //寻找新的较近的敌人
                            droplet.randSeed = Utils.RandNext(); //改变索敌定位时的随机偏移种子
                            droplet.state = 2; //回到追敌攻击状态
                            //VectorLF3 uEnd = swarm.bulletPool[droplet.bulletIds[0]].uEnd;
                            VectorLF3 enemyUPos = EnemyShips.ships[droplet.targetShipIndex].uPos + Utils.RandPosDelta(droplet.randSeed) * 200f;
                            VectorLF3 uEnd = (enemyUPos - newBegin).normalized * Droplet.exceedDis + enemyUPos;
                            droplet.RetargetAllBullet(newBegin, uEnd, droplet.bulletIds.Length, Droplet.maxPosDelta, Droplet.maxPosDelta, DSP_Battle.Configs.dropletSpd);
                        }
                        else
                        {
                            DropletRemote_Return(droplet);
                        }
                    }
                }
                else if (droplet.state == 4) //待命or回程 (MOD)只有當戰鬥結束後才會嘗試回收
                {
                    if (DSP_Battle.Configs.nextWaveState == 3) //戰鬥還沒結束前不回收
                    {
                        if (GameMain.gameTick % 60 == 0) //每秒重新索敵一次
                        {
                            droplet.state = 2;
                            return;
                        }

                        swarm.bulletPool[droplet.bulletIds[0]].t -= tickT; //進入原地待命狀態
                        VectorLF3 lastUPos = droplet.GetCurrentUPos();
                        for (int i = 0; i < droplet.bulletIds.Length; i++)
                        {
                            if (swarm.bulletPool.Length <= droplet.bulletIds[i]) continue;
                            swarm.bulletPool[droplet.bulletIds[i]].uBegin = lastUPos;
                            swarm.bulletPool[droplet.bulletIds[i]].t = 0;
                        }
                        return;
                    }

                    VectorLF3 mechaUPos = GameMain.mainPlayer.uPosition;
                    VectorLF3 mechaUPos2 = GameMain.mainPlayer.uPosition;
                    Vector3 meachLPos = GameMain.mainPlayer.position;
                    int mechaStarIndex = GameMain.localStar?.index ?? -1;
                    int planetId = GameMain.localPlanet?.id ?? -1;

                    if (dropletOwners[droplet.dropletIndex] != 0) //水滴為其他玩家發出
                    {
                        mechaStarIndex = -1; //找不到玩家時, 進入回收階段
                        if (TryGetMechaPositions(dropletOwners[droplet.dropletIndex], ref planetId, ref mechaUPos))
                        {
                            mechaUPos2 = mechaUPos;
                            mechaStarIndex = planetId / 100 > 0 ? planetId / 100 -1 : -1; //機甲不在星球上時, 進入回收階段
                        }
                    }
                    if (planetId != -1) //機甲在星球上
                    {                        
                        AstroData[] astroPoses = GameMain.galaxy.astrosData;
                        mechaUPos = astroPoses[planetId].uPos + Maths.QRotateLF(astroPoses[planetId].uRot, meachLPos);
                        mechaUPos2 = astroPoses[planetId].uPos + Maths.QRotateLF(astroPoses[planetId].uRot, meachLPos * 2);
                    }
                    
                    if (droplet.swarmIndex != mechaStarIndex)
                    {
                        droplet.state = 5; //如果水滴已经处在返回状态但是和机甲不在同一个星系，(MOD)進入回收階段
                    }
                    else
                    {
                        float lastT = swarm.bulletPool[droplet.bulletIds[0]].t;
                        float lastMaxt = swarm.bulletPool[droplet.bulletIds[0]].maxt;
                        VectorLF3 newBegin = droplet.GetCurrentUPos();
                        VectorLF3 newEnd = mechaUPos2;
                        //float newMaxt = (float)((uEnd - uBegin).magnitude / Configs.dropletSpd);

                        if (lastMaxt - lastT <= 0.05 || (newBegin - newEnd).magnitude < DSP_Battle.Configs.dropletSpd / 20f) //已经到机甲上方或者接近机甲
                        {
                            droplet.state = 5;
                        }

                        if (droplet.state == 5)
                        {
                            droplet.TryRemoveOtherBullets();
                            if (planetId != -1) newBegin = mechaUPos2; //如果是在星球上，则从上空(也就是mechUPos2)飞回来，否则从阶段拐点的原位置(也就是GetCurrentUPos())直线向机甲飞回来
                                                                       //RetargetAllBullet(newBegin, mechaUPos, 1, 0, 0, Configs.dropletSpd / 200.0);
                            swarm.bulletPool[droplet.bulletIds[0]].maxt = (float)((newBegin - mechaUPos).magnitude / (DSP_Battle.Configs.dropletSpd / 200.0));
                            swarm.bulletPool[droplet.bulletIds[0]].t = swarm.bulletPool[droplet.bulletIds[0]].maxt - tickT * 3;
                            swarm.bulletPool[droplet.bulletIds[0]].uEnd = newBegin;
                            swarm.bulletPool[droplet.bulletIds[0]].uBegin = mechaUPos;
                            swarm.bulletPool[droplet.bulletIds[0]].lBegin = GameMain.mainPlayer.position;
                        }
                        else
                        {
                            droplet.RetargetAllBullet(newBegin, mechaUPos2, droplet.bulletIds.Length, Droplet.maxPosDelta, Droplet.maxPosDelta, DSP_Battle.Configs.dropletSpd);
                        }
                    }
                }
                else if (droplet.state == 5) //回到机甲阶段 
                {
                    VectorLF3 mechaUPos = GameMain.mainPlayer.uPosition;
                    Vector3 mechaLPos = GameMain.mainPlayer.position;
                    int mechaStarIndex = GameMain.localStar?.index ?? -1;
                    int planetId = GameMain.localPlanet?.id ?? -1;

                    if (dropletOwners[droplet.dropletIndex] != 0) //水滴為其他玩家發出
                    {
                        mechaStarIndex = -1; //找不到遠端機甲時, 直接回收
                        if (TryGetMechaPositions(dropletOwners[droplet.dropletIndex], ref planetId, ref mechaUPos))
                        {
                            if (planetId != -1)
                            {
                                AstroData[] astroPoses = GameMain.galaxy.astrosData;
                                mechaUPos = astroPoses[planetId].uPos + Maths.QRotateLF(astroPoses[planetId].uRot, mechaLPos);
                            }
                            mechaStarIndex = planetId / 100 - 1; //遠端機甲不在星球上時, 直接回收
                        }
                    }
                    else if (planetId != -1) //水滴為本機玩家發出, 且在星球上
                    {                        
                        AstroData[] astroPoses = GameMain.galaxy.astrosData;
                        mechaUPos = astroPoses[planetId].uPos + Maths.QRotateLF(astroPoses[planetId].uRot, mechaLPos);
                    }

                    if (droplet.swarmIndex != mechaStarIndex) //如果水滴和機甲的星系不同, 直接回收
                    {
                        for (int i = 0; i < droplet.bulletIds.Length; i++)
                        {
                            if (swarm.bulletPool.Length <= droplet.bulletIds[i])
                                continue;
                            swarm.RemoveBullet(droplet.bulletIds[i]);
                        }
                        droplet.swarmIndex = -1;
                        droplet.state = 0; //水滴數量共享
                        Log.Debug($"Droplet[{droplet.dropletIndex}]: teleport to mecha[{dropletOwners[droplet.dropletIndex]}]");
                    }

                    swarm.bulletPool[droplet.bulletIds[0]].t -= tickT * 2;
                    float lastT = swarm.bulletPool[droplet.bulletIds[0]].t;
                    //float lastMaxt = swarm.bulletPool[bulletIds[0]].maxt;

                    if (lastT <= 0.03) //足够近，则回到机甲
                    {
                        droplet.state = 0; //水滴數量共享
                        droplet.TryRemoveOtherBullets(0);
                        droplet.swarmIndex = -1;
                        Log.Debug($"Droplet[{droplet.dropletIndex}]: return to mecha[{dropletOwners[droplet.dropletIndex]}]");
                    }
                    else //否则持续更新目标点为机甲位置
                    {
                        for (int i = 0; i < 1; i++)
                        {
                            if (swarm.bulletPool.Length <= droplet.bulletIds[i]) continue;
                            swarm.bulletPool[droplet.bulletIds[i]].uBegin = mechaUPos;
                            swarm.bulletPool[droplet.bulletIds[0]].lBegin = mechaLPos;
                        }
                    }
                }
            }

            static void DropletRemote_Return(Droplet droplet)
            {
                droplet.state = 4;
                VectorLF3 mechaUPos = GameMain.mainPlayer.uPosition;
                Vector3 mechaLPos = GameMain.mainPlayer.position;
                int planetId = GameMain.localPlanet?.id ?? -1;
                VectorLF3 newBegin = droplet.GetCurrentUPos();
                VectorLF3 newEnd = mechaUPos;
                
                if (dropletOwners[droplet.dropletIndex] != 0)
                {
                    if (TryGetMechaPositions(dropletOwners[droplet.dropletIndex], ref planetId, ref mechaUPos))
                    {
                        newEnd = mechaUPos;
                    }
                }
                else if (planetId != -1) //如果玩家在星球上，水滴则不是直线往玩家身上飞，而是飞到玩家头顶星球上空，然后再飞回玩家（这是在state=5阶段）
                {
                    AstroData[] astroPoses = GameMain.galaxy.astrosData;
                    newEnd = astroPoses[planetId].uPos + Maths.QRotateLF(astroPoses[planetId].uRot, mechaLPos);
                }

                droplet.RetargetAllBullet(newBegin, newEnd, droplet.bulletIds.Length, Droplet.maxPosDelta, Droplet.maxPosDelta, DSP_Battle.Configs.dropletSpd);
            }

            public static void RecyleDroplets()
            {
                for (int i = 0; i < Droplets.maxDroplet; i++)
                {
                    var droplet = Droplets.dropletPool[i];
                    if (droplet.state > 0)
                    {
                        var swarm = droplet.GetSwarm();
                        if (swarm != null)
                        {
                            for (int j = 0; j < droplet.bulletIds.Length; j++)
                            {
                                if (swarm.bulletPool.Length <= droplet.bulletIds[j])
                                    continue;
                                swarm.RemoveBullet(droplet.bulletIds[j]);
                            }
                        }
                        droplet.swarmIndex = -1;
                        droplet.state = 0;
                        Log.Debug($"Droplet[{droplet.dropletIndex}]: force recycle to mecha[{dropletOwners[droplet.dropletIndex]}]");
                    }
                }
            }


            #endregion

            #region EnemyShips.sortedShips TempFix

            static int errorCount = 0;
            [HarmonyFinalizer, HarmonyPatch(typeof(EnemyShips), nameof(EnemyShips.sortedShips))]
            static Exception SortedShips(Exception __exception, ref List<EnemyShip> __result)
            {
                if (__exception != null)
                {
                    // 在報錯時取消exception, 並回傳一個數量為0的list以讓流程繼續進行
                    if (errorCount++ < 10)
                        Log.Warn(__exception);
                    __result = new List<EnemyShip>();
                }
                return null;
            }

            #endregion
        }

        public static class Warper_RemoteBuildings
        {
            // Remote buildings : shadow ejector/silo 遠端建築: 影子砲塔

            public static void Export(BinaryWriter w)
            {
                int count = 0;
                foreach (var planet in GameMain.galaxy.stars[DSP_Battle.Configs.nextWaveStarIndex].planets)
                {
                    if (planet.factory == null) continue;
                    w.Write(planet.id);

                    var entityPool = planet.factory.entityPool;
                    var factorySystem = planet.factory.factorySystem;

                    var ejectorPool = factorySystem.ejectorPool;
                    for (int i = 1; i < factorySystem.ejectorCursor; i++)
                    {
                        ref var ejector = ref ejectorPool[i];
                        if (ejector.id == 0) continue;

                        int protoId = entityPool[ejector.entityId].protoId;
                        if (protoId == 2311) continue; // 不收錄原版彈射器

                        // 記錄下EjectorPatch會用到的參數
                        w.Write(protoId);
                        w.Write(ejector.entityId);
                        w.Write(ejector.bulletId);
                        w.Write(ejector.bulletCount);
                        w.Write(ejector.bulletInc);
                        w.Write(ejector.chargeSpend);
                        w.Write(ejector.coldSpend);
                        w.Write(ejector.localPosN.x);
                        w.Write(ejector.localPosN.y);
                        w.Write(ejector.localPosN.z);
                        count++;
                    }
                    w.Write(-1);

                    var siloPool = factorySystem.siloPool;
                    for (int i = 1; i < factorySystem.siloCursor; i++)
                    {
                        ref var silo = ref siloPool[i];
                        if (silo.id == 0) continue;

                        int protoId = entityPool[silo.entityId].protoId;
                        if (protoId == 2312 || protoId == 8036) continue; // 不收錄原版彈射器

                        // 記錄下SiloPatch會用到的參數
                        w.Write(protoId);
                        w.Write(silo.bulletId);
                        w.Write(silo.bulletCount);
                        w.Write(silo.bulletInc);
                        w.Write(silo.chargeSpend);
                        w.Write(silo.coldSpend);
                        w.Write(silo.localPos.x);
                        w.Write(silo.localPos.y);
                        w.Write(silo.localPos.z);
                        count++;
                    }
                    w.Write(-1);
                }
                w.Write(-1);
                Log.Info("RemoteBuildings: Export buildings " + count);
            }

            static int ejectorCursor = 0;
            static EjectorComponent[] ejectorPool = new EjectorComponent[32];
            static int siloCursor = 0;
            static SiloComponent[] siloPool = new SiloComponent[32];

            public static void Import(BinaryReader r)
            {
                ejectorCursor = 1;
                siloCursor = 1;

                while (true)
                {
                    int planetId = r.ReadInt32();
                    if (planetId == -1) break;
                    bool hasFactory = GameMain.galaxy.PlanetById(planetId)?.factory != null;

                    while (true)
                    {
                        int protoId = r.ReadInt32();
                        if (protoId == -1) break;

                        if (ejectorCursor >= ejectorPool.Length)
                        {
                            EjectorComponent[] oldPool = ejectorPool;
                            ejectorPool = new EjectorComponent[oldPool.Length * 2];
                            Array.Copy(oldPool, siloPool, oldPool.Length);
                        }

                        ejectorPool[ejectorCursor].planetId = planetId;
                        ejectorPool[ejectorCursor].id = protoId; // Use id to store protoId
                        ejectorPool[ejectorCursor].entityId = r.ReadInt32(); // 分辨目標用
                        ejectorPool[ejectorCursor].bulletId = r.ReadInt32();
                        ejectorPool[ejectorCursor].bulletCount = r.ReadInt32();
                        ejectorPool[ejectorCursor].bulletInc = r.ReadInt32();
                        ejectorPool[ejectorCursor].chargeSpend = r.ReadInt32();
                        ejectorPool[ejectorCursor].coldSpend = r.ReadInt32();
                        ejectorPool[ejectorCursor].localPosN = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

                        if (!hasFactory) // 只有在工廠尚未載入時才加入影子建築
                        {
                            ejectorPool[ejectorCursor].localRot = Maths.SphericalRotation(ejectorPool[ejectorCursor].localPosN, 0f); // 以localPos猜測localRot
                            ++ejectorCursor;

                        }
                    }

                    while (true)
                    {
                        int protoId = r.ReadInt32();
                        if (protoId == -1) break;

                        if (siloCursor >= siloPool.Length)
                        {
                            SiloComponent[] oldPool = siloPool;
                            siloPool = new SiloComponent[oldPool.Length * 2];
                            Array.Copy(oldPool, siloPool, oldPool.Length);
                        }

                        siloPool[siloCursor].planetId = planetId;
                        siloPool[siloCursor].id = protoId; // Use id to store protoId
                        siloPool[siloCursor].bulletId = r.ReadInt32();
                        siloPool[siloCursor].bulletCount = r.ReadInt32();
                        siloPool[siloCursor].bulletInc = r.ReadInt32();
                        siloPool[siloCursor].chargeSpend = r.ReadInt32();
                        siloPool[siloCursor].coldSpend = r.ReadInt32();
                        siloPool[siloCursor].localPos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                        //Log.Debug($"[{siloCursor}] {protoId} : planet{siloPool[siloCursor].planetId} count{siloPool[siloCursor].bulletCount}");

                        if (!hasFactory)
                        {
                            siloPool[siloCursor].localRot = Maths.SphericalRotation(siloPool[siloCursor].localPos, 0f);
                            ++siloCursor;
                        }
                    }
                }
                Log.Info($"RemoteBuildings: Import ejector {ejectorCursor - 1} silo {siloCursor - 1}");
            }

            [HarmonyPostfix, HarmonyPatch(typeof(WorkerThreadExecutor), nameof(WorkerThreadExecutor.AssemblerPartExecute))]
            static void UpdateShadowBuildings(int ___usedThreadCnt, int ___curThreadIdx)
            {
                // 在客戶端更新影子建築, 製造子彈/火箭動畫。 假設: 1.電力全滿 2.子彈保持為全空/全滿
                if (!NC_Patch.IsClient) return;
                if (DSP_Battle.Configs.nextWaveState != 3) return; // 只在戰鬥階段時運行

                try
                {
                    int start, end;
                    int localplanetId = GameMain.localPlanet?.id ?? -1;
                    var dysonSphere = GameMain.data.dysonSpheres[DSP_Battle.Configs.nextWaveStarIndex];
                    var dysonSwarm = dysonSphere?.swarm;

                    if (dysonSwarm != null && WorkerThreadExecutor.CalculateMissionIndex(1, ejectorCursor - 1, ___usedThreadCnt, ___curThreadIdx, 4, out start, out end))
                    {
                        var astroPoses = GameMain.galaxy.astrosData;
                        for (int i = start; i < end; i++)
                        {
                            ref var ejector = ref ejectorPool[i];
                            if (ejector.planetId == localplanetId) continue; // 如果玩家已經登陸星球, 則跳過

                            int bulletCount = ejector.bulletCount;
                            EjectorPatch(ref ejector, dysonSwarm, astroPoses);
                            ejector.bulletCount = bulletCount; // 鎖定子彈數量
                        }
                    }

                    if (dysonSphere != null && WorkerThreadExecutor.CalculateMissionIndex(1, siloCursor - 1, ___usedThreadCnt, ___curThreadIdx, 4, out start, out end))
                    {
                        for (int i = start; i < end; i++)
                        {
                            ref var silo = ref siloPool[i];
                            if (silo.planetId == localplanetId) continue; // 如果玩家已經登陸星球, 則跳過

                            int bulletCount = silo.bulletCount;
                            SiloPatch(ref silo, dysonSphere);
                            silo.bulletCount = bulletCount; // 鎖定子彈數量
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Dev(e);
                }
            }

            public static bool SiloPatch(ref SiloComponent __instance, DysonSphere sphere)
            {
                float power = 1.0f; // 默認滿電狀態
                int planetId = __instance.planetId;
                int starIndex = planetId / 100 - 1;
                //int gmProtoId = __instance.id; // 使用id儲存

                if (DSP_Battle.Configs.developerMode)
                {
                    __instance.bulletId = 8006;
                    __instance.bulletCount = 99;
                }
                if (GameMain.instance.timei % 60 == 0 && __instance.bulletCount == 0)
                {
                    __instance.bulletId = MissileSilo.nextBulletId(__instance.bulletId);
                }

                if (__instance.fired && __instance.direction != -1)
                {
                    __instance.fired = false;
                }
                float num = (float)Cargo.accTableMilli[__instance.incLevel];
                int num2 = (int)(power * 10000f * (1f + num) + 0.1f);
                Mutex dysonSphere_mx = sphere.dysonSphere_mx;
                uint result;
                lock (dysonSphere_mx)
                {
                    //下面设定目标，发射时是选择最近目标；如果目标丢失则再随机选择目标
                    int targetIndex = 0;
                    //if (DSP_Battle.Configs.nextWaveState == 3) // MOD: 鎖定在戰鬥階段執行
                    {
                        targetIndex = MissileSilo.FindTarget(starIndex, planetId);
                    }

                    __instance.hasNode = (sphere.GetAutoNodeCount() > 0);
                    if (targetIndex <= 0)  //if (!__instance.hasNode) 原本是没有节点，因此不发射
                    {
                        __instance.autoIndex = 0;
                        if (__instance.direction == 1)
                        {
                            __instance.time = (int)((long)__instance.time * (long)__instance.coldSpend / (long)__instance.chargeSpend);
                            __instance.direction = -1;
                        }
                        if (__instance.direction == -1)
                        {
                            __instance.time -= num2;
                            if (__instance.time <= 0)
                            {
                                __instance.time = 0;
                                __instance.direction = 0;
                            }
                        }
                        if (power >= 0.1f)
                        {
                            result = 1U;
                        }
                        else
                        {
                            result = 0U;
                        }
                    }
                    else if (power < 0.1f)
                    {
                        if (__instance.direction == 1)
                        {
                            __instance.time = (int)((long)__instance.time * (long)__instance.coldSpend / (long)__instance.chargeSpend);
                            __instance.direction = -1;
                        }
                        result = 0U;
                    }
                    else
                    {
                        uint num3 = 0U;
                        bool flag2;
                        num3 = ((flag2 = (__instance.bulletCount > 0)) ? 3U : 2U);
                        if (__instance.direction == 1)
                        {
                            if (!flag2)
                            {
                                __instance.time = (int)((long)__instance.time * (long)__instance.coldSpend / (long)__instance.chargeSpend);
                                __instance.direction = -1;
                            }
                        }
                        else if (__instance.direction == 0 && flag2)
                        {
                            __instance.direction = 1;
                        }
                        if (__instance.direction == 1)
                        {
                            __instance.time += num2;
                            if (__instance.time >= __instance.chargeSpend)
                            {
                                AstroData[] astroPoses = sphere.starData.galaxy.astrosData;
                                __instance.fired = true;
                                //DysonNode autoDysonNode = sphere.GetAutoDysonNode(__instance.autoIndex + __instance.id); //原本获取目标节点，现在已不需要
                                DysonRocket dysonRocket = default(DysonRocket);
                                dysonRocket.planetId = __instance.planetId;
                                dysonRocket.uPos = astroPoses[__instance.planetId].uPos + Maths.QRotateLF(astroPoses[__instance.planetId].uRot, __instance.localPos + __instance.localPos.normalized * 6.1f);
                                dysonRocket.uRot = astroPoses[__instance.planetId].uRot * __instance.localRot * Quaternion.Euler(-90f, 0f, 0f);
                                dysonRocket.uVel = dysonRocket.uRot * Vector3.forward;
                                dysonRocket.uSpeed = 0f;
                                dysonRocket.launch = __instance.localPos.normalized;
                                //sphere.AddDysonRocket(dysonRocket, autoDysonNode); //原本
                                int rocketIndex = MissileSilo.AddDysonRockedGniMaerd(ref sphere, ref dysonRocket, null); //这是添加了一个目标戴森球节点为null的火箭，因此被判定为导弹

                                MissileSilo.MissileTargets[starIndex][rocketIndex] = targetIndex;
                                MissileSilo.missileProtoIds[starIndex][rocketIndex] = __instance.bulletId;
                                int damage = 0;
                                if (__instance.bulletId == 8004) damage = DSP_Battle.Configs.missile1Atk;
                                else if (__instance.bulletId == 8005) damage = DSP_Battle.Configs.missile2Atk;
                                else if (__instance.bulletId == 8006) damage = DSP_Battle.Configs.missile3Atk;
                                //注册导弹
                                UIBattleStatistics.RegisterShootOrLaunch(__instance.bulletId, damage);

                                __instance.autoIndex++;
                                if (!Relic.HaveRelic(1, 5))
                                {
                                    __instance.bulletInc -= __instance.bulletInc / __instance.bulletCount;
                                    __instance.bulletCount--;
                                }
                                if (__instance.bulletCount == 0)
                                {
                                    __instance.bulletInc = 0;
                                }
                                __instance.time = __instance.coldSpend;
                                __instance.direction = -1;
                            }
                        }
                        else if (__instance.direction == -1)
                        {
                            __instance.time -= num2;
                            if (__instance.time <= 0)
                            {
                                __instance.time = 0;
                                __instance.direction = (flag2 ? 1 : 0);
                            }
                        }
                        else
                        {
                            __instance.time = 0;
                        }
                        result = num3;
                    }
                }
                return false;
            }

            public static bool EjectorPatch(ref EjectorComponent __instance, DysonSwarm swarm, AstroData[] astroPoses)
            {
                float power = 1.0f;
                int gmProtoId = __instance.id;

                //子弹需求循环
                if (__instance.bulletCount == 0 && gmProtoId != 8014 && GameMain.instance.timei % 60 == 0)
                {
                    __instance.bulletId = Cannon.nextBulletId(__instance.bulletId);
                }
                else if (gmProtoId == 8014)
                {
                    __instance.bulletId = 8007;
                    __instance.bulletCount = 0;
                }

                __instance.targetState = EjectorComponent.ETargetState.None;

                //下面是因为 炮需要用orbitId记录索敌模式，而orbitId有可能超出已设定的轨道数，为了避免溢出，炮的orbitalId在参与计算时需要独立指定为1。
                //后续所有的__instance.orbitId都被替换为此
                if (__instance.orbitId <= 0 || __instance.orbitId > 4)
                {
                    __instance.orbitId = 1;
                }

                CannonFire(ref __instance, power, swarm, astroPoses, gmProtoId);
                return false;
            }

            private static uint CannonFire(ref EjectorComponent __instance, float power, DysonSwarm swarm, AstroData[] astroPoses, int gmProtoId)
            {
                int planetId = __instance.planetId;
                int entityId = __instance.entityId;
                int starIndex = planetId / 100 - 1;
                int calcOrbitId = __instance.orbitId;
                uint result = 0;

                float num2 = (float)Cargo.incTableMilli[__instance.incLevel];
                int num3 = (int)(power * 10000f * (1f + num2) + 0.1f);

                bool relic0_6Activated = Relic.HaveRelic(0, 6) && __instance.bulletId == 8001; // relic0-6 京级巨炮如果激活并且当前子弹确实是穿甲磁轨弹
                if (relic0_6Activated) num3 = (int)(num3 * 0.1);

                __instance.targetState = EjectorComponent.ETargetState.OK;
                bool flag = true;
                int num4 = __instance.planetId / 100 * 100;
                float num5 = __instance.localAlt + __instance.pivotY + (__instance.muzzleY - __instance.pivotY) / Mathf.Max(0.1f, Mathf.Sqrt(1f - __instance.localDir.y * __instance.localDir.y));
                Vector3 vector = new Vector3(__instance.localPosN.x * num5, __instance.localPosN.y * num5, __instance.localPosN.z * num5);
                VectorLF3 vectorLF = astroPoses[__instance.planetId].uPos + Maths.QRotateLF(astroPoses[__instance.planetId].uRot, vector);
                Quaternion q = astroPoses[__instance.planetId].uRot * __instance.localRot;
                VectorLF3 uPos = astroPoses[num4].uPos;
                VectorLF3 b = uPos - vectorLF;

                List<EnemyShip> sortedShips = EnemyShips.sortedShips(calcOrbitId, starIndex, __instance.planetId);

                //下面的参数根据是否是炮还是太阳帆的弹射器有不同的修改
                double maxtDivisor = 5000.0; //决定子弹速度
                int damage = 0;
                int loopNum = sortedShips.Count;
                double cannonSpeedScale = 1;
                if (gmProtoId == 8012)
                    cannonSpeedScale = 2;
                EnemyShip curTarget = null;

                if (__instance.bulletId == 8001)
                {
                    maxtDivisor = relic0_6Activated ? DSP_Battle.Configs.bullet4Speed : DSP_Battle.Configs.bullet1Speed * cannonSpeedScale; // relic0-6京级巨炮 还会大大加速此子弹速度
                    damage = (int)DSP_Battle.Configs.bullet1Atk; //只有这个子弹能够因为引力弹射器而强化伤害。这个强化是不是取消了？
                                                      //if (relic0_6Activated) // relic0-6 京级巨炮效果，由于这个伤害只在统计中计算为发射伤害，实际造成上海市还要再重新计算，因此这里不计算了，统计中记为发射了基础的伤害
                                                      //    damage = Relic.BonusDamage(damage, 500); 
                }
                else if (__instance.bulletId == 8002)
                {
                    maxtDivisor = DSP_Battle.Configs.bullet2Speed * cannonSpeedScale;
                    damage = DSP_Battle.Configs.bullet2Atk;
                }
                else if (__instance.bulletId == 8003)
                {
                    maxtDivisor = DSP_Battle.Configs.bullet3Speed * cannonSpeedScale;
                    damage = DSP_Battle.Configs.bullet3Atk;
                }
                else if (__instance.bulletId == 8007)
                {
                    maxtDivisor = DSP_Battle.Configs.bullet4Speed; //没有速度加成
                    damage = DSP_Battle.Configs.bullet4Atk;
                }

                //不该参与循环的部分，换到循环前了

                bool flag2 = __instance.bulletCount > 0;
                if (gmProtoId == 8014) //脉冲炮不需要子弹
                    flag2 = true;
                VectorLF3 vectorLF2 = VectorLF3.zero;

                int begins = Cannon.indexBegins;
                if (begins >= loopNum)
                {
                    Interlocked.Exchange(ref Cannon.indexBegins, 0);
                    begins = 0;
                }

                bool needFindNewTarget = true;
                EnemyShip lastTargetShip = null;
                if (Cannon.cannonTargets.ContainsKey(planetId))
                {
                    if (Cannon.cannonTargets[planetId].ContainsKey(entityId))
                    {
                        int lastTargetShipIndex = Cannon.cannonTargets[planetId][entityId];
                        if (EnemyShips.ships.ContainsKey(lastTargetShipIndex) && EnemyShips.ships[lastTargetShipIndex].state == EnemyShip.State.active)
                        {
                            lastTargetShip = EnemyShips.ships[lastTargetShipIndex];
                            needFindNewTarget = false; // 老目标存在的话，在下面的循环中首先判断老目标是否合法（不被阻挡、俯仰角合适等）
                        }
                    }
                }
                for (int gm = begins; gm < loopNum && gm < begins + 3; gm++)
                {
                    if (!needFindNewTarget && gm > begins) // 说明上一个循环判定了原目标，且原目标无法作为合法目标，因此重新开始判定目标
                    {
                        needFindNewTarget = true;
                        gm = begins;
                    }

                    //新增的，每次循环开始必须重置
                    __instance.targetState = EjectorComponent.ETargetState.OK;
                    flag = true;
                    flag2 = __instance.bulletCount > 0;
                    if (gmProtoId == 8014) //脉冲炮不需要子弹
                        flag2 = true;
                    else if (relic0_6Activated) // relic0-6 京级巨炮效果 每次消耗五发弹药
                        flag2 = __instance.bulletCount > 4;

                    int shipIdx = 0;//ship总表中的唯一标识：index
                    EnemyShip targetShip = sortedShips[gm];
                    if (!needFindNewTarget) // 如果原本的目标存在，则先判断原本的目标，此时在这个循环中，gm=begins的ship并没有真的被计算俯仰角等合法性判断，因此假若原本的目标失效，进入了下个循环后要根据情况重置gm=begins（见循环节开头）
                    {
                        targetShip = lastTargetShip;
                    }
                    vectorLF2 = targetShip.uPos;
                    shipIdx = targetShip.shipIndex;

                    if (needFindNewTarget && (!EnemyShips.ships.ContainsKey(shipIdx) || targetShip.state != EnemyShip.State.active)) continue;


                    VectorLF3 vectorLF3 = vectorLF2 - vectorLF;
                    __instance.targetDist = vectorLF3.magnitude;
                    vectorLF3.x /= __instance.targetDist;
                    vectorLF3.y /= __instance.targetDist;
                    vectorLF3.z /= __instance.targetDist;
                    Vector3 vector2 = Maths.QInvRotate(q, vectorLF3);
                    __instance.localDir.x = __instance.localDir.x * 0.9f + vector2.x * 0.1f;
                    __instance.localDir.y = __instance.localDir.y * 0.9f + vector2.y * 0.1f;
                    __instance.localDir.z = __instance.localDir.z * 0.9f + vector2.z * 0.1f;
                    if ((double)vector2.y < 0.08715574 || vector2.y > 0.8660254f)
                    {
                        __instance.targetState = EjectorComponent.ETargetState.AngleLimit;
                        flag = false;
                    }
                    if (flag2 && flag)
                    {
                        for (int i = num4 + 1; i <= __instance.planetId + 2; i++)
                        {
                            if (i != __instance.planetId)
                            {
                                double num6 = (double)astroPoses[i].uRadius;
                                if (num6 > 1.0)
                                {
                                    VectorLF3 vectorLF4 = astroPoses[i].uPos - vectorLF;
                                    double num7 = vectorLF4.x * vectorLF4.x + vectorLF4.y * vectorLF4.y + vectorLF4.z * vectorLF4.z;
                                    double num8 = vectorLF4.x * vectorLF3.x + vectorLF4.y * vectorLF3.y + vectorLF4.z * vectorLF3.z;
                                    if (num8 > 0.0)
                                    {
                                        double num9 = num7 - num8 * num8;
                                        num6 += 120.0;
                                        if (num9 < num6 * num6)
                                        {
                                            flag = false;
                                            __instance.targetState = EjectorComponent.ETargetState.Blocked;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (EnemyShips.ships.ContainsKey(shipIdx) && EnemyShips.ships[shipIdx].state == EnemyShip.State.active && __instance.targetState != EjectorComponent.ETargetState.Blocked && __instance.targetState != EjectorComponent.ETargetState.AngleLimit)
                    {
                        curTarget = EnemyShips.ships[shipIdx]; //设定目标
                        if (!Cannon.cannonTargets.ContainsKey(planetId))
                        {
                            Cannon.cannonTargets.TryAdd(planetId, new ConcurrentDictionary<int, int>());
                            Cannon.cannonTargets[planetId].TryAdd(entityId, shipIdx);
                        }
                        else
                        {
                            Cannon.cannonTargets[planetId].AddOrUpdate(entityId, shipIdx, (x, y) => shipIdx);
                        }
                        if (EjectorUIPatch.needToRefreshTarget) //如果需要刷新目标
                        {
                            if (EjectorUIPatch.curEjectorPlanetId == __instance.planetId && EjectorUIPatch.curEjectorEntityId == __instance.entityId)
                            {
                                EjectorUIPatch.curTarget = curTarget;
                            }
                        }
                        break;
                    }
                }
                //如果没有船/船没血了，就不打炮了
                if (curTarget == null)
                {
                    Interlocked.Add(ref Cannon.indexBegins, 3);
                    flag = false; //本身是由于俯仰限制或路径被阻挡的判断，现在找不到目标而不打炮也算做里面
                }
                else if (curTarget != null && curTarget.hp <= 0)
                {
                    flag = false;
                }
                else if (curTarget.state != EnemyShip.State.active)
                {
                    flag = false;
                }

                bool flag3 = flag && flag2;
                result = (flag2 ? (flag ? 4U : 3U) : 2U);
                if (__instance.direction == 1)
                {
                    if (!flag3)
                    {
                        __instance.time = (int)((long)__instance.time * (long)__instance.coldSpend / (long)__instance.chargeSpend);
                        __instance.direction = -1;
                    }
                }
                else if (__instance.direction == 0 && flag3)
                {
                    __instance.direction = 1;
                }


                if (__instance.direction == 1)
                {
                    __instance.time += num3;
                    if (__instance.time >= __instance.chargeSpend)
                    {
                        __instance.fired = true;
                        VectorLF3 uBeginChange = vectorLF;
                        int bulletCost = relic0_6Activated ? 5 : 1;

                        int bulletIndex = -1;

                        if (gmProtoId != 8014 || GameMain.instance.timei % 5 == 1) // 相位炮五帧才发一个，但是伤害x5
                        {
                            //下面是添加子弹
                            bulletIndex = swarm.AddBullet(new SailBullet
                            {
                                maxt = (float)(__instance.targetDist / maxtDivisor),
                                lBegin = vector,
                                uEndVel = VectorLF3.Cross(vectorLF2 - uPos, swarm.orbits[calcOrbitId].up).normalized * Math.Sqrt((double)(swarm.dysonSphere.gravity / swarm.orbits[calcOrbitId].radius)), //至少影响着形成的太阳帆的初速度方向
                                uBegin = uBeginChange,
                                uEnd = vectorLF2
                            }, calcOrbitId);
                        }

                        //设定子弹目标以及伤害，并注册伤害
                        try
                        {
                            if (bulletIndex != -1)
                                swarm.bulletPool[bulletIndex].state = 0; //设置成0，该子弹将不会生成太阳帆
                        }
                        catch (Exception)
                        {
                            DspBattlePlugin.logger.LogInfo("bullet info1 set error.");
                        }
                        UIBattleStatistics.RegisterShootOrLaunch(__instance.bulletId, damage, bulletCost);

                        if (bulletIndex != -1)
                            Cannon.bulletTargets[swarm.starData.index].AddOrUpdate(bulletIndex, curTarget.shipIndex, (x, y) => curTarget.shipIndex);

                        //Main.logger.LogInfo("bullet info2 set error.");


                        try
                        {
                            int bulletId = __instance.bulletId;
                            if (bulletIndex != -1)
                                Cannon.bulletIds[swarm.starData.index].AddOrUpdate(bulletIndex, bulletId, (x, y) => bulletId);
                            // bulletIds[swarm.starData.index][bulletIndex] = 1;//后续可以根据子弹类型/炮类型设定不同数值
                        }
                        catch (Exception)
                        {
                            DspBattlePlugin.logger.LogInfo("bullet info3 set error.");
                        }
                        int bulletIncCost = 0;
                        if (__instance.bulletCount != 0)
                        {
                            bulletIncCost = bulletCost * __instance.bulletInc / __instance.bulletCount;
                            __instance.bulletInc -= bulletIncCost;
                        }
                        __instance.bulletCount -= bulletCost;
                        if (gmProtoId == 8012 && Relic.HaveRelic(2, 3) && Relic.Verify(0.75)) // relic2-3 回声 概率回填弹药
                        {
                            __instance.bulletCount += 1;
                            __instance.bulletInc += bulletIncCost / bulletCost;
                        }
                        if (__instance.bulletCount <= 0)
                        {
                            __instance.bulletInc = 0;
                            __instance.bulletCount = 0;
                        }
                        __instance.time = __instance.coldSpend;
                        __instance.direction = -1;

                        //if (gmProtoId == 8014) //激光炮为了视觉效果，取消冷却阶段每帧都发射（不能简单地将charge和cold的spend设置为0，因为会出现除以0的错误）
                        //    __instance.direction = 1;

                    }
                }

                else if (__instance.direction == -1)
                {
                    __instance.time -= num3;
                    if (__instance.time <= 0)
                    {
                        __instance.time = 0;
                        __instance.direction = (flag3 ? 1 : 0);
                    }
                }
                else
                {
                    __instance.time = 0;

                }

                return result;
            }

        }
    }
}

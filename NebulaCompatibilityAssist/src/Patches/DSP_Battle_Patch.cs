using DSP_Battle;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using UnityEngine;

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSP_Battle_Patch
    {
        private const string NAME = "DSP_Battle";
        private const string GUID = "com.ckcz123.DSP_Battle";
        private const string VERSION = "2.2.2";
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
                    Log.Info("Import enemy ships");
                    EnemyShips.Import(r);
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
                // In WaveStages.UpdateWaveStage3(long time)
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
                    SendConfig();
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

                ShowMessageInChat("add relic ".Translate() + num);
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

            [HarmonyPrefix, HarmonyPatch(typeof(Relic), nameof(Relic.AskRemoveRelic))]
            static bool AskRemoveRelic_Prefix(int removeType, int removeNum)
            {
                if (!NebulaModAPI.IsMultiplayerActive) return true; // Don't overwirte in single player

                if (removeType > 3 || removeNum > 30)
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
                    UIRelic.RefreshSlotsWindowUI();
                    UIRelic.HideSlots();
                });
                return false;
            }

            static void RemoveRelic(int removeType, int removeNum)
            {
                Relic.relics[removeType] = (Relic.relics[removeType] ^ 1 << removeNum);
                if (!NebulaModAPI.IsMultiplayerActive) return;

                ShowMessageInChat("remove relic ".Translate() + removeNum);
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

            [HarmonyPrefix, HarmonyPatch(typeof(UIAlert), nameof(UIAlert.RefreshBattleProgress))]
            static bool RefreshBattleProgress()
            {
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

                            UIAlert.totalDistance += ship.distanceToTarget; // MOD
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
                        double curTotalDistance = 0;
                        double curTotalStrength = 0;
                        foreach (var shipIndex in EnemyShips.ships.Keys)
                        {
                            var ship = EnemyShips.ships[shipIndex];
                            curTotalDistance += ship.distanceToTarget; // MOD
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
                            float deRatio = 1.0f;
                            //if(elimPoint > 0.99)//快消灭干净了，让绿条多填充，弥补一半之前建筑炸掉的比例减成
                            //{
                            //    deRatio = 0.5f / elimPointRatio;
                            //}
                            UIAlert.elimProgRT.sizeDelta = new Vector2(996 * leftProp * UIAlert.elimPointRatio * deRatio, 5);
                            UIAlert.invaProgRT.sizeDelta = new Vector2(996 * (1 - leftProp * UIAlert.elimPointRatio * deRatio), 5);
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
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
                {
                    // Let ship destoryed in client stay until host send packet
                    return isIncomingPacket;
                }
                return true;
            }


            [HarmonyPostfix, HarmonyPatch(typeof(EnemyShip), nameof(EnemyShip.FindAnotherStation))]
            static void OnTargetChange(EnemyShip __instance)
            {
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    Log.Dev($"[Battle] send ship{__instance.shipIndex}:{__instance.state} P{__instance.shipData.planetB} HP:{__instance.hp}");
                    SendEnemyShipState(__instance);
                }
            }

            [HarmonyPostfix, HarmonyPatch(typeof(EnemyShips), nameof(EnemyShips.OnShipDestroyed))]
            static void OnShipDestroyed(EnemyShip ship)
            {
                if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsHost)
                {
                    Log.Dev($"[Battle] send ship{ship.shipIndex}:{ship.state} P{ship.shipData.planetB} HP: {ship.hp}");
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
                            EnemyShips.ships.TryRemove(ship.shipIndex, out _);
                            return;
                        }
                        else if (ship.state == EnemyShip.State.uninitialized)
                        {
                            // EnemyShip.Revive
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

                        Log.Dev($"[Battle] ship[{ship.shipIndex}]:{ship.state} HP:{ship.hp} INC:{ship.shipData.inc}");

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
    }
}

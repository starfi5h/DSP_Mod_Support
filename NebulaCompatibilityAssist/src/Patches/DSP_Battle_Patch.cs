using crecheng.DSPModSave;
using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Hotfix;
using NebulaCompatibilityAssist.Packets;
using NebulaWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

//#pragma warning disable IDE0018 // 內嵌變數宣告

namespace NebulaCompatibilityAssist.Patches
{
    public static class DSP_Battle_Patch
    {
        private const string NAME = "DSP_Battle";
        private const string GUID = "com.ckcz123.DSP_Battle";
        private const string VERSION = "3.4.10";

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

                harmony.PatchAll(typeof(Warper));

                var classType = AccessTools.TypeByName("CommonAPI.Patches.ResourcesPatch");
                var methodInfo = AccessTools.Method(classType, "Prefix");
                harmony.Patch(methodInfo, null, null, new HarmonyMethod(AccessTools.Method(typeof(DSP_Battle_Patch), nameof(ResourcesPatch_Prefix_Transpiler))));

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
                using var p = NebulaModAPI.GetBinaryReader(bytes);
                Save.Import(p.BinaryReader);
            }
        }

        public static void OnReceive(NC_BattleUpdate packet)
        {
            Warper.HandleRequest(packet);
        }

        public static IEnumerable<CodeInstruction> ResourcesPatch_Prefix_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace : CommonAPIPlugin.logger.LogDebug("Loading registered asset ...");
                // with    : Nop
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Callvirt && ((MethodInfo)i.operand).Name == "LogDebug"))
                    .Repeat(matcher => matcher.SetAndAdvance(OpCodes.Pop, null));

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Warn("CommonAPI.Patches.ResourcesPatch.Prefix Transpiler fail!");
                Log.Dev(e);
                return instructions;
            }
        }

        private static class Warper
        {

            private static bool IsIncoming { get; set; }

            public static void HandleRequest(NC_BattleUpdate packet)
            {
                string message = $"{packet.Username} {packet.Type} ";
                Log.Info(message + $"{packet.Value1} {packet.Value2}");
                IsIncoming = true;
                try
                {
                    switch (packet.Type)
                    {
                        case NC_BattleUpdate.EType.AddRelic:
                        {
                            DSP_Battle.Relic.AddRelic(packet.Value1, packet.Value2);
                            DSP_Battle.UIRelic.RefreshSlotsWindowUI();
                            message += ("遗物名称" + packet.Value1.ToString() + "-" + packet.Value2.ToString()).Translate().Split('\n')[0];
                            break;
                        }
                        case NC_BattleUpdate.EType.RemoveRelic:
                        {
                            DSP_Battle.Relic.RemoveRelic(packet.Value1, packet.Value2);
                            DSP_Battle.UIRelic.RefreshSlotsWindowUI();
                            message += ("遗物名称" + packet.Value1.ToString() + "-" + packet.Value2.ToString()).Translate().Split('\n')[0];
                            break;
                        }
                        case NC_BattleUpdate.EType.ApplyAuthorizationPoint:
                        {
                            DSP_Battle.UISkillPointsWindow.ClearTempLevelAdded();
                            Array.Copy(packet.Values1, DSP_Battle.UISkillPointsWindow.tempLevelAddedL, Math.Min(packet.Values1.Length, DSP_Battle.UISkillPointsWindow.tempLevelAddedL.Length));
                            Array.Copy(packet.Values2, DSP_Battle.UISkillPointsWindow.tempLevelAddedR, Math.Min(packet.Values2.Length, DSP_Battle.UISkillPointsWindow.tempLevelAddedR.Length));
                            DSP_Battle.SkillPoints.ConfirmAll();
                            break;
                        }
                        case NC_BattleUpdate.EType.ResetAuthorizationPoint:
                        {
                            DSP_Battle.SkillPoints.ResetAll();
                            break;
                        }
                    }
                    ChatManager.ShowMessageInChat(message);
                }
                catch (Exception e)
                {
                    ChatManager.ShowWarningInChat("DSP_Battle_Patch error!\n" + e);
                }
                IsIncoming = false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DSP_Battle.Relic), nameof(DSP_Battle.Relic.AddRelic))]
            static void AddRelic_Postfix(int type, int num)
            {
                if (!Multiplayer.IsActive || IsIncoming) return;
                Multiplayer.Session.Network.SendPacket(new NC_BattleUpdate(NC_BattleUpdate.EType.AddRelic, type, num));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DSP_Battle.Relic), nameof(DSP_Battle.Relic.RemoveRelic))]
            static void RemoveRelic_Postfix(int removeType, int removeNum)
            {
                if (!Multiplayer.IsActive || IsIncoming) return;
                Multiplayer.Session.Network.SendPacket(new NC_BattleUpdate(NC_BattleUpdate.EType.RemoveRelic, removeType, removeNum));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DSP_Battle.SkillPoints), nameof(DSP_Battle.SkillPoints.ConfirmAll))]
            static void SkillConfirmAll_Prefix()
            {
                if (!Multiplayer.IsActive || IsIncoming) return;
                Multiplayer.Session.Network.SendPacket(new NC_BattleUpdate(NC_BattleUpdate.EType.ApplyAuthorizationPoint,
                    DSP_Battle.UISkillPointsWindow.tempLevelAddedL, DSP_Battle.UISkillPointsWindow.tempLevelAddedR));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DSP_Battle.SkillPoints), nameof(DSP_Battle.SkillPoints.ResetAll))]
            static void ResetAll_Prefix()
            {
                if (!Multiplayer.IsActive || IsIncoming) return;
                Multiplayer.Session.Network.SendPacket(new NC_BattleUpdate(NC_BattleUpdate.EType.ResetAuthorizationPoint,
                    0, 0));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.KillEnemyFinally))]
            static void KillEnemyFinally_Prefix(PlanetFactory __instance, int enemyId)
            {
                if (!Multiplayer.IsActive || !Multiplayer.Session.IsClient || !Multiplayer.Session.Combat.IsIncomingRequest.Value)
                {
                    return;
                }

                if (enemyId <= 0 || enemyId >= __instance.enemyPool.Length) return;

                // 如果是由其他玩家擊殺(DFGKillEnemyProcessor.cs), 觸發地面敵人擊殺效果
                try
                {
                    CombatStat combatStat = new();
                    combatStat.astroId = __instance.planetId;
                    combatStat.objectType = 4;
                    combatStat.objectId = enemyId;
                    DSP_Battle.EventSystem.ZeroHpInceptor(ref combatStat, GameMain.data, GameMain.data.spaceSector.skillSystem);
                }
                catch (Exception ex)
                {
                    Log.Warn(ex);
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(SpaceSector), nameof(SpaceSector.KillEnemyFinal))]
            static void KillEnemyFinal_Prefix(SpaceSector __instance, int enemyId)
            {
                if (!Multiplayer.IsActive || !Multiplayer.Session.IsClient || !Multiplayer.Session.Enemies.IsIncomingRequest.Value)
                {
                    return;
                }

                if (enemyId <= 0 || enemyId >= __instance.enemyPool.Length) return;

                // 如果是由其他玩家擊殺(DFSKillEnemyProcessor.cs), 觸發太空敵人擊殺效果
                try
                {
                    ref EnemyData ptr = ref __instance.enemyPool[enemyId];
                    CombatStat combatStat = new();
                    combatStat.astroId = ptr.originAstroId;
                    combatStat.objectType = 4;
                    combatStat.objectId = enemyId;
                    DSP_Battle.EventSystem.ZeroHpInceptor(ref combatStat, GameMain.data, GameMain.data.spaceSector.skillSystem);
                }
                catch (Exception ex)
                {
                    Log.Warn(ex);
                }
            }
        }
    }
}

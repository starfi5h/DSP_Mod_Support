using HarmonyLib;
using NebulaModel;
using NebulaModel.DataStructures.Chat;
using NebulaModel.Networking;
using NebulaModel.Packets.GameStates;
using NebulaModel.Packets.Logistics;
using NebulaModel.Utils;
using NebulaNetwork;
using NebulaWorld;
using NebulaWorld.Chat;
using NebulaWorld.Combat;
using NebulaWorld.GameStates;
using NebulaWorld.Logistics;
using NebulaWorld.MonoBehaviours.Local.Chat;
using NebulaWorld.Player;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

namespace NebulaCompatibilityAssist.Hotfix
{
    public static class NebulaHotfix
    {
        //private const string NAME = "NebulaMultiplayerMod";
        private const string GUID = "dsp.nebula-multiplayer";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                System.Version nebulaVersion = pluginInfo.Metadata.Version;
                
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 9 && nebulaVersion.Build == 7)
                {
                    harmony.PatchAll(typeof(Warper097));
                    Log.Info("Nebula hotfix 0.9.7 - OK");
                }

                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(Analysis.StacktraceParser));
                harmony.PatchAll(typeof(SuppressErrors));
                Log.Info("Nebula extra features - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        /*
        private static void PatchPacketProcessor(Harmony harmony)
        {
            Type classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
            harmony.Patch(AccessTools.Method(classType, "JoinGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
        }
        */
    }

    public static class SuppressErrors
    {
        static bool suppressed = false;

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnGameBegin()
        {
            suppressed = false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.GameTickLogic))]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.KeyTickLogic))]
        [HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.GameTickLogic))]
        [HarmonyPatch(typeof(EnemyDFHiveSystem), nameof(EnemyDFHiveSystem.KeyTickLogic))]
        [HarmonyPatch(typeof(SkillSystem), nameof(SkillSystem.GameTick))]
        [HarmonyPatch(typeof(DefenseSystem), nameof(DefenseSystem.GameTick))]
        public static Exception EnemyGameTick_Finalizer(Exception __exception)
        {
            if (__exception != null && !suppressed)
            {
                suppressed = true;
                var msg = "NebulaCompatibilityAssist suppressed the following exception: \n" + __exception.ToString();
                ChatManager.ShowWarningInChat(msg);
                Log.Error(msg);
            }
            return null;
        }
    }

    public static class Warper097
    {
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(EnemyDFGroundSystem), nameof(EnemyDFGroundSystem.CalcFormsSupply))]
        public static Exception CalcFormsSupply_Finalizer(Exception __exception)
        {
            if (__exception != null)
            {
                var msg = "Exception during loading: \n" + __exception.ToString();
                ChatManager.ShowWarningInChat(msg);
                Log.Error(msg);
            }
            return null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputePowerTab))]
        public static bool ComputePowerTab_Prefix(UIStatisticsWindow __instance, PowerStat[] powerPool, long energyConsumption, long factoryIndex)
        {
            if (!Multiplayer.IsActive || Multiplayer.Session.LocalPlayer.IsHost) return true;

            /* This is fix for the power statistics.
               Originally, this function is iterating through all factories and manually summing up "energyStored" values from their PowerSystems.
               Since client does not have all factories loaded it would cause exceptions.
             * This fix is basically replacing this:

                PowerSystem powerSystem = this.gameData.factories[i].powerSystem;
                int netCursor = powerSystem.netCursor;
                PowerNetwork[] netPool = powerSystem.netPool;
                for (int j = 1; j < netCursor; j++)
                {
                    num2 += netPool[j].energyStored;
                }

                With: Multiplayer.Session.Statistics.UpdateTotalChargedEnergy(factoryIndex);

             * In the UpdateTotalChargedEnergy(), the total energyStored value is being calculated no clients based on the data received from the server. */

            long num = __instance.ComputePower(powerPool[0]);
            __instance.productEntryList.Add(1, num, 0L, energyConsumption);
            num = __instance.ComputePower(powerPool[1]);
            __instance.productEntryList.Add(1, 0L, num);
            num = __instance.ComputePower(powerPool[3]);

            long num2 = UpdateTotalChargedEnergy((int)factoryIndex);

            __instance.productEntryList.Add(2, num, 0L, num2);
            num = __instance.ComputePower(powerPool[2]);
            __instance.productEntryList.Add(2, 0L, num);
            return false;
        }

        public static long UpdateTotalChargedEnergy(int factoryIndex)
        {
            var powerEnergyStoredData = Multiplayer.Session.Statistics.PowerEnergyStoredData;
            if (powerEnergyStoredData == null || factoryIndex >= powerEnergyStoredData.Length) return 0;
            return powerEnergyStoredData[factoryIndex];
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChatUtils), "IsCommandMessage")]
        public static bool IsCommandMessage(this ChatMessageType type, ref bool __result)
        {
            __result = !(type is ChatMessageType.PlayerMessage or ChatMessageType.PlayerMessagePrivate);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChatWindow), nameof(ChatWindow.SendLocalChatMessage))]
        public static bool SendLocalChatMessage_Prefix(ChatWindow __instance, string text, ChatMessageType messageType, ref ChatMessage __result)
        {
            __result = SendLocalChatMessage(__instance, text, messageType);
            return false;
        }

        public static ChatMessage SendLocalChatMessage(ChatWindow @this, string text, ChatMessageType messageType)
        {
            if (!messageType.IsCommandMessage())
            {
                text = ChatUtils.SanitizeText(text);
            }
            else
            {
                switch (messageType)
                {
                    case ChatMessageType.SystemInfoMessage when !Config.Options.EnableInfoMessage:
                    case ChatMessageType.SystemWarnMessage when !Config.Options.EnableWarnMessage:
                    case ChatMessageType.BattleMessage when !Config.Options.EnableBattleMessage:
                        return null;
                }
            }

            text = RichChatLinkRegistry.ExpandRichTextTags(text);

            if (@this.messages.Count > ChatWindow.MAX_MESSAGES)
            {
                @this.messages[0].DestroyMessage();
                @this.messages.Remove(@this.messages[0]);
            }

            var textObj = UnityEngine.Object.Instantiate(@this.textObject, @this.chatPanel);
            var newMsg = new ChatMessage(textObj, text, messageType);

            var notificationMsg = UnityEngine.Object.Instantiate(textObj, @this.notifier);
            newMsg.notificationText = notificationMsg.GetComponent<TMP_Text>();
            var message = notificationMsg.AddComponent<NotificationMessage>();
            message.Init(Config.Options.NotificationDuration);

            @this.messages.Add(newMsg);

            if (@this.chatWindow.activeSelf)
            {
                return newMsg;
            }
            if (Config.Options.AutoOpenChat && !messageType.IsCommandMessage())
            {
                @this.Toggle(false, false);
            }

            return newMsg;
        }
    }
}

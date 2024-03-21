using HarmonyLib;
using System;
using System.Reflection;

using NebulaModel.Networking;
using NebulaModel.Packets.Warning;

namespace FactoryLocator.Compat
{
    public static class NebulaCompat
    {
        public static bool IsClient { get; private set; }
        public static bool SyncWarning { get; set; } = true;

        private const string GUID = "dsp.nebula-multiplayer";
        private static bool isPatched;

        public static void Init()
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                // Initial patching version: 0.8.12
                Patch();
                Log.Debug("Nebula compat - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        public static void OnOpen()
        {
            if (IsClient)
            {
                SyncWarning = false;

                var ws = GameMain.data.warningSystem;
                ws.warningRecycleCursor = 0;
                for (int i = 1; i < ws.warningCursor; i++)
                {
                    if (ws.warningPool[i].id != i)
                        ws.warningRecycle[ws.warningRecycleCursor++] = i;
                }
            }
        }

        public static void OnClose()
        {
            if (IsClient)
            {
                SyncWarning = true;
            }
        }

        public static void OnUpdate()
        {
            if (IsClient)
            {
                // WarningLogic is disable in client, so we need to update warningSignals and warningSignalCount
                var ws = GameMain.data.warningSystem;
                Array.Clear(ws.warningCounts, 0, ws.warningCounts.Length);
                Array.Clear(ws.warningSignals, 0, ws.warningSignals.Length);
                ws.warningSignalCount = 0;

                for (int i = 1; i < ws.warningCursor; i++)
                {
                    if (ws.warningPool[i].id == i && ws.warningPool[i].state > 0)
                    {
                        int signalId = ws.warningPool[i].signalId;
                        if (ws.warningCounts[signalId] == 0)
                        {
                            ws.warningSignals[ws.warningSignalCount++] = signalId;
                        }
                        ws.warningCounts[signalId]++;
                    }
                }
                // Bubble sort warningSignals
                for (int l = 0; l < ws.warningSignalCount - 1; l++)
                {
                    for (int m = l + 1; m < ws.warningSignalCount; m++)
                    {
                        if (ws.warningSignals[l] > ws.warningSignals[m])
                        {
                            int tmp = ws.warningSignals[m];
                            ws.warningSignals[m] = ws.warningSignals[l];
                            ws.warningSignals[l] = tmp;
                        }
                    }
                }
            }
        }

        public static void Patch()
        {
            Type classType;
            classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            Plugin.harmony.Patch(AccessTools.Method(classType, "JoinGame"), new HarmonyMethod(typeof(NebulaCompat).GetMethod(nameof(BeforeJoinGame))));
            Plugin.harmony.Patch(AccessTools.Method(classType, "LeaveGame"), new HarmonyMethod(typeof(NebulaCompat).GetMethod(nameof(BeforeLeaveGame))));


            classType = AccessTools.TypeByName("NebulaPatcher.Patches.Dynamic.WarningSystem_Patch");
            var method = AccessTools.Method(classType, "CalcFocusDetail_Prefix");
            if (method != null)
                Plugin.harmony.Patch(method, new HarmonyMethod(typeof(NebulaCompat).GetMethod(nameof(Guard))));
        }

        public static void BeforeJoinGame()
        {
            if (!isPatched)
            {
                isPatched = true;
                try
                {
                    // We need patch PacketProcessor after NebulaNetwork assembly is loaded
                    foreach (Assembly a in AccessTools.AllAssemblies())
                    {
                        //Somehow need to iterate all assemblies
                    }

                    Type classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Warning.WarningDataProcessor");
                    MethodInfo methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(WarningDataPacket), typeof(NebulaConnection) });
                    Plugin.harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaCompat).GetMethod(nameof(Guard))));

                    classType = AccessTools.TypeByName("NebulaNetwork.PacketProcessors.Warning.WarningSignalProcessor");
                    methodInfo = AccessTools.Method(classType, "ProcessPacket", new Type[] { typeof(WarningSignalPacket), typeof(NebulaConnection) });
                    Plugin.harmony.Patch(methodInfo, new HarmonyMethod(typeof(NebulaCompat).GetMethod(nameof(Guard))));

                    Log.Info("PacketProcessors patch success!");
                }
                catch (Exception e)
                {
                    Log.Warn("PacketProcessors patch fail!");
                    Log.Warn(e);
                }
            }
            IsClient = true;
        }

        public static void BeforeLeaveGame()
        {
            IsClient = false;
        }


        public static bool Guard()
        {
            // Stop warning data syncing when the window is opened
            return SyncWarning;
        }
    }
}

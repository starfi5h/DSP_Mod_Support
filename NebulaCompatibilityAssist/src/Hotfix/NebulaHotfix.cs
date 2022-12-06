using HarmonyLib;
using System;
using System.IO;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Reflection;

using NebulaAPI;
using NebulaWorld;
using NebulaModel.Packets.Factory.Splitter;
using NebulaModel.Packets.Trash;
using UnityEngine;
using System.Collections;

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
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 8 && nebulaVersion.Build == 12)
                {
                    Patch0812(harmony);
                    Log.Info("Nebula hotfix 0.8.12 - OK");                    
                }
                ChatManager.Init(harmony);
                harmony.PatchAll(typeof(Analysis.StacktraceParser));
                Log.Info("Nebula extra features - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"Nebula hotfix patch fail! Current version: " + pluginInfo.Metadata.Version);
                Log.Debug(e);
            }
        }

        private static void Patch0812(Harmony harmony)
        {
            Type classType;
            classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
            harmony.Patch(AccessTools.Method(classType, "JoinGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));

            classType = AccessTools.TypeByName("NebulaWorld.SimulatedWorld");
            harmony.Patch(AccessTools.Method(classType, "SetupInitialPlayerState"),
                null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(SetupInitialPlayerState))));

            harmony.Patch(typeof(PlanetTransport).GetMethod("RefreshDispenserOnStoragePrebuildBuild"),
                null, null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(RefreshDispenserOnStoragePrebuildBuild_Transpiler))));

            //=== Fix trash warning ===
            classType = AccessTools.TypeByName("NebulaPatcher.Patches.Dynamic.TrashContainer_Patch");
            harmony.Patch(AccessTools.Method(classType, "NewTrash_Postfix"),
                new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(DisableFunction))));
            harmony.Patch(typeof(TrashContainer).GetMethod("NewTrash"),
                null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(NewTrash_Postfix))));

            classType = AccessTools.TypeByName("NebulaWorld.Warning.WarningManager");
            harmony.Patch(AccessTools.Method(classType, "ExportBinaryData"),
                new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(ExportBinaryData_Prefix))));
            harmony.Patch(AccessTools.Method(classType, "ImportBinaryData"),
                new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(ImportBinaryData))));

            //=== Fix splitter? ===
            classType = AccessTools.TypeByName("NebulaPatcher.Patches.Dynamic.SplitterComponent_Patch");
            harmony.Patch(AccessTools.Method(classType, "SetPriority_Postfix"),
                new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(DisableFunction))));

            //=== Hide ip ===
            classType = AccessTools.TypeByName("NebulaPatcher.Patches.Dynamic.UIMainMenu_Patch");
            harmony.Patch(AccessTools.Method(classType, "JoinGame"),
                null, null, new HarmonyMethod(typeof(NebulaHotfix).GetMethod(nameof(JoinGame_Transpiler))));
            ConnectToServer = AccessTools.MethodDelegate<Func<string, int, bool, string, bool>>(AccessTools.Method(classType, "ConnectToServer"));

            harmony.PatchAll(typeof(NebulaHotfix));
        }

        public static bool DisableFunction()
        {
            return false;
        }

        public static void SetupInitialPlayerState()
        {
            var player = NebulaModAPI.MultiplayerSession.LocalPlayer;
            if (player.IsClient && player.IsNewPlayer)
            {
                // Make new client spawn higher to avoid collision
                float altitude = GameMain.mainPlayer.transform.localPosition.magnitude;
                if (altitude > 0)
                    GameMain.mainPlayer.transform.localPosition *= (altitude + 20f) / altitude;
                Log.Debug($"Starting: {GameMain.mainPlayer.transform.localPosition} {altitude}");
            }
            else
            {
                // Prevent old client from dropping into gas gaint
                var planet = GameMain.galaxy.PlanetById(player.Data.LocalPlanetId);
                if (planet != null && planet.type == EPlanetType.Gas)
                {
                    GameMain.mainPlayer.movementState = EMovementState.Fly;
                }
            }
            // Set the name of local player in starmap from Icarus to user name
            GameMain.mainPlayer.mecha.appearance.overrideName = " " + player.Data.Username + " ";
        }

        public static IEnumerable<CodeInstruction> RefreshDispenserOnStoragePrebuildBuild_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // factoryModel.gpuiManager is null for remote planets, so we need to use GameMain.gpuiManager which is initialized by nebula
                // replace : this.factory.planet.factoryModel.gpuiManager
                // with    : GameMain.gpuiManager
                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(OpCodes.Ldarg_0),
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(OpCodes.Callvirt),
                        new CodeMatch(OpCodes.Ldfld),
                        new CodeMatch(i => i.opcode ==OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "gpuiManager")
                    )
                    .Repeat(matcher => matcher
                            .RemoveInstructions(4)
                            .SetAndAdvance(OpCodes.Call, typeof(GameMain).GetProperty("gpuiManager").GetGetMethod()
                    ));

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Warn("RefreshDispenserOnStoragePrebuildBuild_Transpiler fail!");
                Log.Dev(e);
                return instructions;
            }
        }

        public static void NewTrash_Postfix(TrashContainer __instance, int __result, TrashObject trashObj, TrashData trashData)
        {
            //Notify other that trash was created 
            if (Multiplayer.IsActive && !Multiplayer.Session.Trashes.NewTrashFromOtherPlayers)
            {
                //Refresh trash to assign local planet Id and local position
                GameMain.data.trashSystem.Gravity(ref trashData, GameMain.data.galaxy.astrosData, 0, 0, 0, (GameMain.data.localPlanet != null) ? GameMain.data.localPlanet.id : 0, (GameMain.data.localPlanet != null) ? GameMain.data.localPlanet.data : null);
                Multiplayer.Session.Network.SendPacket(new TrashSystemNewTrashCreatedPacket(__result, trashObj, trashData, Multiplayer.Session.LocalPlayer.Id, GameMain.mainPlayer.planetId));
            }
            // Wait until WarningDataPacket to assign warningId
            if (Multiplayer.IsActive && Multiplayer.Session.LocalPlayer.IsClient)
            {
                __instance.trashDataPool[__result].warningId = -1;
            }
        }

        public static bool ExportBinaryData_Prefix(BinaryWriter bw, ref int __result)
        {
            __result = ExportBinaryData(bw);
            return false;
        }

        public static int ExportBinaryData(BinaryWriter bw)
        {
            var ws = GameMain.data.warningSystem;
            int activeWarningCount = 0;
            WarningData[] warningPool = ws.warningPool;
            //index start from 1 in warningPool
            for (int i = 1; i < ws.warningCursor; i++)
            {
                WarningData data = warningPool[i];
                if (data.id == i && data.state > 0)
                {
                    bw.Write(data.signalId);
                    bw.Write(data.detailId);
                    bw.Write(data.astroId);
                    bw.Write(data.localPos.x);
                    bw.Write(data.localPos.y);
                    bw.Write(data.localPos.z);

                    int trashId = data.factoryId == WarningData.TRASH_SYSTEM ? data.objectId : -1;
                    bw.Write(trashId);

                    activeWarningCount++;
                }
            }
            return activeWarningCount;
        }

        public static bool ImportBinaryData(BinaryReader br, int activeWarningCount)
        {
            var ws = GameMain.data.warningSystem;
            int newCapacity = ws.warningCapacity;
            while (activeWarningCount + 1 > newCapacity)
            {
                newCapacity *= 2;
            }
            if (newCapacity > ws.warningCapacity)
            {
                ws.SetWarningCapacity(newCapacity);
            }
            ws.warningCursor = activeWarningCount + 1;

            WarningData[] warningPool = GameMain.data.warningSystem.warningPool;
            //index start from 1 in warningPool
            for (int i = 1; i <= activeWarningCount; i++)
            {
                // factoryId is not synced to skip WarningLogic update in client
                warningPool[i].id = i;
                warningPool[i].state = 1;
                warningPool[i].signalId = br.ReadInt32();
                warningPool[i].detailId = br.ReadInt32();
                // localPos is base on astroId
                warningPool[i].astroId = br.ReadInt32();
                warningPool[i].localPos.x = br.ReadSingle();
                warningPool[i].localPos.y = br.ReadSingle();
                warningPool[i].localPos.z = br.ReadSingle();

                // reassign warningId for trash
                int trashId = br.ReadInt32();
                if (trashId >= 0 && trashId < GameMain.data.trashSystem.container.trashCursor)
                {
                    GameMain.data.trashSystem.container.trashDataPool[trashId].warningId = i;
                }
            }

            return false;
        }


        [HarmonyTranspiler]
        [HarmonyPatch(typeof(UISplitterWindow), nameof(UISplitterWindow.OnCircleClick))]
        [HarmonyPatch(typeof(UISplitterWindow), nameof(UISplitterWindow.OnCircleFilterRightClick))]
        [HarmonyPatch(typeof(UISplitterWindow), nameof(UISplitterWindow.OnCircleRightClick))]
        private static IEnumerable<CodeInstruction> SetPriority_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Intercept SetPriority() with warper to broadcast the change
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "SetPriority"))
                    .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(NebulaHotfix), nameof(SetPriority)))
                     );
                return matcher.InstructionEnumeration();
            }
            catch
            {
                NebulaModel.Logger.Log.Error("UISpraycoaterWindow.SetPriority_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }
        }

        private static void SetPriority(ref SplitterComponent splitter, int slot, bool isPriority, int filter)
        {
            splitter.SetPriority(slot, isPriority, filter);
            if (Multiplayer.IsActive)
            {
                Multiplayer.Session.Network.SendPacketToLocalStar(new SplitterPriorityChangePacket(splitter.id, slot, isPriority, filter, GameMain.localPlanet?.id ?? -1));
            }
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickInserters), new Type[] { typeof(long), typeof(bool) })]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.GameTickInserters), new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int) })]
        private static IEnumerable<CodeInstruction> GameTickInserters_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                CodeMatcher matcher = new CodeMatcher(instructions)
                    .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "InternalOffsetCorrection"))
                    .Repeat(matcher => matcher
                        .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(NebulaHotfix), nameof(InternalOffsetCorrection)))
                     );
                return matcher.InstructionEnumeration();
            }
            catch
            {
                NebulaModel.Logger.Log.Error("FactorySystem.GameTickInserters_Transpiler failed. Mod version not compatible with game version.");
                return instructions;
            }
        }

        private static void InternalOffsetCorrection(ref InserterComponent inserter, EntityData[] entityPool, CargoTraffic traffic, BeltComponent[] beltPool)
        {
            bool flag = false;
            int beltId = entityPool[inserter.pickTarget].beltId;
            if (beltId > 0)
            {
                CargoPath cargoPath = traffic.GetCargoPath(beltPool[beltId].segPathId);
                if (cargoPath != null)
                {
                    int num = beltPool[beltId].segPivotOffset + beltPool[beltId].segIndex;
                    int num2 = num + (int)inserter.pickOffset;
                    if (num2 < 4)
                    {
                        num2 = 4;
                    }
                    if (num2 + 5 >= cargoPath.pathLength)
                    {
                        num2 = cargoPath.pathLength - 5 - 1;
                    }
                    if (inserter.pickOffset != (short)(num2 - num))
                    {
                        Log.Warn($"{traffic.factory.planetId} Fix inserter{inserter.id} pickOffset {inserter.pickOffset} -> {num2 - num}");
                        inserter.pickOffset = (short)(num2 - num);
                        flag = true;
                    }
                }
            }
            int beltId2 = entityPool[inserter.insertTarget].beltId;
            if (beltId2 > 0)
            {
                CargoPath cargoPath2 = traffic.GetCargoPath(beltPool[beltId2].segPathId);
                if (cargoPath2 != null)
                {
                    int num3 = beltPool[beltId2].segPivotOffset + beltPool[beltId2].segIndex;
                    int num4 = num3 + (int)inserter.insertOffset;
                    if (num4 < 4)
                    {
                        num4 = 4;
                    }
                    if (num4 + 5 >= cargoPath2.pathLength)
                    {
                        num4 = cargoPath2.pathLength - 5 - 1;
                    }
                    if (inserter.insertOffset != (short)(num4 - num3))
                    {
                        Log.Warn($"{traffic.factory.planetId} Fix inserter{inserter.id} insertOffset {inserter.insertOffset} -> {num4 - num3}");
                        inserter.insertOffset = (short)(num4 - num3);
                        flag = true;
                    }
                }
            }
            if (flag && Multiplayer.IsActive)
            {
                Multiplayer.Session.Network.SendPacketToLocalStar(new InserterOffsetCorrectionPacket(inserter.id, inserter.pickOffset, inserter.insertOffset, traffic.factory.planetId));
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlanetFactory), nameof(PlanetFactory.Import))]
        public static void PlanetFactoryImport_Postfix(PlanetFactory __instance)
        {
            EntityData[] entityPool = __instance.entityPool;
            CargoTraffic traffic = __instance.factorySystem.traffic;
            BeltComponent[] beltPool = __instance.factorySystem.traffic.beltPool;
            for (int i = 1; i < __instance.factorySystem.inserterCursor; i++)
            {
                ref InserterComponent inserter = ref __instance.factorySystem.inserterPool[i];
                if (inserter.id == i)
                {
                    InternalOffsetCorrection(ref inserter, entityPool, traffic, beltPool);
                }
            }
        }

        public static IEnumerable<CodeInstruction> JoinGame_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                // replace : UIRoot.instance.StartCoroutine(UIMainMenu_Patch.TryConnectToServer(s, p, isIP, password));
                // with    : UIRoot.instance.StartCoroutine(TryConnectToServer(s, p, isIP, password));

                var codeMatcher = new CodeMatcher(instructions)
                    .MatchForward(false,
                        new CodeMatch(i => i.opcode == OpCodes.Call && ((MethodInfo)i.operand).Name == "TryConnectToServer")
                    )
                    .SetOperandAndAdvance(typeof(NebulaHotfix).GetMethod(nameof(TryConnectToServer)));

                return codeMatcher.InstructionEnumeration();
            }
            catch (Exception e)
            {
                Log.Warn("JoinGame_Transpiler fail!");
                Log.Dev(e);
                return instructions;
            }
        }

        static Func<string, int, bool, string, bool> ConnectToServer;
        public static IEnumerator TryConnectToServer(string ip, int port, bool isIP, string password)
        {
            Type UIMainMenu_Patch = AccessTools.TypeByName("NebulaPatcher.Patches.Dynamic.UIMainMenu_Patch");
            RectTransform multiplayerMenu = AccessTools.StaticFieldRefAccess<RectTransform>(UIMainMenu_Patch, "multiplayerMenu");
            InGamePopup.ShowInfo("Connecting", "Connecting to server...", null, null);
            multiplayerMenu.gameObject.SetActive(false);

            yield return new WaitForSeconds(0.5f);

            if (!ConnectToServer(ip, port, isIP, password))
            {
                InGamePopup.FadeOut();
                //re-enabling the menu again after failed connect attempt
                InGamePopup.ShowWarning("Connect failed", "Was not able to connect to server", "OK");
                multiplayerMenu.gameObject.SetActive(true);
            }
            else
            {
                InGamePopup.FadeOut();
            }
        }
    }
}

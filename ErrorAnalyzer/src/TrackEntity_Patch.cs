using System;
using HarmonyLib;
using UnityEngine;

namespace ErrorAnalyzer
{
    internal class TrackEntity_Patch
    {
        public static bool Active { get => _patch != null; }
        public static int AstroId { get; private set; }
        public static int EntityId { get; private set; }
        public static Vector3 LocalPos { get; private set; }

        private static Harmony _patch;

        public static void Enable(bool on)
        {
            AstroId = 0;
            EntityId = 0;
            if (on)
            {
                _patch ??= Harmony.CreateAndPatchAll(typeof(TrackEntity_Patch));
                if (GameConfig.gameVersion < new Version(0, 10, 33))
                {
                    _patch.PatchAll(typeof(Patch1032));
                    Plugin.Log.LogInfo("TrackEntity_Patch enable (0.10.32)");
                }
                else
                {
                    _patch.PatchAll(typeof(Patch1033));
                    Plugin.Log.LogInfo("TrackEntity_Patch enable");
                }                
                return;
            }
            _patch?.UnpatchSelf();
            _patch = null;
            Plugin.Log.LogInfo("TrackEntity_Patch disable");
        }

        public static void ResetId()
        {
            AstroId = 0;
            EntityId = 0;
            LocalPos = Vector3.zero;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIEntityBriefInfo), nameof(UIEntityBriefInfo._OnOpen))]
        static void UIEntityBriefInfo_OnOpen(UIEntityBriefInfo __instance)
        {
            __instance.entityIdText.text = __instance.entityId.ToString();
        }


        static class Patch1033
        {
            // Rework of multithread functions in 0.10.33
            [HarmonyFinalizer]
            [HarmonyPatch(typeof(FactorySystem), "GameTick")]
            [HarmonyPatch(typeof(FactorySystem), "GameTickLabProduceMode")]
            [HarmonyPatch(typeof(FactorySystem), "GameTickLabResearchMode")]
            [HarmonyPatch(typeof(FactorySystem), "GameTickInserters")]
            [HarmonyPatch(typeof(PowerSystem), "GameTick")]
            [HarmonyPatch(typeof(CargoTraffic), "SpraycoaterGameTick")]
            [HarmonyPatch(typeof(DefenseSystem), "GameTick")]
            [HarmonyPatch(typeof(ConstructionSystem), "GameTick")]
            static Exception GetFactoryId(Exception __exception, PlanetFactory ___factory)
            {
                if (__exception != null && AstroId == 0) AstroId = ___factory.planet.astroId;
                return __exception;
            }
        }

        static class Patch1032
        {
            // Last target version: 0.10.32.25783
            // Only track for multithread functions
            [HarmonyFinalizer]
            [HarmonyPatch(typeof(FactorySystem), "GameTick", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
            [HarmonyPatch(typeof(FactorySystem), "GameTickLabProduceMode", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int), typeof(int) })]
            [HarmonyPatch(typeof(FactorySystem), "GameTickLabResearchMode", new Type[] { typeof(long), typeof(bool) })]
            [HarmonyPatch(typeof(FactorySystem), "GameTickInserters", new Type[] { typeof(long), typeof(bool), typeof(int), typeof(int) })]
            [HarmonyPatch(typeof(PowerSystem), "GameTick")]
            [HarmonyPatch(typeof(CargoTraffic), "SpraycoaterGameTick")]
            [HarmonyPatch(typeof(CargoTraffic), "PresentCargoPathsAsync")]
            [HarmonyPatch(typeof(DefenseSystem), "GameTick")]
            [HarmonyPatch(typeof(ConstructionSystem), "GameTick")]
            static Exception GetFactoryId(Exception __exception, PlanetFactory ___factory)
            {
                if (__exception != null && AstroId == 0) AstroId = ___factory.planet.astroId;
                return __exception;
            }
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(MinerComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(AssemblerComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(FractionatorComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(FractionatorComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(EjectorComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(SiloComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(LabComponent), "InternalUpdateAssemble")]
        [HarmonyPatch(typeof(LabComponent), "InternalUpdateResearch")]
        [HarmonyPatch(typeof(InserterComponent), "InternalOffsetCorrection")]
        [HarmonyPatch(typeof(SpraycoaterComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(TurretComponent), "InternalUpdate")]
        static Exception GetEntityId (Exception __exception, int ___entityId)
        {
            if (__exception != null && EntityId == 0) EntityId = ___entityId;
            return __exception;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(InserterComponent), "InternalUpdate_Bidirectional")]
        [HarmonyPatch(typeof(InserterComponent), "InternalUpdate")]
        [HarmonyPatch(typeof(InserterComponent), "InternalUpdateNoAnim")]
        [HarmonyPatch(typeof(DispenserComponent), "InternalTick")]
        static Exception GetInserterId(Exception __exception, int ___entityId, PlanetFactory factory)
        {
            if (__exception != null)
            {
                if (AstroId == 0) AstroId = factory.planet.astroId;
                if (EntityId == 0) EntityId = ___entityId;
            }
            return __exception;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(StationComponent), "DetermineDispatch")]
        [HarmonyPatch(typeof(StationComponent), "InternalTickLocal")]
        [HarmonyPatch(typeof(StationComponent), "InternalTickRemote")]
        static Exception GetStationId(Exception __exception, int ___entityId, int ___planetId)
        {
            if (__exception != null)
            {
                if (AstroId == 0) AstroId = ___planetId;
                if (EntityId == 0) EntityId = ___entityId;
            }
            return __exception;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CargoPath), "PresentCargos")]
        static Exception GetCargoPathPos(Exception __exception, Vector3[] ___pointPos)
        {
            if (__exception != null)
            {
                if (LocalPos == Vector3.zero) LocalPos = ___pointPos[0];
            }
            return __exception;
        }

        #region Dismantle Belt

        // IndexOutOfRangeException: Index was outside the bounds of the array.
        // at CargoTraffic.SetBeltState(System.Int32 beltId, System.Int32 state); (IL_002D)
        // Worst outcome when suppressed: Belt highlight is incorrect
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CargoTraffic), "SetBeltState")]
        static Exception SetBeltState()
        {
            return null;
        }

        // IndexOutOfRangeException: Index was outside the bounds of the array.
        // CargoContainer.RemoveCargo (System.Int32 index) [0x0001e] ;IL_001E => (cargoPool[index])
        // Worst outcome when suppressed: may hide the cargoPool memory leak issue
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CargoContainer), "RemoveCargo")]
        static Exception RemoveCargo()
        {
            return null;
        }

        // IndexOutOfRangeException: Index was outside the bounds of the array.
        // CargoPath.TryPickItem (System.Int32 index, System.Int32 length, System.Byte& stack, System.Byte& inc) [0x000b6]
        // CargoTraffic.PickupBeltItems (Player player, System.Int32 beltId, System.Boolean all) [0x000cf]
        // Worst outcome when suppressed: player don't get item from the belt;
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(CargoTraffic), "PickupBeltItems")]
        static Exception PickupBeltItems()
        {
            return null;
        }

        #endregion
    }
}

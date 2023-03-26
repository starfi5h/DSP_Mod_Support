using HarmonyLib;
using System;
using System.Collections.Generic;

using NebulaWorld.Logistics;
using UnityEngine;
using NebulaModel.Packets.Logistics;
using NebulaModel.Packets.Players;
using NebulaWorld.MonoBehaviours.Remote;

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
                if (nebulaVersion.Major == 0 && nebulaVersion.Minor == 8 && nebulaVersion.Build == 13)
                {
                    Patch0813(harmony);
                    Log.Info("Nebula hotfix 0.8.13 - OK");                    
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

        private static void Patch0813(Harmony harmony)
        {
            Type classType;
            classType = AccessTools.TypeByName("NebulaWorld.Multiplayer");
            harmony.Patch(AccessTools.Method(classType, "HostGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));
            harmony.Patch(AccessTools.Method(classType, "JoinGame"), new HarmonyMethod(typeof(NebulaNetworkPatch).GetMethod(nameof(NebulaNetworkPatch.BeforeMultiplayerGame))));

            harmony.PatchAll(typeof(Warper0813));
        }

        private static class Warper0813
        {
            [HarmonyPrefix, HarmonyPatch(typeof(StationUIManager), "UpdateStorage")]
            static bool UpdateStorage(StationUI packet)
            {
                StationComponent stationComponent = StationUIManager.GetStation(packet.PlanetId, packet.StationId, packet.StationGId);
                if (stationComponent == null || stationComponent.storage?.Length == 0) // Storge may be null?
                {
                    return false;
                }
                return true;
            }

            #region mecha animation
            struct Snapshot
            {
                public EMovementState MovementState { get; set; }
                public float HorzSpeed { get; set; }
                public float VertSpeed { get; set; }
                public float Turning { get; set; }
                public float JumpWeight { get; set; }
                public float JumpNormalizedTime { get; set; }
                public byte IdleAnimIndex { get; set; }
                public byte MiningAnimIndex { get; set; }
                public float MiningWeight { get; set; }
                public PlayerMovement.EFlags Flags { get; set; }
            }

            static readonly Dictionary<RemotePlayerAnimation, Snapshot[]> dict = new();


            [HarmonyPrefix, HarmonyPatch(typeof(RemotePlayerAnimation), "UpdateState")]
            static bool RemotePlayerAnimationUpdateState(RemotePlayerAnimation __instance, PlayerMovement packet)
            {
                if (!dict.TryGetValue(__instance, out var snapshotBuffer))
                {
                    snapshotBuffer = new Snapshot[3];
                    dict[__instance] = snapshotBuffer;
                }

                for (int i = 0; i < snapshotBuffer.Length - 1; ++i)
                {
                    snapshotBuffer[i] = snapshotBuffer[i + 1];
                }

                snapshotBuffer[snapshotBuffer.Length - 1] = new Snapshot()
                {
                    MovementState = packet.MovementState,
                    HorzSpeed = packet.HorzSpeed,
                    VertSpeed = packet.VertSpeed,
                    Turning = packet.Turning,
                    JumpWeight = packet.JumpWeight,
                    JumpNormalizedTime = packet.JumpNormalizedTime,
                    IdleAnimIndex = packet.IdleAnimIndex,
                    MiningAnimIndex = packet.MiningAnimIndex,
                    MiningWeight = packet.MiningWeight,
                    Flags = packet.Flags
                };

                // TODO: Fix effect timing
                __instance.remotePlayerEffects.UpdateState(packet);

                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(RemotePlayerAnimation), "Update")]
            static bool RemotePlayerAnimationUpdate(RemotePlayerAnimation __instance)
            {
                if (!dict.TryGetValue(__instance, out var snapshotBuffer))
                    return false;

                var snapshot = snapshotBuffer[0];

                __instance.PlayerAnimator.jumpWeight = snapshot.JumpWeight;
                __instance.PlayerAnimator.jumpNormalizedTime = snapshot.JumpNormalizedTime;
                __instance.PlayerAnimator.idleAnimIndex = snapshot.IdleAnimIndex;
                __instance.PlayerAnimator.sailAnimIndex = 0;
                __instance.PlayerAnimator.miningWeight = snapshot.MiningWeight;
                __instance.PlayerAnimator.miningAnimIndex = snapshot.MiningAnimIndex;

                __instance.PlayerAnimator.movementState = snapshot.MovementState;
                __instance.PlayerAnimator.horzSpeed = snapshot.HorzSpeed;
                __instance.PlayerAnimator.turning = snapshot.Turning;
                PlanetData localPlanet = GameMain.galaxy.PlanetById(__instance.rootMovement.localPlanetId);
                __instance.altitudeFactor = (localPlanet == null) ? 1f : Mathf.Clamp01((__instance.transform.position.magnitude - localPlanet.realRadius - 7f) * 0.15f);

                float deltaTime = Time.deltaTime;
                __instance.CalculateMovementStateWeights(__instance.PlayerAnimator, deltaTime);
                __instance.CalculateDirectionWeights(__instance.PlayerAnimator);

                __instance.PlayerAnimator.AnimateIdleState(deltaTime);
                if (__instance.PlayerAnimator.idleAnimIndex == 0)
                {
                    for (int i = 1; i < __instance.PlayerAnimator.idles.Length; i++)
                    {
                        __instance.PlayerAnimator.idles[i].weight = 0;
                        __instance.PlayerAnimator.idles[i].normalizedTime = 0f;
                    }
                }
                __instance.PlayerAnimator.AnimateRunState(deltaTime);
                __instance.PlayerAnimator.AnimateDriftState(deltaTime);
                __instance.AnimateFlyState(__instance.PlayerAnimator);
                __instance.AnimateSailState(__instance.PlayerAnimator);

                __instance.PlayerAnimator.AnimateJumpState(deltaTime);
                __instance.PlayerAnimator.AnimateSkills(deltaTime);
                __instance.AnimateRenderers(__instance.PlayerAnimator);

                //__instance.remotePlayerEffects.UpdateState(packet);

                return false;
            }
            #endregion
        }
    }
}

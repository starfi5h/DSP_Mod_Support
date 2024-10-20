﻿using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[assembly: AssemblyTitle(CameraTools.Plugin.NAME)]
[assembly: AssemblyVersion(CameraTools.Plugin.VERSION)]

namespace CameraTools
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.CameraTools";
        public const string NAME = "CameraTools";
        public const string VERSION = "0.4.0";

        public static ManualLogSource Log;
        public static ConfigFile ConfigFile;
        public readonly static List<CameraPoint> CameraList = new();
        public readonly static List<CameraPath> PathList = new();
        public static CameraPoint ViewingCam
        {
            get => viewingCam;
            set
            {
                if (GameMain.mainPlayer != null && GameMain.localPlanet == null)
                {
                    if (value != null && viewingCam == null && viewingPath == null) // No view => View
                    {
                        lastPlayerUpos = GameMain.mainPlayer.uPosition;
                    }
                    else if (value == null && viewingPath == null) // View => No view
                    {
                        if (ModConfig.MovePlayerWithSpaceCamera.Value)
                            GameMain.mainPlayer.uPosition = lastPlayerUpos;
                    }
                }
                viewingCam = value;
            }
        }
        public static CameraPoint LastViewCam { get; set; } = null;
        public static CameraPath ViewingPath
        {
            get => viewingPath;
            set
            {
                if (GameMain.mainPlayer != null && GameMain.localPlanet == null)
                {
                    if (value != null && viewingCam == null && viewingPath == null) // No view => View
                    {
                        lastPlayerUpos = GameMain.mainPlayer.uPosition;
                    }
                    else if (value == null && viewingCam == null) // View => No view
                    {
                        if (ModConfig.MovePlayerWithSpaceCamera.Value)
                            GameMain.mainPlayer.uPosition = lastPlayerUpos;
                    }
                }
                viewingPath = value;
            }
        }
        public static FreePointPoser FreePoser { get; set; } = null;

        static CameraPoint viewingCam;
        static CameraPath viewingPath;
        static VectorLF3 lastPlayerUpos;
        Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            ConfigFile = Config;
            FreePoser = new FreePointPoser();

            // Add converter to support VectorLF3 (double) for ConfigEntry
            // https://github.com/BepInEx/BepInEx/blob/master/Runtimes/Unity/BepInEx.Unity.Mono/UnityTomlTypeConverters.cs
            var jsonConverter = new TypeConverter
            {
                ConvertToString = (obj, type) => JsonUtility.ToJson(obj),
                ConvertToObject = (str, type) => JsonUtility.FromJson(type: type, json: str)
            };
            TomlTypeConverter.AddConverter(typeof(VectorLF3), jsonConverter);

            ModConfig.LoadConfig(Config);
            ModConfig.LoadList(Config, CameraList, PathList);
            UIWindow.LoadWindowPos();
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));
            LookTarget.OnAwake();
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
            LookTarget.OnDestory();
        }

        public void OnGUI()
        {
            UIWindow.OnGUI();
        }

        public void Update()
        {
            // UICursor.BeginCursorDetermine will reset cursor, need to set the cursor before rendering
            if (UIWindow.CanResize) UICursor.SetCursor(ECursor.TargetIn);
            LookTarget.OnUpdate();
        }

        public void LateUpdate()
        {
            if (VFInput.escape)
            {
                // Exit modfiy camera mode
                UIWindow.OnEsc();
            }

            if (ModConfig.CameraListWindowShortcut.Value.IsDown())
            {
                UIWindow.ToggleCameraListWindow();
            }

            if (ModConfig.CameraPathWindowShortcut.Value.IsDown())
            {
                UIWindow.TogglePathConfigWindow();
            }

            if (ModConfig.ToggleLastCameraShortcut.Value.IsDown())
            {
                if (ViewingCam != null)
                {
                    LastViewCam = ViewingCam;
                    ViewingCam = null;
                }
                else
                {
                    ViewingCam = LastViewCam;
                }
            }

            if (ModConfig.CycyleNextCameraShortcut.Value.IsDown())
            {
                ViewingCam = FindNextAvailableCam();
            }

            if (ViewingPath != null)
            {
                ViewingPath.OnLateUpdate();
            }

            if (ModConfig.PlayCurrentPathShortcut.Value.IsDown() && UIWindow.EditingPath != null)
            {
                UIWindow.EditingPath.TogglePlayButton();
            }
        }

        static CameraPoint FindNextAvailableCam()
        {
            if (CameraList.Count == 0) return null;
            if (CameraList.Count == 1)
            {
                if (CameraList[0].CanView) return CameraList[0];
                return null;
            }
            if (ViewingCam == null)
            {
                if (CameraList[0].CanView) return CameraList[0];
            }
            int startIndex = ViewingCam == null ? 0 : ViewingCam.Index;
            int index = startIndex;
            int loop = 0;
            do
            {
                index = (index + 1) % CameraList.Count;
                if (CameraList[index].CanView) return CameraList[index];
            } while (index != startIndex && loop++ < 1000);
            return null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameCamera), "FrameLogic")]
        static void FrameLogic()
        {
            if (ViewingCam != null)
            {
                if (FreePoser.Enabled && UIWindow.EditingCam == ViewingCam) FreePoser.Calculate(ref ViewingCam.CamPose);
                ViewingCam.ApplyToCamera(GameCamera.main);
            }
            else if (ViewingPath != null)
            {
                ViewingPath.ApplyToCamera(GameCamera.main);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ABN_MechaPosition), nameof(ABN_MechaPosition.OnGameTick))]
        static bool ABN_MechaPosition_Prefix()
        {
            // Skip position check so it will not trigger when mecha move along with space camera
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GetInput))]
        static bool GetInput_Prefix(PlayerController __instance)
        {
            // Disable mecha movement input when free cam adjust mode is activated
            if (FreePoser.Enabled && UIWindow.EditingCam == ViewingCam)
            {
                __instance.input0 = Vector4.zero;
                __instance.input1 = Vector4.zero;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.GameTick))]
        static bool DsiableUposUpdate()
        {
            // Disable force & upos update when ovweriting mecha position in space
            return GameMain.localPlanet != null || !ModConfig.MovePlayerWithSpaceCamera.Value || (ViewingCam == null && ViewingPath == null);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerAction_Inspect), nameof(PlayerAction_Inspect.GameTick))]
        static bool PlayerAction_Inspect_Prefix(PlayerAction_Inspect __instance)
        {
            if (ViewingPath != null && ViewingPath.IsPlaying && CameraPath.HideGUI)
            {
                __instance.hoveringEntityId = 0;
                __instance.hoveringEnemyId = 0;
                __instance.hoveringEnemyClusterId = 0;
                __instance.hoveringEnemyFormId = 0;
                __instance.hoveringEnemyPortId = 0;
                __instance.hoveringEnemyAstroId = 0;
                __instance.hoveringTooFar = false;
                __instance.hoveringPrebuildTooFar = false;
                __instance.hoveringPrebuildId = 0;
                __instance.InspectNothing();
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControlGizmo), nameof(PlayerControlGizmo.OnOutlineDraw))]
        static bool OnOutlineDraw_Prefix(PlayerControlGizmo __instance)
        {
            if (ViewingPath != null && ViewingPath.IsPlaying && CameraPath.HideGUI)
            {
                __instance._tmp_outline_local_objcnt = 0;
                __instance._tmp_outline_local_pos = Vector3.zero;
                return false;
            }
            return true;
        }
    }
}
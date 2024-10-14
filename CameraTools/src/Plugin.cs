using BepInEx;
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
        public const string VERSION = "0.2.0";

        public static ManualLogSource Log;
        public static ConfigFile ConfigFile;
        public readonly static List<CameraPoint> CameraList = new();
        public readonly static List<CameraPath> PathList = new();
        public static CameraPoint ViewingCam { get; set; } = null;
        public static CameraPoint LastViewCam { get; set; } = null;
        public static CameraPath ViewingPath { get; set; } = null;
        public static FreePointPoser FreePoser { get; set; } = null;
        

        Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            ConfigFile = Config;
            FreePoser = new FreePointPoser();
            ModConfig.LoadConfig(Config);
            ModConfig.LoadList();
            UIWindow.LoadWindowPos();
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
        }

        public void OnGUI()
        {
            UIWindow.OnGUI();
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
    }
}

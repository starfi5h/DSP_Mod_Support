using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
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
        public const string VERSION = "0.1.0";

        public static ManualLogSource Log;
        public static ConfigFile ConfigFile;
        public readonly static List<FixedCamera> CameraList = new();
        public readonly static List<CameraPath> PathList = new();
        public static FixedCamera ViewingCam { get; set; } = null;
        public static CameraPath ViewingPath { get; set; } = null;

        Harmony harmony;
        //float timelapseTimer;

        public void Awake()
        {
            Log = Logger;
            ConfigFile = Config;
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Plugin));
            ModConfig.LoadConfig(Config);
            ModConfig.LoadList();
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ABN_MechaPosition), nameof(ABN_MechaPosition.OnGameTick))]
        static bool ABN_MechaPosition_Prefix()
        {
            // Skip position check so it will not trigger when mecha move along with space camera
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameCamera), "FrameLogic")]
        static void FrameLogic()
        {
            if (ViewingCam != null)
            {
                ViewingCam.ApplyToCamera(GameCamera.main);
            }
            else if (ViewingPath != null)
            {
                ViewingPath.ApplyToCamera(GameCamera.main);
            }
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

            if (ViewingPath != null)
            {
                ViewingPath.OnLateUpdate();
            }

            /*
            if (GeneralConfig.TimeInterval.Value > 0)
            {
                timelapseTimer += Time.deltaTime;
                if (timelapseTimer > GeneralConfig.TimeInterval.Value)
                {
                    CaptureScreenShots();
                    timelapseTimer = 0;
                }
            }
            */
        }

        /*
        static void CaptureScreenShots()
        {
            bool captured = false;
            foreach (var cam in CameraList)
            {
                if (cam.CanCapture())
                {
                    captured = true;
                    break;
                }
            }
            if (!captured) return;

            var folderPath = GeneralConfig.ScreenshotFolderPath.Value;
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                folderPath = GameConfig.overrideDocumentFolder + GameConfig.gameName + "/TimeLapse/";
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            }
            if (!Directory.Exists(folderPath))
            {
                Log.LogWarning("Directory doesn't exisit! " + folderPath);
                return;
            }

            var position = GameCamera.main.transform.position;
            var rotation = GameCamera.main.transform.rotation;
            var fieldOfView = GameCamera.main.fieldOfView;
            var nearClipPlane = GameCamera.main.nearClipPlane;
            var farClipPlane = GameCamera.main.farClipPlane;

            foreach (var cam in CameraList)
            {
                if (cam.CanCapture())
                {
                    try
                    {
                        Log.LogDebug($"Capturing [{cam.Index}] " + cam.GetInfo());
                        cam.ApplyToCamera(GameCamera.main);
                        UniverseSimulatorGameTick(cam);
                        CaptureScreenShot(GeneralConfig.ResolutionWidth.Value, GeneralConfig.ResolutionHeight.Value, GeneralConfig.JpgQuality.Value);
                        cam.SaveScreenShot(folderPath);
                    }
                    catch (System.Exception ex)
                    {
                        Log.LogError($"Capturing [{cam.Index}] error " + cam.GetInfo());
                        Log.LogError(ex);
                    }
                }
            }

            // Known issue: shadow may flicker during capturing
            GameCamera.main.transform.position = position;
            GameCamera.main.transform.rotation = rotation;
            GameCamera.main.fieldOfView = fieldOfView;
            GameCamera.main.nearClipPlane = nearClipPlane;
            GameCamera.main.farClipPlane = farClipPlane;
            UniverseSimulatorGameTick();

            if (GameMain.data.localPlanet != null && GameMain.data.localPlanet.factoryLoaded)
            {
                GameMain.data.localPlanet.factoryModel.DrawInstancedBatches(GameCamera.main, true);
            }
            GameMain.data.OnDraw(Time.frameCount, true, true);
            if (GameMain.data.spaceSector != null)
            {
                GameMain.data.spaceSector.model.DrawInstancedBatches(GameCamera.main, true);
            }
            GameCamera.main.Render();
        }

        public static void CaptureScreenShot(int width, int height, int quality = 90)
        {
            if (GameMain.data == null)
            {
                return;
            }
            try
            {
                var stopwatch = new HighStopwatch();
                stopwatch.Begin();
                Camera camera = GameCamera.main;
                RenderTexture renderTexture = new RenderTexture(width, height, 24);
                try
                {
                    camera.targetTexture = renderTexture;
                    camera.cullingMask = (int)GameCamera.instance.gameLayerMask;
                    if (GameMain.data.localPlanet != null && GameMain.data.localPlanet.factoryLoaded)
                    {
                        GameMain.data.localPlanet.factoryModel.DrawInstancedBatches(camera, true);
                    }
                    GameMain.data.OnDraw(Time.frameCount, true, true);
                    if (GameMain.data.spaceSector != null)
                    {
                        GameMain.data.spaceSector.model.DrawInstancedBatches(camera, true);
                    }
                    camera.Render();
                }
                catch
                {
                    Log.LogError("CaptureScreenShot Failed #1");
                }
                Log.LogDebug("Render: " + stopwatch.duration);

                stopwatch.Begin();
                Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
                RenderTexture active = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture2D.ReadPixels(new Rect(0f, 0f, (float)renderTexture.width, (float)renderTexture.height), 0, 0);
                texture2D.Apply();
                Log.LogDebug("ReadPixels: " + stopwatch.duration);

                stopwatch.Begin();
                RenderTexture.active = active;
                GameMain.data.screenShot = texture2D.EncodeToJPG(quality);
                //GameMain.data.screenShot = texture2D.EncodeToPNG();
                camera.targetTexture = null;
                Object.Destroy(texture2D);
                renderTexture.Release();
                Object.Destroy(renderTexture);
                Log.LogDebug("EncodeToJPG: " + stopwatch.duration);
            }
            catch
            {
                GameMain.data.screenShot = new byte[0];
                Log.LogError("CaptureScreenShot Failed #0");
            }
        }
        */
    }
}
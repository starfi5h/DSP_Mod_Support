using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CameraTools
{
    public static class ModConfig
    {
        public static ConfigEntry<KeyboardShortcut> CameraListWindowShortcut;
        public static ConfigEntry<KeyboardShortcut> CameraPathWindowShortcut;
        public static ConfigEntry<KeyboardShortcut> ToggleLastCameraShortcut;
        public static ConfigEntry<KeyboardShortcut> CycyleNextCameraShortcut;
        public static ConfigEntry<bool> MovePlayerWithSpaceCamera;

        /*
        public static ConfigEntry<float> TimeInterval;
        public static ConfigEntry<int> ResolutionWidth;
        public static ConfigEntry<int> ResolutionHeight;
        public static ConfigEntry<int> JpgQuality;
        public static ConfigEntry<string> ScreenshotFolderPath;
        */

        //Internal
        public static ConfigEntry<int> CameraListCount;
        public static ConfigEntry<int> PathListCount;
        public static ConfigEntry<Vector2> PosModConfigWindow;
        public static ConfigEntry<Vector2> PosCameraListWindow;
        public static ConfigEntry<Vector2> PosCameraConfigWindow;
        public static ConfigEntry<Vector2> PosPathListWindow;
        public static ConfigEntry<Vector2> PosPathConfigWindow;

        public static void LoadConfig(ConfigFile config)
        {
            CameraListWindowShortcut = config.Bind("- KeyBind -", "Camera List Window", new KeyboardShortcut(KeyCode.F5, KeyCode.LeftAlt),
                "Hotkey to open the camera list window");
            CameraPathWindowShortcut = config.Bind("- KeyBind -", "Camera Path Window", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftAlt),
                "Hotkey to open the camera path config window");
            ToggleLastCameraShortcut = config.Bind("- KeyBind -", "Toggle Last Cam", new KeyboardShortcut(KeyCode.None),
                "Hotkey to swith between the last viewing camera and the main camera");
            CycyleNextCameraShortcut = config.Bind("- KeyBind -", "Cycyle To Next Cam", new KeyboardShortcut(KeyCode.None),
                "Hotkey to view the next available camera in the list");

            MovePlayerWithSpaceCamera = config.Bind("- General -", "Move Player With Space Camera", false,
                "Move mecha position to the space camera so the star image doesn't distort");

            /*
            TimeInterval = config.Bind("- TimeLapse -", "Time Interval", 60f,
                "Screenshot time interval in seconds");
            ResolutionWidth = config.Bind("- TimeLapse -", "Resolution Width", 1920,
                "Resolution width of timelapse screeshots");
            ResolutionHeight = config.Bind("- TimeLapse -", "Resolution Height", 1080,
                "Resolution height of timelapse screeshots");
            JpgQuality = config.Bind("- TimeLapse -", "Jpg Quality", 90,
                new ConfigDescription("Quality of screeshots", new AcceptableValueRange<int>(0, 100)));
            ScreenshotFolderPath = config.Bind("- TimeLapse -", "Screenshot Folder Path", "",
                @"The folder path to save screenshots. If not set, it will use the same folder of DSP user data. (Documents\Dyson Sphere Program\TimeLapse)");
            */

            CameraListCount = config.Bind("internal", "CameraListCount", 0);
            PathListCount = config.Bind("internal", "PathListCount", 0);
            PosModConfigWindow = config.Bind("internal", "RectModConfigWindow", new Vector2(20f, 20f));
            PosCameraListWindow = config.Bind("intenral", "RectCameraListWindow", new Vector2(20f, 250f));
            PosCameraConfigWindow = config.Bind("intenral", "RectCameraConfigWindow", new Vector2(320f, 250f));
            PosPathListWindow = config.Bind("intenral", "RectPathListWindow", new Vector2(900f, 350f));
            PosPathConfigWindow = config.Bind("intenral", "RectPathConfigWindow", new Vector2(1200f, 350f));
        }

        public static void LoadList(ConfigFile configFile, List<CameraPoint> cameraList, List<CameraPath> pathList)
        {
            Plugin.Log.LogDebug("Load config file " + configFile.ConfigFilePath);

            int cameraListCount = configFile.Bind("internal", "CameraListCount", 0).Value;
            cameraList.Clear();
            for (int i = 0; i < cameraListCount; i++)
            {
                var cam = new CameraPoint(i);
                cameraList.Add(cam);
                cam.Import(configFile);
            }
            Plugin.Log.LogDebug("Load camera: " + Plugin.CameraList.Count);

            int pathListCount = configFile.Bind("internal", "PathListCount", 0).Value;
            pathList.Clear();
            for (int i = 0; i < pathListCount; i++)
            {
                var path = new CameraPath(i);
                pathList.Add(path);
                path.Import(configFile);
            }
            Plugin.Log.LogDebug("Load path: " + Plugin.PathList.Count);
        }

        public static void SaveList(ConfigFile configFile, List<CameraPoint> cameraList, List<CameraPath> pathList)
        {
            Plugin.Log.LogDebug("Save config file " + configFile.ConfigFilePath);
            configFile.Bind("internal", "CameraListCount", 0).Value = cameraList.Count;
            foreach (var cam in cameraList) cam.Export(configFile);
            configFile.Bind("internal", "PathListCount", 0).Value = pathList.Count;
            foreach (var path in pathList) path.Export(configFile);
        }

        public static void ConfigWindowFunc()
        {
            Util.AddKeyBindField(CameraListWindowShortcut);
            Util.AddKeyBindField(CameraPathWindowShortcut);
            Util.AddKeyBindField(ToggleLastCameraShortcut);
            Util.AddKeyBindField(CycyleNextCameraShortcut);
            Util.AddToggleField(MovePlayerWithSpaceCamera);
            if (GUILayout.Button("Reset Windows Position".Translate())) UIWindow.LoadWindowPos(true);
        }

        static string path;
        static string status = "";
        static bool importSuccessFlag;
        static readonly List<CameraPoint> cameras = new();
        static readonly List<CameraPath> paths = new();

        public static void ImportWindowFunc()
        {            
            path = GUILayout.TextField(path);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Import cfg file".Translate()))
            {
                importSuccessFlag = false;
                status = "";
                cameras.Clear();
                paths.Clear();
                try
                {
                    Plugin.Log.LogDebug("Importing " + path);
                    if (Path.GetExtension(path) != ".cfg") throw new System.ArgumentException("only support .cfg file");
                    if (!File.Exists(path)) throw new FileNotFoundException("could not found the file");                    
                    var configFile = new ConfigFile(path, false);
                    LoadList(configFile, cameras, paths);
                    importSuccessFlag = true;
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Error when importing config file!\n" + e);
                    status = e.ToString();
                }
                if (status == "")
                {
                    status = string.Format("Load {0} camera and {1} path.".Translate(), cameras.Count, paths.Count);
                    Plugin.Log.LogInfo(status);
                }
            }
            if (GUILayout.Button("Export All".Translate()))
            {
                status = "";
                try
                {
                    Plugin.Log.LogDebug("Exporting " + path);
                    if (File.Exists(path))
                    {
                        UIMessageBox.Show("Export", string.Format("Overwrite {0} ?".Translate(), path), "否".Translate(), "是".Translate(), 1, null, 
                            () => { File.Delete(path); SaveList(new ConfigFile(path, true), Plugin.CameraList, Plugin.PathList); status = "Overwirte success"; });
                        return;
                    }
                    SaveList(new ConfigFile(path, true), Plugin.CameraList, Plugin.PathList);
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Error when exporting config file!\n" + e);
                    status = e.ToString();
                }
                if (status == "")
                {
                    status = string.Format("Export {0} camera and {1} path.".Translate(), Plugin.CameraList.Count, Plugin.PathList.Count);
                    Plugin.Log.LogInfo(status);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(status);

            GUILayout.BeginHorizontal();
            if (importSuccessFlag && GUILayout.Button(string.Format("Import All {0} Camera".Translate(), cameras.Count)))
            {
                int index = Plugin.CameraList.Count;
                foreach (var cam in cameras)
                {
                    cam.Index = index;
                    Plugin.CameraList.Add(cam);
                    cam.Export();
                }
                Plugin.Log.LogDebug($"Add {index - CameraListCount.Value} camera to list");
                CameraListCount.Value = index;
                cameras.Clear();
            }
            if (importSuccessFlag && GUILayout.Button(string.Format("Import All {0} Path".Translate(), paths.Count)))
            {
                int index = Plugin.PathList.Count;
                foreach (var path in paths)
                {
                    path.Index = index;
                    Plugin.PathList.Add(path);
                    path.Export();
                }
                Plugin.Log.LogDebug($"Add {index - PathListCount.Value} path to list");
                PathListCount.Value = index;
                paths.Clear();
            }
            GUILayout.EndHorizontal();
        }
    }
}

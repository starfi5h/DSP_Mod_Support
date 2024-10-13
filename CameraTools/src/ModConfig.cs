using BepInEx.Configuration;
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

        public static void LoadList()
        {
            Plugin.CameraList.Clear();
            for (int i = 0; i < CameraListCount.Value; i++)
            {
                var cam = new CameraPoint(i);
                Plugin.CameraList.Add(cam);
                cam.Import();
            }
            Plugin.Log.LogDebug("Load camera: " + Plugin.CameraList.Count);
            Plugin.PathList.Clear();
            for (int i = 0; i < PathListCount.Value; i++)
            {
                var path = new CameraPath(i);
                Plugin.PathList.Add(path);
                path.Import();
            }
            Plugin.Log.LogDebug("Load path: " + Plugin.PathList.Count);
        }
    }
}

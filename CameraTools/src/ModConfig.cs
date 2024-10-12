using BepInEx.Configuration;
using UnityEngine;

namespace CameraTools
{
    public static class ModConfig
    {
        public static ConfigEntry<int> CameraListCount;
        public static ConfigEntry<int> PathListCount;
        public static ConfigEntry<KeyboardShortcut> CameraListWindowShortcut;
        public static ConfigEntry<KeyboardShortcut> CameraPathWindowShortcut;
        public static ConfigEntry<bool> MovePlayerWithSpaceCamera;

        /*
        public static ConfigEntry<float> TimeInterval;
        public static ConfigEntry<int> ResolutionWidth;
        public static ConfigEntry<int> ResolutionHeight;
        public static ConfigEntry<int> JpgQuality;
        public static ConfigEntry<string> ScreenshotFolderPath;
        */

        public static void LoadConfig(ConfigFile config)
        {
            CameraListCount = config.Bind("internal", "CameraListCount", 0);
            PathListCount = config.Bind("internal", "PathListCount", 0);

            CameraListWindowShortcut = config.Bind("- KeyBind -", "Camera List Window Shortcut", new KeyboardShortcut(KeyCode.F5, KeyCode.LeftAlt),
                "Keyboard combo to open the camera list window");
            CameraPathWindowShortcut = config.Bind("- KeyBind -", "Camera Path Window Shortcut", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftAlt),
                "Keyboard combo to open the camera path config window");
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
        }

        public static void LoadList()
        {
            Plugin.CameraList.Clear();
            for (int i = 0; i < CameraListCount.Value; i++)
            {
                var cam = new FixedCamera(i);
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

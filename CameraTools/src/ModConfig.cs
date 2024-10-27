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
        public static ConfigEntry<KeyboardShortcut> RecordWindowShortcut;
        public static ConfigEntry<KeyboardShortcut> ToggleLastCameraShortcut;
        public static ConfigEntry<KeyboardShortcut> CycleNextCameraShortcut;
        public static ConfigEntry<KeyboardShortcut> PlayCurrentPathShortcut;
        public static ConfigEntry<bool> MovePlayerWithSpaceCamera;

        //Internal
        public static ConfigEntry<int> CameraListCount;
        public static ConfigEntry<int> PathListCount;
        public static ConfigEntry<Vector2> PosModConfigWindow;
        public static ConfigEntry<Vector2> PosCameraListWindow;
        public static ConfigEntry<Vector2> PosCameraConfigWindow;
        public static ConfigEntry<Vector2> PosPathListWindow;
        public static ConfigEntry<Vector2> PosPathConfigWindow;
        public static ConfigEntry<Vector2> PosTargetConfigWindow;

        public static void LoadConfig(ConfigFile config)
        {
            CameraListWindowShortcut = config.Bind("- KeyBind -", "Camera List Window", new KeyboardShortcut(KeyCode.F5, KeyCode.LeftAlt),
                "Hotkey to open the camera list window");
            CameraPathWindowShortcut = config.Bind("- KeyBind -", "Camera Path Window", new KeyboardShortcut(KeyCode.F6, KeyCode.LeftAlt),
                "Hotkey to open the camera path config window");
            RecordWindowShortcut = config.Bind("- KeyBind -", "Record Window", new KeyboardShortcut(KeyCode.F7, KeyCode.LeftAlt),
                "Hotkey to open the timelapse record window");
            ToggleLastCameraShortcut = config.Bind("- KeyBind -", "Toggle Last Cam", new KeyboardShortcut(KeyCode.None),
                "Hotkey to switch between the last viewing camera and the main camera");
            CycleNextCameraShortcut = config.Bind("- KeyBind -", "Cycle To Next Cam", new KeyboardShortcut(KeyCode.None),
                "Hotkey to view the next available camera in the list");
            PlayCurrentPathShortcut = config.Bind("- KeyBind -", "Play Current Path", new KeyboardShortcut(KeyCode.None),
                "Hotkey to toggle the editing path play button (play from start/pause/resume)");
            MovePlayerWithSpaceCamera = config.Bind("- General -", "Move Player With Space Camera", true,
                "Move mecha position to the space camera so the star image doesn't distort");

            CameraListCount = config.Bind("internal", "CameraListCount", 0);
            PathListCount = config.Bind("internal", "PathListCount", 0);
            PosModConfigWindow = config.Bind("internal", "RectModConfigWindow", new Vector2(20f, 20f));
            PosCameraListWindow = config.Bind("internal", "RectCameraListWindow", new Vector2(20f, 260f));
            PosCameraConfigWindow = config.Bind("internal", "RectCameraConfigWindow", new Vector2(320f, 260f));
            PosPathListWindow = config.Bind("internal", "RectPathListWindow", new Vector2(900f, 350f));
            PosPathConfigWindow = config.Bind("internal", "RectPathConfigWindow", new Vector2(1200f, 350f));
            PosTargetConfigWindow = config.Bind("internal", "RectTargetConfigWindow", new Vector2(900f, 350f));
        }

        public static void LoadList(ConfigFile configFile, List<CameraPoint> cameraList, List<CameraPath> pathList)
        {
            //Plugin.Log.LogDebug("Load config file " + configFile.ConfigFilePath);
            var stopwatch = new HighStopwatch();
            stopwatch.Begin();
            int cameraListCount = configFile.Bind("internal", "CameraListCount", 0).Value;
            cameraList.Clear();
            for (int i = 0; i < cameraListCount; i++)
            {
                var cam = new CameraPoint(i);
                cameraList.Add(cam);
                cam.Import(configFile);
            }
            Plugin.Log.LogDebug($"Load camera: {Plugin.CameraList.Count} time cost: {stopwatch.duration:F3}s");

            stopwatch.Begin();
            int pathListCount = configFile.Bind("internal", "PathListCount", 0).Value;
            pathList.Clear();
            for (int i = 0; i < pathListCount; i++)
            {
                var path = new CameraPath(i);
                pathList.Add(path);
                path.Import(configFile);
            }
            Plugin.Log.LogDebug($"Load path: {Plugin.PathList.Count} time cost: {stopwatch.duration:F3}s");
        }

        public static void SaveList(ConfigFile configFile, List<CameraPoint> cameraList, List<CameraPath> pathList)
        {
            //Plugin.Log.LogDebug("Save config file " + configFile.ConfigFilePath);
            configFile.Bind("internal", "CameraListCount", 0).Value = cameraList.Count;
            foreach (var cam in cameraList) cam.Export(configFile);
            configFile.Bind("internal", "PathListCount", 0).Value = pathList.Count;
            foreach (var path in pathList) path.Export(configFile);
        }

        public static void SaveEditingPath(ConfigFile configFile)
        {
            if (UIWindow.EditingPath == null) return;            
            configFile.Bind("internal", "PathListCount", 0).Value = 1;
            int pathIndex = UIWindow.EditingPath.Index;
            UIWindow.EditingPath.Index = 0;
            UIWindow.EditingPath.Export(configFile);
            UIWindow.EditingPath.Index = pathIndex;
        }

        public static void ConfigWindowFunc()
        {
            Util.AddKeyBindField(CameraListWindowShortcut);
            Util.AddKeyBindField(CameraPathWindowShortcut);
            Util.AddKeyBindField(RecordWindowShortcut);            
            Util.AddKeyBindField(ToggleLastCameraShortcut);
            Util.AddKeyBindField(CycleNextCameraShortcut);
            Util.AddKeyBindField(PlayCurrentPathShortcut);            
            Util.ConfigToggleField(MovePlayerWithSpaceCamera);
            Util.AddFloatFieldInput("Path Preview Size".Translate(), ref GizmoManager.PathMarkerSize);
            if (GUILayout.Button("Reset Windows Position".Translate())) UIWindow.LoadWindowPos(true);
        }

        static string pathInput;
        static string status = "";
        static Vector2 scrollPosition;
        static readonly List<CameraPoint> tmpCameraList = new();
        static readonly List<CameraPath> tmpPathList = new();

        public static void ImportWindowFunc()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Path".Translate(), GUILayout.MaxWidth(60));
            pathInput = GUILayout.TextField(pathInput);            
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool exportAll = false;
            bool exportCurrentPath = false;
            GUILayout.Label("Export cfg File".Translate());
            if (GUILayout.Button("Current Path".Translate())) exportCurrentPath = true;
            if (GUILayout.Button("All".Translate())) exportAll = true;
            if (exportAll || exportCurrentPath)
            {
                status = "";
                try
                {
                    Plugin.Log.LogDebug("Exporting " + pathInput);
                    string path = pathInput;
                    if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)))
                    {
                        // If directory doesn't provide, store in BepInEx\config\CameraTools\
                        path = Path.Combine(BepInEx.Paths.ConfigPath, Plugin.NAME, path);
                        Plugin.Log.LogDebug(path);
                    }
                    if (File.Exists(path))
                    {
                        UIMessageBox.Show("Export", "Overwrite ".Translate() + path + " ?", "否".Translate(), "是".Translate(), 1, null,
                            () => { 
                                File.Delete(path);
                                if (exportAll) SaveList(new ConfigFile(path, true), Plugin.CameraList, Plugin.PathList);
                                else SaveEditingPath(new ConfigFile(path, true));
                                status = "Overwrite file success"; });
                        return;
                    }
                    if (exportAll) SaveList(new ConfigFile(path, true), Plugin.CameraList, Plugin.PathList);
                    else SaveEditingPath(new ConfigFile(path, true));
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Error when exporting config file!\n" + e);
                    status = e.ToString();
                }
                if (status == "")
                {
                    status = exportAll ? 
                        string.Format("Export {0} camera and {1} path.".Translate(), Plugin.CameraList.Count, Plugin.PathList.Count) :
                        "Export 1 path.".Translate();
                    Plugin.Log.LogInfo(status);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            bool importCamera = false;
            bool importPath = false;
            GUILayout.Label("Import cfg File".Translate());
            if (GUILayout.Button("Camera".Translate())) importCamera = true;
            if (GUILayout.Button("Path".Translate())) importPath = true;
            if (importCamera || importPath)
            {
                status = "";
                tmpCameraList.Clear();
                tmpPathList.Clear();
                try
                {
                    Plugin.Log.LogDebug("Importing " + pathInput);
                    string path = pathInput;
                    if (string.IsNullOrWhiteSpace(Path.GetDirectoryName(path)))
                    {
                        // If directory doesn't provide, store in BepInEx\config\CameraTools\
                        path = Path.Combine(BepInEx.Paths.ConfigPath, Plugin.NAME, path);
                        Plugin.Log.LogDebug(path);
                    }
                    if (Path.GetExtension(path) != ".cfg") throw new System.ArgumentException("only support .cfg file");
                    if (!File.Exists(path)) throw new FileNotFoundException("could not found the file");                    
                    var configFile = new ConfigFile(path, false);
                    LoadList(configFile, tmpCameraList, tmpPathList);
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Error when importing config file!\n" + e);
                    status = e.ToString();
                }
                if (importCamera) tmpPathList.Clear();
                if (importPath) tmpCameraList.Clear();
                if (status == "")
                {
                    status = string.Format("Load {0} camera and {1} path.".Translate(), tmpCameraList.Count, tmpPathList.Count);
                    Plugin.Log.LogInfo(status);
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(status);

            int removingCamIndex = -1;
            int removingPathIndex = -1;
            foreach (var cam in tmpCameraList)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"[{cam.Index}]", GUILayout.MaxWidth(20));
                GUILayout.Label(cam.Name);
                if (GUILayout.Button("Add".Translate(), GUILayout.MaxWidth(60)))
                {
                    removingCamIndex = cam.Index;
                    cam.Index = Plugin.PathList.Count;
                    Plugin.CameraList.Add(cam);
                    cam.Export();
                    PathListCount.Value = Plugin.PathList.Count;                    
                }
                if (GUILayout.Button("Remove".Translate(), GUILayout.MaxWidth(60)))
                {
                    removingCamIndex = cam.Index;
                }
                GUILayout.EndHorizontal();
            }
            foreach (var path in tmpPathList)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"[{path.Index}]", GUILayout.MaxWidth(20));
                GUILayout.Label(path.Name);
                if (GUILayout.Button("Add".Translate(), GUILayout.MaxWidth(60)))
                {
                    removingPathIndex = path.Index;
                    path.Index = Plugin.PathList.Count;
                    Plugin.PathList.Add(path);
                    path.Export();
                    PathListCount.Value = Plugin.PathList.Count;                    
                }
                if (GUILayout.Button("Remove".Translate(), GUILayout.MaxWidth(60)))
                {
                    removingPathIndex = path.Index;
                }
                GUILayout.EndHorizontal();
            }

            if (removingCamIndex != -1)
            {
                tmpCameraList.RemoveAt(removingCamIndex);
                for (int i = 0; i < tmpCameraList.Count; i++) tmpCameraList[i].Index = i;
            }
            if (removingPathIndex != -1)
            {
                tmpPathList.RemoveAt(removingPathIndex);
                for (int i = 0; i < tmpPathList.Count; i++) tmpPathList[i].Index = i;
            }
            GUILayout.EndScrollView();
        }
    }
}

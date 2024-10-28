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
        public static ConfigEntry<string> ConfigFolderPath;

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
            ConfigFolderPath = config.Bind("- General -", "Config Folder Path", "",
                "Folder path of which config files export/import. If unset, it will use BepInEx\\config\\CameraTools\\");
            pathInput = ConfigFolderPath.Value;
            if (string.IsNullOrEmpty(pathInput)) pathInput = Path.Combine(BepInEx.Paths.ConfigPath, Plugin.NAME);

            CameraListCount = config.Bind("internal", "CameraListCount", 0);
            PathListCount = config.Bind("internal", "PathListCount", 0);
            PosModConfigWindow = config.Bind("internal", "RectModConfigWindow", new Vector2(320f, 20f));
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

        public static void SaveCamera(ConfigFile configFile, CameraPoint cameraPoint)
        {
            if (cameraPoint == null) return;
            configFile.Bind("internal", "CameraListCount", 0).Value = 1;
            int pathIndex = cameraPoint.Index;
            cameraPoint.Index = 0;
            cameraPoint.Export(configFile);
            cameraPoint.Index = pathIndex;
        }

        public static void SavePath(ConfigFile configFile, CameraPath cameraPath)
        {
            if (cameraPath == null) return;            
            configFile.Bind("internal", "PathListCount", 0).Value = 1;
            int pathIndex = cameraPath.Index;
            cameraPath.Index = 0;
            cameraPath.Export(configFile);
            cameraPath.Index = pathIndex;
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
            Util.AddFloatFieldInput("Path Preview Size".Translate(), ref GizmoManager.PathMarkerSize, 1f);
            if (GUILayout.Button("Reset Windows Position".Translate())) UIWindow.LoadWindowPos(true);
        }

        static string pathInput;
        static string statusText = "";
        static Vector2 scrollPosition;
        static readonly List<CameraPoint> tmpCameraList = new();
        static readonly List<CameraPath> tmpPathList = new();
        static string[] files = new string[0];

        public static void LoadFilesInFolder()
        {
            try
            {
                files = Directory.GetFiles(pathInput, "*.cfg");
            }
            catch (System.Exception ex)
            {
                statusText = "Error when loading dir: " + ex.Message;
                Plugin.Log.LogWarning(statusText);
                files = new string[0];
            }
        }

        public static void ImportWindowFunc()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginHorizontal();
            var input = Util.AddTextFieldInput("Folder".Translate(), pathInput);
            if (!string.IsNullOrWhiteSpace(input))
            {
                try
                {
                    Directory.CreateDirectory(input);
                    if (Directory.Exists(input))
                    {
                        pathInput = input;
                        ConfigFolderPath.Value = pathInput;
                        LoadFilesInFolder();
                    }
                }
                catch (System.Exception ex)
                {
                    statusText = ex.Message;
                    Plugin.Log.LogWarning(statusText);
                }
            }
            GUILayout.EndHorizontal();

            ExportHeader();

            GUILayout.Label(statusText);

            TempContentList();

            FileList();

            GUILayout.EndScrollView();
        }

        static void ExportHeader()
        {
            GUILayout.BeginHorizontal();
            string name = "";
            bool exportCurrentCam = false;
            bool exportCurrentPath = false;
            GUILayout.Label("Export File".Translate());
            if (GUILayout.Button("Current Cam".Translate()))
            {
                if (UIWindow.EditingCam != null)
                {
                    name = UIWindow.EditingCam.Name;
                    exportCurrentCam = true;
                }
                else
                {
                    statusText = "No editing camera!";
                }
            }
            if (GUILayout.Button("Current Path".Translate()))
            {
                if (UIWindow.EditingPath != null)
                {
                    name = UIWindow.EditingPath.Name;
                    exportCurrentPath = true;
                }
                else
                {
                    statusText = "No editing camera path!";
                }
            }
            if (exportCurrentCam || exportCurrentPath)
            {
                statusText = "";
                try
                {
                    string filePath = Path.Combine(pathInput, name+".cfg");
                    statusText = "Exporting " + filePath;
                    Plugin.Log.LogInfo(statusText);

                    if (File.Exists(filePath))
                    {
                        UIMessageBox.Show("Export", "Overwrite ".Translate() + filePath + " ?", "否".Translate(), "是".Translate(), 1, null,
                            () => {
                                File.Delete(filePath);
                                if (exportCurrentCam) SaveCamera(new ConfigFile(filePath, true), UIWindow.EditingCam);
                                if (exportCurrentPath) SavePath(new ConfigFile(filePath, true), UIWindow.EditingPath);
                                statusText = "Overwrite " + filePath;
                                Plugin.Log.LogInfo(statusText);
                                LoadFilesInFolder();
                            });
                        return;
                    }
                    if (exportCurrentCam)
                    {
                        SaveCamera(new ConfigFile(filePath, true), UIWindow.EditingCam);
                        LoadFilesInFolder();
                    }
                    if (exportCurrentPath)
                    {
                        SavePath(new ConfigFile(filePath, true), UIWindow.EditingPath);
                        LoadFilesInFolder();
                    }
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Error when exporting config file!\n" + e);
                    statusText = e.ToString();
                }
            }
            GUILayout.EndHorizontal();
        }

        static void TempContentList()
        {
            if (tmpCameraList.Count + tmpPathList.Count == 0) return;

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Imported Content".Translate());
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
            GUILayout.EndVertical();

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
        }
    
        static void FileList()
        {
            int importingIndex = -1;
            int removingIndex = -1;
            for (int i = 0; i < files.Length; i++)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label(Path.GetFileName(files[i]));
                if (GUILayout.Button("Import".Translate(), GUILayout.MaxWidth(60)))
                {
                    importingIndex = i;
                }
                if (GUILayout.Button("Remove".Translate(), GUILayout.MaxWidth(60)))
                {
                    removingIndex = i;
                }
                GUILayout.EndHorizontal();
            }
            if (importingIndex >= 0)
            {
                statusText = "";
                tmpCameraList.Clear();
                tmpPathList.Clear();
                try
                {
                    string filePath = files[importingIndex];
                    Plugin.Log.LogInfo("Importing " + filePath);
                    if (!File.Exists(filePath)) throw new FileNotFoundException("could not found the file");
                    var configFile = new ConfigFile(filePath, false);
                    LoadList(configFile, tmpCameraList, tmpPathList);
                    statusText = $"Import {tmpCameraList.Count} cam and {tmpPathList.Count} path from {Path.GetFileName(filePath)}";
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Error when importing config file!\n" + e);
                    statusText = e.ToString();
                }
            }
            if (removingIndex >= 0)
            {
                try
                {
                    string filePath = files[removingIndex];
                    statusText = "Removing " + filePath;
                    Plugin.Log.LogInfo(statusText);
                    File.Delete(filePath);
                    LoadFilesInFolder();
                }
                catch (System.Exception e)
                {
                    Plugin.Log.LogError("Error when removing config file!\n" + e);
                    statusText = e.ToString();
                }
            }
        }
    }
}

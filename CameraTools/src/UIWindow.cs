using UnityEngine;
using System;

namespace CameraTools
{
    static class UIWindow
    {
        private static Rect modConfigWindow = new(20f, 20f, 300f, 200f);
        private static Rect cameraListWindow = new(20f, 250f, 300f, 240f);
        private static Rect cameraConfigWindow = new(320f, 250f, 300f, 365f);
        private static Rect pathListWindow = new(900f, 350f, 300f, 240f);
        private static Rect pathConfigWindow = new(1200f, 350f, 300f, 350f);

        public static CameraPoint EditingCam { get; set; } = null;
        public static CameraPath EditingPath { get; private set; } = null;
        public static int lastEditingPathIndex = 0;

        static bool cameraListWindowActivated = true;
        static bool pathListWindowActivated = false;
        static bool modConfigWindowActivated = false;

        public static void LoadWindowPos(bool reset = false)
        {
            Util.SetWindowPos(ref modConfigWindow, ModConfig.PosModConfigWindow, reset);
            Util.SetWindowPos(ref cameraListWindow, ModConfig.PosCameraListWindow, reset);
            Util.SetWindowPos(ref cameraConfigWindow, ModConfig.PosCameraConfigWindow, reset);
            Util.SetWindowPos(ref pathListWindow, ModConfig.PosPathListWindow, reset);
            Util.SetWindowPos(ref pathConfigWindow, ModConfig.PosPathConfigWindow, reset);
            if (reset) SaveWindowPos();
        }

        public static void SaveWindowPos()
        {
            ModConfig.PosModConfigWindow.Value = modConfigWindow.position;
            ModConfig.PosCameraListWindow.Value = cameraListWindow.position;
            ModConfig.PosCameraConfigWindow.Value = cameraConfigWindow.position;
            ModConfig.PosPathListWindow.Value = pathListWindow.position;
            ModConfig.PosPathConfigWindow.Value = pathConfigWindow.position;
        }

        public static void OnEsc()
        {
            Plugin.ViewingCam = null;
            Plugin.ViewingPath = null;            
            Plugin.FreePoser.Enabled = false;
        }

        public static void ToggleCameraListWindow()
        {
            cameraListWindowActivated = !cameraListWindowActivated;
            if (!cameraListWindowActivated)
            {
                foreach (var cam in Plugin.CameraList) cam.Export();
                ModConfig.CameraListCount.Value = Plugin.CameraList.Count;
                SaveWindowPos();
            }
        }

        public static void TogglePathConfigWindow()
        {
            if (EditingPath == null)
            {
                if (Plugin.PathList.Count == 0)
                {
                    EditingPath = new CameraPath(0);
                    EditingPath.Name = GameMain.localPlanet?.displayName ?? GameMain.localStar.displayName ?? "path";
                    Plugin.PathList.Add(EditingPath);
                    ModConfig.PathListCount.Value = 1;
                }
                EditingPath = Plugin.PathList[lastEditingPathIndex % Plugin.PathList.Count];
            }
            else
            {
                lastEditingPathIndex = EditingPath.Index;
                EditingPath.Export();
                EditingPath = null;
                Plugin.ViewingPath = null;
                SaveWindowPos();
            }
        }

        public static void TogglePathListWindow()
        {
            pathListWindowActivated = !pathListWindowActivated;
            if (!pathListWindowActivated)
            {
                foreach (var path in Plugin.PathList) path.Export();
                ModConfig.PathListCount.Value = Plugin.PathList.Count;
                SaveWindowPos();
            }
        }

        public static void OnGUI()
        {
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null || (EditingPath != null && EditingPath.HideGUI && EditingPath.IsPlaying))
            {
                return;
            }

            if (modConfigWindowActivated)
            {
                modConfigWindow = GUI.Window(1307890670, modConfigWindow, ModConfigWindowFunc, "Mod Config".Translate());
                EatInputInRect(modConfigWindow);
            }

            if (cameraListWindowActivated)
            {
                var title = "Camera List".Translate();
                if (Plugin.ViewingCam != null) title += $" [{Plugin.ViewingCam.Index}]";
                cameraListWindow = GUI.Window(1307890671, cameraListWindow, CameraListWindowFunc, title);
                EatInputInRect(cameraListWindow);
            }

            if (EditingCam != null)
            {
                cameraConfigWindow = GUI.Window(1307890672, cameraConfigWindow, CamConfigWindowFunc, "Camera Config".Translate());
                EatInputInRect(cameraConfigWindow);
            }

            if (EditingPath != null)
            {
                pathConfigWindow = GUI.Window(1307890673, pathConfigWindow, PathConfigWindowFunc, "Path Config".Translate() + $" [{EditingPath.Index}]");
                EatInputInRect(pathConfigWindow);
            }

            if (pathListWindowActivated)
            {
                pathListWindow = GUI.Window(1307890674, pathListWindow, PathListWindowFunc, "Path List".Translate());
                EatInputInRect(pathListWindow);
            }
        }

        static void ModConfigWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(cameraConfigWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X")) modConfigWindowActivated = false;
            GUILayout.EndArea();
            Util.AddKeyBindField(ModConfig.CameraListWindowShortcut);
            Util.AddKeyBindField(ModConfig.CameraPathWindowShortcut);
            Util.AddKeyBindField(ModConfig.ToggleLastCameraShortcut);
            Util.AddKeyBindField(ModConfig.CycyleNextCameraShortcut);
            Util.AddToggleField(ModConfig.MovePlayerWithSpaceCamera);
            if (GUILayout.Button("Reset Windows Position".Translate())) LoadWindowPos(true);
            GUI.DragWindow();
        }

        static void CamConfigWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(cameraConfigWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X"))
            {
                EditingCam.Export();
                EditingCam = null;
                Plugin.FreePoser.Enabled = false;
                return;
            }
            GUILayout.EndArea();

            EditingCam?.ConfigWindowFunc();
            GUI.DragWindow();
        }

        static void PathConfigWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(pathConfigWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X"))
            {
                TogglePathConfigWindow();
                pathListWindowActivated = false;
            }
            GUILayout.EndArea();

            EditingPath?.ConfigWindowFunc();
            GUI.DragWindow();
        }

        static Vector2 scrollPositionPathList = new(100, 100);

        static void PathListWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(pathListWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X") || EditingPath == null)
            {
                TogglePathListWindow();
                return;
            }
            GUILayout.EndArea();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"[{EditingPath.Index}]", GUILayout.MaxWidth(20));
            string name = GUILayout.TextField(EditingPath.Name);
            if (name != EditingPath.Name)
            {
                EditingPath.Name = name;
            }
            GUILayout.EndHorizontal();

            int removingIndex = -1;
            scrollPositionPathList = GUILayout.BeginScrollView(scrollPositionPathList);
            foreach (var path in Plugin.PathList)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"[{path.Index}]", GUILayout.MaxWidth(20));
                GUILayout.Label(path.Name);
                if (GUILayout.Button("Load".Translate(), GUILayout.MaxWidth(60)))
                {
                    EditingPath = path;
                }
                if (GUILayout.Button("Remove".Translate(), GUILayout.MaxWidth(60)))
                {
                    removingIndex = path.Index;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (removingIndex != -1 && Plugin.PathList.Count > 1)
            {
                if (removingIndex == EditingPath.Index)
                {
                    // Set editing path to the next in the list
                    EditingPath = Plugin.PathList[(removingIndex + 1) % (Plugin.PathList.Count - 1)];
                }
                Plugin.Log.LogDebug("Remove Path " + removingIndex);
                Plugin.PathList.RemoveAt(removingIndex);
                for (int i = 0; i < Plugin.PathList.Count; i++)
                {
                    Plugin.PathList[i].Index = i;
                    Plugin.PathList[i].Export();
                }
                ModConfig.PathListCount.Value = Plugin.PathList.Count;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Path".Translate()))
            {
                int index = Plugin.PathList.Count;
                Plugin.Log.LogDebug("Add Path " + index);                
                EditingPath = new CameraPath(index);
                EditingPath.Name = (GameMain.localPlanet?.displayName ?? GameMain.localStar.displayName ?? "space") + "-" + index;
                EditingPath.Export();
                Plugin.PathList.Add(EditingPath);
                ModConfig.PathListCount.Value = Plugin.PathList.Count;
            }
            if (GUILayout.Button("Camera List".Translate()))
            {
                ToggleCameraListWindow();
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        static Vector2 scrollPositionCameraList = new(100, 100);

        static void CameraListWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(cameraConfigWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X"))
            {
                EditingCam = null;
                ToggleCameraListWindow();
                return;
            }
            GUILayout.EndArea();

            int removingIndex = -1;
            int upIndex = -1;
            int downIndex = -1;
            scrollPositionCameraList = GUILayout.BeginScrollView(scrollPositionCameraList);
            foreach (var camera in Plugin.CameraList)
            {
                GUILayout.BeginVertical(UnityEngine.GUI.skin.box);
                {
                    // Title
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(camera.Name);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(camera.GetInfo());
                    GUILayout.EndHorizontal();

                    /*
                    // Enable recording
                    GUILayout.BeginHorizontal();
                    tmpBool = camera.EnableRecording;
                    if (tmpBool != GUILayout.Toggle(tmpBool, "Enable Record".Translate()))
                    {
                        camera.EnableRecording = !camera.EnableRecording;
                    }
                    GUILayout.Label(" (" + camera.GetStatus() + ")");
                    GUILayout.EndHorizontal();
                    */

                    // View, Edit, Remove
                    GUILayout.BeginHorizontal();
                    bool isViewing = Plugin.ViewingCam == camera;
                    if (GUILayout.Button(isViewing ? "[Viewing]".Translate() : "View".Translate()))
                    {
                        if (isViewing) Plugin.ViewingCam = null;
                        else if (!camera.CanView) UIRealtimeTip.Popup("Camera type mismatch to current environment!".Translate());
                        else
                        {
                            Plugin.ViewingCam = camera;
                            Plugin.LastViewCam = camera;
                        }
                    }
                    if (EditingCam == camera)
                    {
                        if (GUILayout.Button("↑")) upIndex = camera.Index;
                        if (GUILayout.Button("↓")) downIndex = camera.Index;
                    }
                    else
                    {
                        if (GUILayout.Button("Edit".Translate())) EditingCam = camera;
                    }
                    if (GUILayout.Button("Remove".Translate()))
                    {
                        removingIndex = camera.Index;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }
            if (removingIndex != -1)
            {
                RemoveCamera(removingIndex);
            }
            if (upIndex >= 1)
            {
                SwapCamIndex(upIndex, upIndex - 1);
            }
            if (downIndex >= 0 && (downIndex + 1) < Plugin.CameraList.Count)
            {
                SwapCamIndex(downIndex, downIndex + 1);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Camera".Translate()))
            {
                Plugin.Log.LogDebug("Add Cam " + Plugin.CameraList.Count);
                var cam = new CameraPoint(Plugin.CameraList.Count);
                if (GameMain.localPlanet != null) cam.SetPlanetCamera();
                else cam.SetSpaceCamera();
                Plugin.CameraList.Add(cam);
                cam.Export();
                ModConfig.CameraListCount.Value = Plugin.CameraList.Count;
            }
            if (GUILayout.Button("Path".Translate()))
            {
                TogglePathConfigWindow();
            }
            if (GUILayout.Button("Config".Translate()))
            {
                modConfigWindowActivated = !modConfigWindowActivated;
            }
            GUILayout.EndHorizontal();
            GUI.DragWindow();
        }

        public static void RemoveCamera(int index)
        {
            if (index < 0 || index >= Plugin.CameraList.Count) return;
            Plugin.Log.LogDebug("Remove Cam " + index);
            if (Plugin.CameraList[index] == Plugin.ViewingCam) Plugin.ViewingCam = null;
            if (Plugin.CameraList[index] == EditingCam) EditingCam = null;
            Plugin.CameraList.RemoveAt(index);

            for (int i = 0; i < Plugin.CameraList.Count; i++)
            {
                Plugin.CameraList[i].Index = i;
                Plugin.CameraList[i].Export();
            }
            ModConfig.CameraListCount.Value = Plugin.CameraList.Count;
        }

        public static void SwapCamIndex(int a, int b)
        {
            var tmp = Plugin.CameraList[a];
            Plugin.CameraList[a] = Plugin.CameraList[b];
            Plugin.CameraList[b] = tmp;
            Plugin.CameraList[a].Index = a;
            Plugin.CameraList[b].Index = b;
            Plugin.CameraList[a].Export();
            Plugin.CameraList[b].Export();
        }

        public static void EatInputInRect(Rect eatRect)
        {
            if (!(Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))) //Eat only when left-click
                return;
            if (eatRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
        }
    }
}
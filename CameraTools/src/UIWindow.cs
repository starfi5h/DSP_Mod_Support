using UnityEngine;
using System;

namespace CameraTools
{
    static class UIWindow
    {
        private static Rect modConfigWindow = new(20f, 20f, 300f, 240f);
        private static Rect cameraListWindow = new(20f, 260f, 300f, 240f);
        private static Rect cameraConfigWindow = new(320f, 260f, 300f, 365f);
        private static Rect pathListWindow = new(900f, 350f, 320f, 240f);
        private static Rect pathConfigWindow = new(1200f, 350f, 300f, 370f);
        private static Rect targetConfigWindow = new(900f, 350f, 300f, 270f);
        private static Rect recordWindow = new(900f, 20f, 300f, 270f);

        public static bool CanResize { get; private set; }
        public static CameraPoint EditingCam { get; set; }
        public static CameraPath EditingPath { get; set; }
        public static int lastEditingPathIndex;
        public static LookTarget EditingTarget { get; set; }

        static bool cameraListWindowActivated = true;
        static bool pathListWindowActivated;
        static bool modConfigWindowActivated;
        static bool recordWindowActivated;

        public static void LoadWindowPos(bool reset = false)
        {
            Util.SetWindowPos(ref modConfigWindow, ModConfig.PosModConfigWindow, reset);
            Util.SetWindowPos(ref cameraListWindow, ModConfig.PosCameraListWindow, reset);
            Util.SetWindowPos(ref cameraConfigWindow, ModConfig.PosCameraConfigWindow, reset);
            Util.SetWindowPos(ref pathListWindow, ModConfig.PosPathListWindow, reset);
            Util.SetWindowPos(ref pathConfigWindow, ModConfig.PosPathConfigWindow, reset);
            Util.SetWindowPos(ref targetConfigWindow, ModConfig.PosTargetConfigWindow, reset);
            if (reset) SaveWindowPos();
        }

        public static void SaveWindowPos()
        {
            ModConfig.PosModConfigWindow.Value = modConfigWindow.position;
            ModConfig.PosCameraListWindow.Value = cameraListWindow.position;
            ModConfig.PosCameraConfigWindow.Value = cameraConfigWindow.position;
            ModConfig.PosPathListWindow.Value = pathListWindow.position;
            ModConfig.PosPathConfigWindow.Value = pathConfigWindow.position;
            ModConfig.PosTargetConfigWindow.Value = targetConfigWindow.position;
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
                    EditingPath.Name += GameMain.localPlanet != null ? " (planet)" : " (space)";
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

        public static void ToggleRecordWindow()
        {
            recordWindowActivated = !recordWindowActivated;
            if (!recordWindowActivated)
            {
                SaveWindowPos();
            }
        }

        public static void OnGUI()
        {
            CanResize = false;
            if (DSPGame.IsMenuDemo || GameMain.mainPlayer == null || (Plugin.ViewingPath != null && CameraPath.HideGUI && Plugin.ViewingPath.IsPlaying))
            {
                return;
            }

            if (modConfigWindowActivated)
            {
                modConfigWindow = GUI.Window(1307890670, modConfigWindow, ModConfigWindowFunc, "Mod Config".Translate());
                HandleDrag(1307890670, ref modConfigWindow);
            }

            if (cameraListWindowActivated)
            {
                var title = "Camera List".Translate();
                if (Plugin.ViewingCam != null) title += $" [{Plugin.ViewingCam.Index}]";
                cameraListWindow = GUI.Window(1307890671, cameraListWindow, CameraListWindowFunc, title);
                HandleDrag(1307890671, ref cameraListWindow);
            }

            if (EditingCam != null)
            {
                cameraConfigWindow = GUI.Window(1307890672, cameraConfigWindow, CamConfigWindowFunc, "Camera Config".Translate());
                HandleDrag(1307890672, ref cameraConfigWindow);
            }

            if (EditingPath != null)
            {
                pathConfigWindow = GUI.Window(1307890673, pathConfigWindow, PathConfigWindowFunc, "Path Config".Translate() + $" [{EditingPath.Index}]");
                HandleDrag(1307890673, ref pathConfigWindow);
            }

            if (pathListWindowActivated)
            {
                pathListWindow = GUI.Window(1307890674, pathListWindow, PathListWindowFunc, "Path List".Translate());
                HandleDrag(1307890674, ref pathListWindow);
            }

            if (EditingTarget != null)
            {
                targetConfigWindow = GUI.Window(1307890675, targetConfigWindow, TargetConfigWindowFunc, "Target Config".Translate());
                HandleDrag(1307890675, ref targetConfigWindow);
            }

            if (recordWindowActivated)
            {
                recordWindow = GUI.Window(1307890676, recordWindow, RecordWindowFunc, "Timelapse Record".Translate());
                HandleDrag(1307890676, ref recordWindow);
            }
        }

        static readonly string[] modConfigTabText = { "Config", "I/O" };
        static int modConfigTabIndex;
        static void ModConfigWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(modConfigWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X")) modConfigWindowActivated = false;
            GUILayout.EndArea();

            modConfigTabIndex = GUILayout.Toolbar(modConfigTabIndex, Extensions.TL(modConfigTabText));
            switch (modConfigTabIndex)
            {
                case 0: ModConfig.ConfigWindowFunc(); break;
                case 1: ModConfig.ImportWindowFunc(); break;
            }
            GUI.DragWindow();
        }

        static void RecordWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(recordWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X")) ToggleRecordWindow();
            GUILayout.EndArea();

            CaptureManager.ConfigWindowFunc();
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
                if (EditingPath != null) EditingPath.OnKeyFrameChange();
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

        static void TargetConfigWindowFunc(int id)
        {
            GUILayout.BeginArea(new Rect(targetConfigWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X"))
            {
                EditingTarget = null;
                SaveWindowPos();
            }
            GUILayout.EndArea();

            EditingTarget?.ConfigWindowFunc();
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
            int swappingIndex = -1;
            scrollPositionPathList = GUILayout.BeginScrollView(scrollPositionPathList);
            foreach (var path in Plugin.PathList)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"[{path.Index}]", GUILayout.MaxWidth(20));
                GUILayout.Label(path.Name);

                if (path != EditingPath)
                {
                    if (GUILayout.Button("Load".Translate(), GUILayout.MaxWidth(60))) EditingPath = path;
                }
                else
                {
                    if (GUILayout.Button("↑", GUILayout.MaxWidth(35))) swappingIndex = path.Index - 1;
                    if (GUILayout.Button("↓", GUILayout.MaxWidth(35))) swappingIndex = path.Index;
                }
                if (GUILayout.Button("Remove".Translate(), GUILayout.MaxWidth(60))) removingIndex = path.Index;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            if (swappingIndex >= 0 && (swappingIndex + 1) < Plugin.PathList.Count)
            {
                int a = swappingIndex; int b = swappingIndex + 1;
                (Plugin.PathList[a], Plugin.PathList[b]) = (Plugin.PathList[b], Plugin.PathList[a]);
                Plugin.PathList[a].Index = a;
                Plugin.PathList[b].Index = b;
                Plugin.PathList[a].Export();
                Plugin.PathList[b].Export();
            }
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
                EditingPath.Name += GameMain.localPlanet != null ? " (planet)" : " (space)";
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
            GUILayout.BeginArea(new Rect(cameraListWindow.width - 27f, 1f, 25f, 16f));
            if (GUILayout.Button("X"))
            {
                EditingCam = null;
                ToggleCameraListWindow();
                return;
            }
            GUILayout.EndArea();

            int removingIndex = -1;
            int swappingIndex = -1;
            scrollPositionCameraList = GUILayout.BeginScrollView(scrollPositionCameraList);
            foreach (var camera in Plugin.CameraList)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    // Title
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(camera.Name);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(camera.GetInfo());
                    GUILayout.EndHorizontal();


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
                        if (GUILayout.Button("↑", GUILayout.MaxWidth(35))) swappingIndex = camera.Index - 1;
                        if (GUILayout.Button("↓", GUILayout.MaxWidth(35))) swappingIndex = camera.Index;
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
            if (swappingIndex >= 0 && (swappingIndex + 1) < Plugin.CameraList.Count)
            {
                SwapCamIndex(swappingIndex, swappingIndex + 1);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Camera".Translate()))
            {
                Plugin.Log.LogDebug("Add Cam " + Plugin.CameraList.Count);
                var cam = new CameraPoint(Plugin.CameraList.Count);
                if (GameMain.localPlanet != null) cam.SetPlanetCamera();
                else cam.SetSpaceCamera();
                cam.Name = string.Format("cam-{0}-{1}", GameMain.localPlanet != null ? "planet" : "space", cam.Index);
                Plugin.CameraList.Add(cam);
                cam.Export();
                ModConfig.CameraListCount.Value = Plugin.CameraList.Count;
            }
            if (GUILayout.Button("Config".Translate()))
            {
                modConfigWindowActivated = !modConfigWindowActivated;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Camera Path Window".Translate()))
            {
                TogglePathConfigWindow();
            }
            if (GUILayout.Button("Record Window".Translate()))
            {
                ToggleRecordWindow();
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
            (Plugin.CameraList[a], Plugin.CameraList[b]) = (Plugin.CameraList[b], Plugin.CameraList[a]);
            Plugin.CameraList[a].Index = a;
            Plugin.CameraList[b].Index = b;
            Plugin.CameraList[a].Export();
            Plugin.CameraList[b].Export();
        }


        static int resizingWindowId;
        static void HandleDrag(int id, ref Rect windowRect)
        {            
            Rect resizeHandleRect = new(windowRect.xMax - 13, windowRect.yMax - 13, 20, 20);
            //GUI.Box(resizeHandleRect, ""); // Draw a resize handle in the bottom-right corner for 20x20 pixel

            if (resizeHandleRect.Contains(Event.current.mousePosition) && !windowRect.Contains(Event.current.mousePosition))
            {
                CanResize = true;
                if (Event.current.type == EventType.MouseDown && resizingWindowId == 0)
                {
                    resizingWindowId = id;
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                resizingWindowId = 0; // Release resizing when mouse button is released
            }

            if (id == resizingWindowId)
            {
                CanResize = true; // Use the same flag for cursor currently
                // Calculate new window size based on mouse position, keeping the minimum window size as 30x30
                windowRect.xMax = Math.Max(Event.current.mousePosition.x, windowRect.xMin + 30);
                windowRect.yMax = Math.Max(Event.current.mousePosition.y, windowRect.yMin + 30);
            }
            EatInputInRect(windowRect);
        }

        static void EatInputInRect(Rect eatRect)
        {
            if (!(Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))) //Eat only when left-click
                return;
            if (eatRect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
                Input.ResetInputAxes();
        }
    }
}
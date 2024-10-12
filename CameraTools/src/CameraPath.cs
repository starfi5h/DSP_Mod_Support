using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;

namespace CameraTools
{
    public class CameraPath
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public bool IsPlaying { get; private set; }
        public bool HideGUI { get; private set; }

        readonly List<FixedCamera> cameras;
        readonly List<float> keyTimes;
        float duration = 5;

        string sectionName => "path-" + Index;
        CameraPose camPose;
        VectorLF3 uPosition;
        float progression;

        public CameraPath(int index)
        {
            Index = index;
            Name = sectionName;
            cameras = new List<FixedCamera>();
            keyTimes = new List<float>();
        }

        public void Import()
        {
            ConfigFile configFile = Plugin.ConfigFile;
            Name = configFile.Bind(sectionName, "Name", "path-" + Index).Value;
            int cameraCount = configFile.Bind(sectionName, "cameraCount", 0).Value;
            cameras.Clear();
            keyTimes.Clear();
            for (int i = 0; i < cameraCount; i++)
            {
                var cam = new FixedCamera(i, sectionName);
                cam.Import();
                cameras.Add(cam);
                var keyTime = configFile.Bind(sectionName, "keytime-" + i, 0f).Value;
                keyTimes.Add(keyTime);
            }
            duration = configFile.Bind(sectionName, "duration", 5f).Value;
        }

        public void Export()
        {
            ConfigFile configFile = Plugin.ConfigFile;
            configFile.Bind(sectionName, "Name", "Cam-" + Index).Value = Name;
            configFile.Bind(sectionName, "cameraCount", 0).Value = cameras.Count;
            foreach (var cam in cameras) cam.Export();
            for (int i = 0; i < keyTimes.Count; i++)
            {
                configFile.Bind(sectionName, "keytime-"+i, 0f).Value = keyTimes[i];
            }
            configFile.Bind(sectionName, "duration", 5f).Value = duration;
        }


        public void OnLateUpdate()
        {
            if (duration == 0 || !IsPlaying) return;
            progression = Mathf.Clamp01(progression + Time.deltaTime / duration);
            if (duration > 0 && progression == 1.0f) IsPlaying = false;
        }

        public void ApplyToCamera(Camera cam)
        {
            if (cameras.Count == 0) return;
            int index = 0;
            for (int i = 0; i < keyTimes.Count; i++)
            {
                if (progression < keyTimes[i]) break;
                index++;
            }
            if (index == 0)
            {
                cameras[0].ApplyToCamera(cam);
                return;
            }
            else if (index >= cameras.Count)
            {
                cameras[cameras.Count - 1].ApplyToCamera(cam);
                return;
            }
            float total = keyTimes[index] - keyTimes[index - 1];
            if (total == 0f)
            {
                cameras[index].ApplyToCamera(cam);
                return;
            }
            float t = (progression - keyTimes[index - 1]) / total;
            camPose = FixedCamera.Lerp(cameras[index - 1], cameras[index], t, out uPosition);
            camPose.ApplyToCamera(cam);
            if (GameMain.localPlanet == null && GameMain.mainPlayer != null)
            {
                if (ModConfig.MovePlayerWithSpaceCamera.Value)
                {
                    GameMain.mainPlayer.uPosition = uPosition;
                }
                else
                {
                    var diff = GameMain.mainPlayer.uPosition - uPosition;
                    GameCamera.main.transform.position -= (Vector3)diff;
                    Util.UniverseSimulatorGameTick(uPosition);
                }
            }
        }

        static Vector2 scrollPosition = new(100, 100);

        public void ConfigWindowFunc()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            progression = GUILayout.HorizontalSlider(progression, 0.0f, 1.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Progress: ".Translate() + progression.ToString("F2"));
            if (GUILayout.Button("|<<", GUILayout.MaxWidth(40)))
            {
                progression = 0;
                Plugin.ViewingPath = this;
            }
            if (GUILayout.Button(IsPlaying ? "||" : "▶︎", GUILayout.MaxWidth(40)))
            {
                if (cameras.Count < 2) UIRealtimeTip.Popup("Not enough camera! (≥2)");
                IsPlaying = !IsPlaying;
                if (IsPlaying) Plugin.ViewingPath = this;
            }
            if (GUILayout.Button(">>|", GUILayout.MaxWidth(40)))
            {
                progression = 1.0f;
                Plugin.ViewingPath = this;
            }
            GUILayout.EndHorizontal();

            Util.AddFloatFieldInput("Duration(s)".Translate(), ref duration, 1);
            GUILayout.EndVertical();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Plugin.ViewingPath == this ? "[Viewing]".Translate() : "View".Translate()))
            {
                Plugin.ViewingPath = Plugin.ViewingPath == this ? null : this;
            }
            HideGUI = GUILayout.Toggle(HideGUI, "Hide GUI during playback".Translate());
            GUILayout.EndHorizontal();

            int removingIndex = -1;
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            foreach (var camera in cameras)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    // Title
                    float keyTime = keyTimes[camera.Index];
                    Util.AddFloatFieldInput($"[{camera.Index}]", ref keyTime);
                    keyTimes[camera.Index] = keyTime;

                    // View, Edit, Remove
                    GUILayout.BeginHorizontal();
                    bool isViewing = Plugin.ViewingCam == camera;
                    if (GUILayout.Button(isViewing ? "[Viewing]".Translate() : "View".Translate()))
                    {
                        if (isViewing) Plugin.ViewingCam = null;
                        else if (!camera.CanView()) UIRealtimeTip.Popup("Camera type mismatch to current environment!".Translate());
                        else Plugin.ViewingCam = camera;
                    }
                    bool isEditing = UIWindow.EditingCam == camera;
                    if (GUILayout.Button(isEditing ? "[Editing]".Translate() : "Edit".Translate()))
                    {
                        UIWindow.EditingCam = isEditing ? null : camera;
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
                Plugin.Log.LogDebug("Remove Path Cam " + removingIndex);
                UIWindow.EditingCam = null;
                cameras.RemoveAt(removingIndex);
                keyTimes.RemoveAt(removingIndex);
                RearrangeTimes();
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add New Camera".Translate()))
            {
                Plugin.Log.LogDebug("Add Path Cam " + cameras.Count);
                var cam = new FixedCamera(cameras.Count, sectionName);
                if (GameMain.localPlanet != null) cam.SetPlanetCamera();
                else cam.SetSpaceCamera();
                cameras.Add(cam);
                keyTimes.Add(1.0f);
                RearrangeTimes();
            }
            if (GUILayout.Button("Save/Load".Translate()))
            {
                UIWindow.TogglePathListWindow();
            }
            GUILayout.EndHorizontal();
        }

        void RearrangeTimes()
        {
            int count = cameras.Count;
            if (count == 0) return;
            while (keyTimes.Count < count) keyTimes.Add(1.0f);

            for (int i = 0; i < count; i++)
            {
                cameras[i].Index = i;
                keyTimes[i] = count > 1 ? (float)i / (count - 1) : 0f;
            }
            keyTimes[count - 1] = 1f;
            Export();
        }
    }
}

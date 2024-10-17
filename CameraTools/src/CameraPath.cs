using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine;

namespace CameraTools
{
    public class CameraPath
    {
        // Serial data
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public bool IsPlaying { get; private set; }
        public bool HideGUI { get; private set; }

        readonly List<CameraPoint> cameras = new();
        readonly List<float> keyTimes = new();
        float duration = 5;
        int interpolation = 1;
        readonly LookTarget lookTarget = new();

        // UI window
        static readonly string[] interpolationTexts = { "Linear", "Spherical", "Curve" };
        static bool autoSplit = true;
        static int keyFormat = 0;        
        static readonly string[] keyFormatTexts = { "Ratio", "Second" };
        static Vector2 scrollPosition = new(100, 100);

        // internal tempoary values
        string SectionName => "path-" + Index;
        CameraPose camPose;
        VectorLF3 uPosition;
        float progression;
        AnimationCurve animCurveX = null;
        AnimationCurve animCurveY = null;
        AnimationCurve animCurveZ = null;
        AnimationCurve animCurveUX = null;
        AnimationCurve animCurveUY = null;
        AnimationCurve animCurveUZ = null;

        public CameraPath(int index)
        {
            Index = index;
            Name = SectionName;
        }

        public void Import(ConfigFile configFile = null)
        {
            if (configFile == null) configFile = Plugin.ConfigFile;
            Name = configFile.Bind(SectionName, "Name", "path-" + Index).Value;
            int cameraCount = configFile.Bind(SectionName, "cameraCount", 0).Value;
            cameras.Clear();
            keyTimes.Clear();
            for (int i = 0; i < cameraCount; i++)
            {
                var cam = new CameraPoint(i)
                {
                    SectionPrefix = SectionName
                };
                cam.Import(configFile);
                cameras.Add(cam);
                var keyTime = configFile.Bind(SectionName, "keytime-" + i, 0f).Value;
                keyTimes.Add(keyTime);
            }
            duration = configFile.Bind(SectionName, "duration", 5f).Value;
            interpolation = configFile.Bind(SectionName, "interpolation", 0).Value;
            lookTarget.Import(SectionName, configFile);
            OnKeyFrameChange();
        }

        public void Export(ConfigFile configFile = null)
        {
            if (configFile == null) configFile = Plugin.ConfigFile;
            configFile.Bind(SectionName, "Name", "Cam-" + Index).Value = Name;
            configFile.Bind(SectionName, "cameraCount", 0).Value = cameras.Count;
            foreach (var cam in cameras)
            {
                cam.SectionPrefix = SectionName;
                cam.Export(configFile);
            }
            for (int i = 0; i < keyTimes.Count; i++)
            {
                configFile.Bind(SectionName, "keytime-"+i, 0f).Value = keyTimes[i];
            }
            configFile.Bind(SectionName, "duration", 5f).Value = duration;
            configFile.Bind(SectionName, "interpolation", 0).Value = interpolation;
            lookTarget.Export(SectionName, configFile);
        }

        public void OnKeyFrameChange()
        {
            if (interpolation == 2) // Curve
            {
                //Plugin.Log.LogDebug($"RebuildAnimationCurves " + keyTimes.Count);
                RebuildAnimationCurves();
            }
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
            if (!UpdateCameraPose()) return;

            if (lookTarget.Type != LookTarget.TargetType.None)
            {
                camPose.rotation = lookTarget.SetRotation(camPose.position, uPosition);
            }
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

        private bool UpdateCameraPose()
        {
            int index = 0;
            for (int i = 0; i < keyTimes.Count; i++)
            {
                if (progression < keyTimes[i]) break;
                index++;
            }
            if (index == 0)
            {
                if (!cameras[0].CanView) return false;
                camPose = cameras[0].CamPose;
                uPosition = cameras[0].UPosition;
                return true;
            }
            else if (index >= cameras.Count)
            {
                if (!cameras[cameras.Count - 1].CanView) return false;
                camPose = cameras[cameras.Count - 1].CamPose;
                uPosition = cameras[cameras.Count - 1].UPosition;
                return true;
            }
            if (!cameras[index].CanView || !cameras[index - 1].CanView) return false;
            float total = keyTimes[index] - keyTimes[index - 1];
            if (total == 0f)
            {
                camPose = cameras[index].CamPose;
                uPosition = cameras[index].UPosition;
                return false;
            }
            float t = (progression - keyTimes[index - 1]) / total;
            camPose = PiecewiseLerp(cameras[index - 1], cameras[index], t);

            if (interpolation == 2 && animCurveX != null) // Curve
            {
                camPose.position = new Vector3(animCurveX.Evaluate(progression), animCurveY.Evaluate(progression), animCurveZ.Evaluate(progression));
                if (GameMain.localPlanet == null)
                {
                    // Use relative Upos to avoid the precision loss of double to float
                    uPosition = cameras[0].UPosition + new VectorLF3(animCurveUX.Evaluate(progression), animCurveUY.Evaluate(progression), animCurveUZ.Evaluate(progression));
                }
            }
            return true;
        }

        private CameraPose PiecewiseLerp(CameraPoint from, CameraPoint to, float t)
        {
            Vector3 positon;
            uPosition = VectorLF3.zero;

            if (interpolation == 1) // Spherical
            {
                positon = Vector3.Lerp(from.CamPose.position, to.CamPose.position, t);
                uPosition = from.UPosition + (to.UPosition - from.UPosition) * t;

                if (GameMain.localPlanet != null) // linear interpolation for altitude
                {
                    positon *= Mathf.Lerp(from.CamPose.position.magnitude, to.CamPose.position.magnitude, t) / positon.magnitude;
                }
                else if (GameMain.localStar != null)
                {
                    float distA = (float)(from.UPosition - GameMain.localStar.uPosition).magnitude;
                    float distB = (float)(to.UPosition - GameMain.localStar.uPosition).magnitude;
                    float distToStar = Mathf.Lerp(distA, distB, t);
                    uPosition = GameMain.localStar.uPosition + (uPosition - GameMain.localStar.uPosition).normalized * distToStar;
                }
            }
            else // Linear
            {
                positon = Vector3.Lerp(from.CamPose.position, to.CamPose.position, t);
                uPosition = from.UPosition + (to.UPosition - from.UPosition) * t;
            }

            return new CameraPose(positon,
                    Quaternion.Slerp(from.CamPose.rotation, to.CamPose.rotation, t),
                    Mathf.Lerp(from.CamPose.fov, to.CamPose.fov, t), Mathf.Lerp(from.CamPose.near, to.CamPose.near, t), Mathf.Lerp(from.CamPose.far, to.CamPose.far, t));
        }

        private void RebuildAnimationCurves()
        {
            animCurveX = new AnimationCurve();
            animCurveY = new AnimationCurve();
            animCurveZ = new AnimationCurve();
            animCurveUX = new AnimationCurve();
            animCurveUY = new AnimationCurve();
            animCurveUZ = new AnimationCurve();
            for (int i = 0; i < keyTimes.Count; i++)
            {
                animCurveX.AddKey(keyTimes[i], cameras[i].CamPose.position.x);
                animCurveY.AddKey(keyTimes[i], cameras[i].CamPose.position.y);
                animCurveZ.AddKey(keyTimes[i], cameras[i].CamPose.position.z);
                animCurveUX.AddKey(keyTimes[i], (float)(cameras[i].UPosition.x - cameras[0].UPosition.x));
                animCurveUY.AddKey(keyTimes[i], (float)(cameras[i].UPosition.y - cameras[0].UPosition.y));
                animCurveUZ.AddKey(keyTimes[i], (float)(cameras[i].UPosition.z - cameras[0].UPosition.z));
            }
        }

        public void ConfigWindowFunc()
        {
            int tmpInt;
            GUILayout.BeginVertical(GUI.skin.box);
            progression = GUILayout.HorizontalSlider(progression, 0.0f, 1.0f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Progress: ".Translate() + progression.ToString("F2"));
            if (GUILayout.Button("|<<", GUILayout.MaxWidth(40)))
            {
                progression = 0f;
                Plugin.ViewingPath = this;
            }
            if (GUILayout.Button(IsPlaying ? "||" : "▶︎", GUILayout.MaxWidth(40)))
            {
                if (cameras.Count < 2) UIRealtimeTip.Popup("Not enough camera! (≥2)");
                IsPlaying = !IsPlaying;
                if (IsPlaying)
                {
                    Plugin.ViewingCam = null;
                    Plugin.ViewingPath = this;
                    if (progression >= 1.0f && duration > 0) progression = 0f;
                }
            }
            if (GUILayout.Button(">>|", GUILayout.MaxWidth(40)))
            {
                progression = 1.0f;
                Plugin.ViewingPath = this;
            }
            GUILayout.EndHorizontal();

            Util.AddFloatFieldInput("Duration(s)".Translate(), ref duration, 1);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Interp".Translate());
            tmpInt = GUILayout.Toolbar(interpolation, interpolationTexts);
            if (tmpInt != interpolation)
            {
                interpolation = tmpInt;
                OnKeyFrameChange();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Plugin.ViewingPath == this ? "[Viewing]".Translate() : "View".Translate()))
            {
                Plugin.ViewingPath = Plugin.ViewingPath == this ? null : this;
            }
            HideGUI = GUILayout.Toggle(HideGUI, "Hide GUI during playback".Translate());
            GUILayout.EndHorizontal();
                        
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.BeginHorizontal();
            keyFormat = GUILayout.Toolbar(keyFormat, keyFormatTexts);
            autoSplit = GUILayout.Toggle(autoSplit, "Auto Split".Translate());
            GUILayout.EndHorizontal();

            int removingIndex = -1;
            int upIndex = -1;
            int downIndex = -1;
            foreach (var camera in cameras)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    // Title
                    if (keyFormat == 0) // Ratio
                    {
                        float keyTime = keyTimes[camera.Index];
                        Util.AddFloatFieldInput($"[{camera.Index}] {camera.Name}", ref keyTime);
                        keyTimes[camera.Index] = Mathf.Clamp01(keyTime);
                    }
                    else // Time (seconds)
                    {
                        float second = keyTimes[camera.Index] * duration;
                        Util.AddFloatFieldInput($"[{camera.Index}] {camera.Name}", ref second);
                        if (duration != 0f) keyTimes[camera.Index] = Mathf.Clamp01(second / duration);
                    }

                    // View, Edit, Remove
                    GUILayout.BeginHorizontal();
                    bool isViewing = Plugin.ViewingCam == camera;
                    if (GUILayout.Button(isViewing ? "[Viewing]".Translate() : (camera.CanView ? "View".Translate() : "Unavailable".Translate())))
                    {
                        if (isViewing) Plugin.ViewingCam = null;
                        else if (!camera.CanView) UIRealtimeTip.Popup("Camera type mismatch to current environment!".Translate());
                        else Plugin.ViewingCam = camera;
                    }
                    if (GUILayout.Button("↑")) upIndex = camera.Index;
                    if (GUILayout.Button("↓")) downIndex = camera.Index;
                    bool isEditing = UIWindow.EditingCam == camera;
                    if (GUILayout.Button(isEditing ? "[Editing]".Translate() : "Edit".Translate()))
                    {
                        UIWindow.EditingCam = isEditing ? null : camera;
                        OnKeyFrameChange();
                    }
                    if (GUILayout.Button("Remove".Translate(), GUILayout.MaxWidth(60)))
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
                RearrangeTimes(autoSplit);
            }
            if (upIndex >= 1)
            {
                SwapCamIndex(upIndex, upIndex - 1);
            }
            if (downIndex >= 0 && (downIndex + 1) < cameras.Count)
            {
                SwapCamIndex(downIndex, downIndex + 1);
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Insert Keyframe".Translate()))
            {
                int index = 0;
                for (; index < cameras.Count; index++)
                {
                    if (progression <= keyTimes[index]) break;
                }
                if (index >= cameras.Count) index = cameras.Count;
                Plugin.Log.LogDebug("Insert Path Cam " + index);
                var cam = new CameraPoint(index)
                {
                    SectionPrefix = SectionName
                };
                if (GameMain.localPlanet != null) cam.SetPlanetCamera();
                else cam.SetSpaceCamera();
                cam.Name = cam.GetPolarName();
                cameras.Insert(index, cam);
                keyTimes.Insert(index, progression);
                RearrangeTimes(false);
            }
            if (GUILayout.Button("Append Keyframe".Translate()))
            {
                Plugin.Log.LogDebug("Add Path Cam " + cameras.Count);
                var cam = new CameraPoint(cameras.Count)
                {
                    SectionPrefix = SectionName
                };
                if (GameMain.localPlanet != null) cam.SetPlanetCamera();
                else cam.SetSpaceCamera();
                cam.Name = cam.GetPolarName();
                cameras.Add(cam);
                keyTimes.Add(1.0f);
                RearrangeTimes(autoSplit);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Target: ".Translate() + lookTarget.Type.ToString()))
            {
                if (UIWindow.EditingTarget != lookTarget) LookTarget.OpenAndSetWindow(lookTarget);
                else UIWindow.EditingTarget = null;
            }
            if (GUILayout.Button("Save/Load".Translate()))
            {
                UIWindow.TogglePathListWindow();
            }
            GUILayout.EndHorizontal();
        }

        void RearrangeTimes(bool evenSplitTime)
        {
            int count = cameras.Count;
            if (count == 0) return;
            while (keyTimes.Count < count) keyTimes.Add(1.0f);

            for (int i = 0; i < count; i++)
            {
                cameras[i].Index = i;
                if (evenSplitTime) keyTimes[i] = count > 1 ? (float)i / (count - 1) : 0f;
            }
            if (evenSplitTime) keyTimes[count - 1] = 1f;
            Export();
            OnKeyFrameChange();
        }

        void SwapCamIndex(int a, int b)
        {
            var tmp = cameras[a];
            cameras[a] = cameras[b];
            cameras[b] = tmp;
            cameras[a].Index = a;
            cameras[b].Index = b;
            Export();
            OnKeyFrameChange();
        }
    }
}

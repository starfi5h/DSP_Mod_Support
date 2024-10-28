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
        public bool IsPlaying { get; set; }
        public static bool Loop { get; private set; }
        public bool HideGUI => hideGUI & IsPlaying;
        public bool Preview => preview & !HideGUI;

        readonly List<CameraPoint> cameras = new();
        readonly List<float> keyTimes = new();
        float duration = 5;
        int interpolation = 1;
        readonly LookTarget lookTarget = new();

        // UI window
        static bool hideGUI;
        static bool preview;
        static readonly string[] interpolationTexts = { "Linear", "Spherical", "Curve" };
        static bool autoSplit = true;
        static int keyFormat = 0;        
        static readonly string[] keyFormatTexts = { "Ratio", "Second" };
        static Vector2 scrollPosition = new(100, 100);

        // internal temporary values
        string SectionName => "path-" + Index;
        CameraPose camPose;
        VectorLF3 uPosition;
        float progression; // [0,1] timeline progression of the path
        float totalTime;   // total time from starting the path (second)
        AnimationCurve animCurveX;
        AnimationCurve animCurveY;
        AnimationCurve animCurveZ;
        AnimationCurve animCurveUX;
        AnimationCurve animCurveUY;
        AnimationCurve animCurveUZ;

        public CameraPath(int index)
        {
            Index = index;
            Name = SectionName;
        }

        public void Import(ConfigFile configFile = null)
        {
            configFile ??= Plugin.ConfigFile;
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
            configFile ??= Plugin.ConfigFile;
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

        public void OnLateUpdate(float deltaTime)
        {
            if (duration == 0 || !IsPlaying || GameMain.isPaused) return;
            progression = Mathf.Clamp01(progression + deltaTime / duration);
            totalTime += Time.deltaTime;
            if (duration > 0 && progression == 1.0f)
            {
                if (Loop) progression = 0.0f;
                else IsPlaying = false;
            }
        }

        public void TogglePlayButton()
        {
            IsPlaying = !IsPlaying;
            if (IsPlaying)
            {
                Plugin.ViewingCam = null;
                Plugin.ViewingPath = this;
                if (progression >= 1.0f && duration > 0)
                {
                    progression = 0f;
                    totalTime = 0f;
                }
            }
        }

        public void ApplyToCamera(Camera cam)
        {
            if (cameras.Count == 0) return;
            if (!UpdateCameraPose(progression)) return;

            if (lookTarget.Type != LookTarget.TargetType.None)
            {
                lookTarget.SetFinalPose(ref camPose, ref uPosition, totalTime);
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

        private bool UpdateCameraPose(float normalizedTime)
        {
            int index = 0;
            for (int i = 0; i < keyTimes.Count; i++)
            {
                if (normalizedTime < keyTimes[i]) break;
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
                return true;
            }
            float t = (normalizedTime - keyTimes[index - 1]) / total;
            camPose = PiecewiseLerp(cameras[index - 1], cameras[index], t);

            if (interpolation == 2 && animCurveX != null) // Curve
            {
                camPose.position = new Vector3(animCurveX.Evaluate(normalizedTime), animCurveY.Evaluate(normalizedTime), animCurveZ.Evaluate(normalizedTime));
                if (GameMain.localPlanet == null)
                {
                    // Use relative Upos to avoid the precision loss of double to float
                    uPosition = cameras[0].UPosition + new VectorLF3(animCurveUX.Evaluate(normalizedTime), animCurveUY.Evaluate(normalizedTime), animCurveUZ.Evaluate(normalizedTime));
                }
            }
            return true;
        }

        private CameraPose PiecewiseLerp(CameraPoint from, CameraPoint to, float t)
        {
            Vector3 position;
            uPosition = VectorLF3.zero;

            if (interpolation == 1) // Spherical
            {
                position = Vector3.Lerp(from.CamPose.position, to.CamPose.position, t);
                uPosition = from.UPosition + (to.UPosition - from.UPosition) * t;

                if (GameMain.localPlanet != null) // linear interpolation for altitude
                {
                    position *= Mathf.Lerp(from.CamPose.position.magnitude, to.CamPose.position.magnitude, t) / position.magnitude;
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
                position = Vector3.Lerp(from.CamPose.position, to.CamPose.position, t);
                uPosition = from.UPosition + (to.UPosition - from.UPosition) * t;
            }

            return new CameraPose(position,
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

        public int GetCameraCount()
        {
            return cameras.Count;
        }

        public int SetCameraPoints(List<GameObject> cameraObjs, List<VectorLF3> uPointList)
        {
            uPointList.Clear();
            for (int i = 0; i < cameras.Count; i++)
            {
                float t = keyTimes[i];
                if (!UpdateCameraPose(t)) return 0;  // if the camera is not viewable, don't show all camera objects
                if (lookTarget.Type != LookTarget.TargetType.None)
                {
                    lookTarget.SetFinalPose(ref camPose, ref uPosition, t * duration);
                }
                cameraObjs[i].transform.position = camPose.position;
                cameraObjs[i].transform.rotation = camPose.rotation;
                uPointList.Add(uPosition + (VectorLF3)camPose.position);
            }
            return cameras.Count;
        }

        public int SetPathPoints(Vector3[] lPoints, VectorLF3[] uPoints)
        {
            int pointCount = lPoints.Length;
            for (int i = 0; i < pointCount; i++)
            {
                float t = i / (float)pointCount;
                if (!UpdateCameraPose(t)) return 0; // if the camera is not viewable, don't show the line
                if (lookTarget.Type != LookTarget.TargetType.None)
                {
                    lookTarget.SetFinalPose(ref camPose, ref uPosition, t * duration);
                }
                lPoints[i] = camPose.position;
                uPoints[i] = uPosition + (VectorLF3)camPose.position;
            }
            return pointCount;
        }

        public void UIConfigWindowFunc()
        {
            UIPlayControlPanel();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Plugin.ViewingPath == this ? "[Viewing]".Translate() : "View".Translate()))
            {
                Plugin.ViewingPath = Plugin.ViewingPath == this ? null : this;
            }
            Loop = GUILayout.Toggle(Loop, "Loop".Translate());
            preview = GUILayout.Toggle(preview, "Preview".Translate());
            hideGUI = GUILayout.Toggle(hideGUI, "Hide GUI".Translate());
            GUILayout.EndHorizontal();

            UIKeyframePanel();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Path List".Translate())) UIWindow.TogglePathListWindow();
            if (GUILayout.Button("Record This Path".Translate()))
            {
                CaptureManager.SetCameraPath(this);
                UIWindow.ToggleRecordWindow();
            }
            GUILayout.EndHorizontal();
        }

        void UIPlayControlPanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            float tmpFloat = GUILayout.HorizontalSlider(progression, 0.0f, 1.0f);
            if (tmpFloat != progression)
            {
                progression = tmpFloat;
                totalTime = duration * progression;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Progress: ".Translate() + progression.ToString("F2"));
            if (GUILayout.Button("|<<", GUILayout.MaxWidth(40)))
            {
                progression = 0f;
                totalTime = 0f;
                Plugin.ViewingPath = this;
            }
            if (GUILayout.Button(IsPlaying ? "||" : "▶︎", GUILayout.MaxWidth(40)))
            {
                TogglePlayButton();
            }
            if (GUILayout.Button(">>|", GUILayout.MaxWidth(40)))
            {
                progression = 1.0f;
                totalTime = duration;
                Plugin.ViewingPath = this;
            }
            GUILayout.EndHorizontal();

            Util.AddFloatFieldInput("Duration(s)".Translate(), ref duration, 1);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Interp".Translate());
            int tmpInt = GUILayout.Toolbar(interpolation, Extensions.TL(interpolationTexts));
            if (tmpInt != interpolation)
            {
                interpolation = tmpInt;
                OnKeyFrameChange();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target".Translate());
            if (GUILayout.Button("<"))
            {
                int typeValue = lookTarget.Type == LookTarget.TargetType.None ? 3 : (int)lookTarget.Type - 1;
                lookTarget.Type = (LookTarget.TargetType)typeValue;
                GizmoManager.OnPathChange();
            }
            if (GUILayout.Button(lookTarget.Type.ToString().Translate()))
            {
                if (UIWindow.EditingTarget != lookTarget)
                {
                    LookTarget.OpenAndSetWindow(lookTarget);
                    GizmoManager.OnPathChange();
                }
                else UIWindow.EditingTarget = null;
            }
            if (GUILayout.Button(">"))
            {
                lookTarget.Type = (LookTarget.TargetType)(((int)lookTarget.Type + 1) % 4);
                GizmoManager.OnPathChange();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        void UIKeyframePanel()
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Keyframe".Translate());
            keyFormat = GUILayout.Toolbar(keyFormat, Extensions.TL(keyFormatTexts));
            autoSplit = GUILayout.Toggle(autoSplit, "Auto Split".Translate());
            GUILayout.EndHorizontal();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
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

            GUILayout.EndVertical();
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
            (cameras[a], cameras[b]) = (cameras[b], cameras[a]);
            cameras[a].Index = a;
            cameras[b].Index = b;
            Export();
            OnKeyFrameChange();
        }
    }
}

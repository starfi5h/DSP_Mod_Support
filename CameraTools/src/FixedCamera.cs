using BepInEx.Configuration;
using System.IO;
using UnityEngine;

namespace CameraTools
{
    public class FixedCamera
    {
        static readonly string[] cameraTypeTexts = { "Planet", "Space" };
        enum CameraType
        {
            Planet,
            Space
        }

        public int Index { get; set; }
        public bool EnableRecording { get; set; }
        public string Name { get; private set; } = "";
        public bool CanView
        {
            get
            {
                if (GameMain.data == null || DSPGame.Game.isMenuDemo || GameMain.mainPlayer == null) return false;
                if (cameraType == CameraType.Planet && GameMain.localPlanet == null) return false;
                if (cameraType == CameraType.Space && GameMain.localPlanet != null) return false;
                return true;
            }
        }

        CameraType cameraType;
        CameraPose camPose;
        VectorLF3 uPosition;

        readonly string groupName = "";
        string SectionName => groupName + "cam-" + Index;
        int screenshotCount = 0;

        public FixedCamera(int index, string groupName = "")
        {
            Index = index;
            this.groupName = groupName;
            Name = SectionName;
        }

        public void Import()
        {
            ConfigFile configFile = Plugin.ConfigFile;
            Name = configFile.Bind(SectionName, "Name", "Cam-" + Index).Value;
            //EnableRecording = configFile.Bind(sectionName, "Enable recording", false).Value;
            cameraType = (CameraType)configFile.Bind(SectionName, "cameraType", 0).Value;
            camPose.position = configFile.Bind(SectionName, "pose Position", Vector3.zero).Value;
            camPose.rotation = configFile.Bind(SectionName, "pose Rotation", Quaternion.identity).Value;
            camPose.fov = configFile.Bind(SectionName, "pose Fov", 0.0f).Value;
            camPose.near = configFile.Bind(SectionName, "pose Near", 0.0f).Value;
            camPose.far = configFile.Bind(SectionName, "pose Far", 0.0f).Value;
            uPosition = configFile.Bind(SectionName, "uPosition", Vector3.zero).Value;
        }

        public void Export()
        {
            ConfigFile configFile = Plugin.ConfigFile;
            configFile.Bind(SectionName, "Name", "Cam-" + Index).Value = Name;
            //configFile.Bind(sectionName, "Enable recording", false).Value = EnableRecording;
            configFile.Bind(SectionName, "cameraType", 0).Value = (int)cameraType;
            configFile.Bind(SectionName, "pose Position", Vector3.zero).Value = camPose.position;
            configFile.Bind(SectionName, "pose Rotation", Quaternion.identity).Value = camPose.rotation;
            configFile.Bind(SectionName, "pose Fov", 0.0f).Value = camPose.fov;
            configFile.Bind(SectionName, "pose Near", 0.0f).Value = camPose.near;
            configFile.Bind(SectionName, "pose Far", 0.0f).Value = camPose.far;
            configFile.Bind(SectionName, "uPosition", Vector3.zero).Value = uPosition;
        }

        public void SetPlanetCamera()
        {
            cameraType = CameraType.Planet;
            uPosition = Vector3.zero;
            camPose = GameCamera.instance.finalPoser.cameraPose;
        }

        public void SetSpaceCamera()
        {
            cameraType = CameraType.Space;
            uPosition = GameMain.mainPlayer?.uPosition ?? Vector3.zero;
            camPose = GameCamera.instance.finalPoser.cameraPose;
        }

        public void ApplyToCamera(Camera cam)
        {
            camPose.ApplyToCamera(cam);
            if (cameraType == CameraType.Space && GameMain.mainPlayer != null)
            {
                if (ModConfig.MovePlayerWithSpaceCamera.Value)
                {
                    GameMain.mainPlayer.uPosition = uPosition;
                }
                else
                {
                    // In universe, the position of main player is alway 0
                    // So we need to calculate the real cam postion with uPos difference
                    var diff = GameMain.mainPlayer.uPosition - uPosition;
                    GameCamera.main.transform.position -= (Vector3)diff;
                    Util.UniverseSimulatorGameTick(uPosition);
                }
            }
        }

        public void SaveScreenShot(string folderPath)
        {
            folderPath += "/" + Name + "/";
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            screenshotCount++;
            var fullPath = folderPath + screenshotCount.ToString("00000") + ".jpg";
            Plugin.Log.LogDebug("Save " + fullPath);
            File.WriteAllBytes(fullPath, GameMain.data.screenShot);            
        }

        public string GetInfo()
        {
            switch (cameraType)
            {
                case CameraType.Planet:
                    var pos = camPose.pose.position;
                    return $"P({pos.x:F0},{pos.y:F0},{pos.z:F0})";
                case CameraType.Space:
                    return $"S({uPosition.x:F0},{uPosition.y:F0},{uPosition.z:F0})";
            }
            return "";
        }

        public string GetStatus()
        {
            if (GameMain.data == null || DSPGame.Game.isMenuDemo || GameMain.mainPlayer == null) return "Not in game".Translate();
            if (cameraType == CameraType.Planet && GameMain.localPlanet == null) return "In space".Translate();
            if (cameraType == CameraType.Space && GameMain.localPlanet != null) return "On planet".Translate();
            if (EnableRecording)
            {
                //if (Plugin.ViewingCam != this) return "Not viewing".Translate(); // TODO: Fix for viewing
                return "Recording".Translate() + " " + screenshotCount;
            }
            return "Standby".Translate();
        }

        public static CameraPose Lerp(FixedCamera from, FixedCamera to, float t, out VectorLF3 uPostion)
        {
            var positon = Vector3.zero;
            uPostion = VectorLF3.zero;
            if (from.cameraType == CameraType.Planet)
            {
                //positon = Vector3.Slerp(from.camPose.position, to.camPose.position, t);
                positon = Vector3.Lerp(from.camPose.position, to.camPose.position, t);
            }
            else if (from.cameraType == CameraType.Space)
            {
                positon = Vector3.Lerp(from.camPose.position, to.camPose.position, t);
                uPostion = Vector3.Lerp(from.uPosition, to.uPosition, t);
            }

            return new CameraPose(positon,
                    Quaternion.Slerp(from.camPose.rotation, to.camPose.rotation, t),
                    Mathf.Lerp(from.camPose.fov, to.camPose.fov, t), Mathf.Lerp(from.camPose.near, to.camPose.near, t), Mathf.Lerp(from.camPose.far, to.camPose.far, t));
        }

        public void ConfigWindowFunc()
        {
            int tmpInt;
            string tmpString;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set to Current Cam".Translate()))
            {
                if (GameMain.localPlanet != null) SetPlanetCamera();
                else SetSpaceCamera();
            }
            if (GUILayout.Button("Undo All".Translate()))
            {
                Import();
            }
            GUILayout.EndHorizontal();            

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name".Translate());
            tmpString = Name;
            tmpString = GUILayout.TextField(tmpString, 100, GUILayout.MinWidth(100));
            if (!string.IsNullOrWhiteSpace(tmpString) && tmpString != Name)
            {
                Name = tmpString;
                screenshotCount = 0;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Camera Type".Translate(), GUILayout.MinWidth(35));
            tmpInt = GUILayout.Toolbar((int)cameraType, cameraTypeTexts);
            if (tmpInt != (int)cameraType)
            {
                cameraType = (CameraType)tmpInt;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Position".Translate());
                if (cameraType == CameraType.Planet)
                {
                    Util.AddFloatField("x", ref camPose.pose.position.x, 1f);
                    Util.AddFloatField("y", ref camPose.pose.position.y, 1f);
                    Util.AddFloatField("z", ref camPose.pose.position.z, 1f);
                }
                else if (cameraType == CameraType.Space)
                {
                    Vector3 pos = uPosition;
                    Util.AddFloatField("ux", ref pos.x, 100f);
                    Util.AddFloatField("uy", ref pos.y, 100f);
                    Util.AddFloatField("uz", ref pos.z, 100f);
                    uPosition = pos;
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Rotation".Translate());
                Vector3 rot = camPose.eulerAngles;
                Util.AddFloatField("pitch", ref rot.x, 10f);
                Util.AddFloatField("yaw", ref rot.y, 10f);
                Util.AddFloatField("roll", ref rot.z, 10f);
                camPose.eulerAngles = rot;
            }
            GUILayout.EndVertical();

            Util.AddFloatField("Fov", ref camPose.fov, 1f);
        }
    }
}

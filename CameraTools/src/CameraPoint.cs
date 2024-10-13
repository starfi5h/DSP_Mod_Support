using BepInEx.Configuration;
using UnityEngine;

namespace CameraTools
{
    public class CameraPoint
    {
        static readonly string[] cameraTypeTexts = { "Planet", "Space" };
        enum CameraType
        {
            Planet,
            Space
        }

        public int Index { get; set; }
        public string Name { get; set; } = "";
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
        public CameraPose CamPose;
        public VectorLF3 UPosition;

        readonly string groupName = "";
        string SectionName => groupName + "cam-" + Index;

        public CameraPoint(int index, string groupName = "")
        {
            Index = index;
            this.groupName = groupName;
            Name = SectionName;
        }

        public void Import()
        {
            ConfigFile configFile = Plugin.ConfigFile;
            Name = configFile.Bind(SectionName, "Name", "Cam-" + Index).Value;
            cameraType = (CameraType)configFile.Bind(SectionName, "cameraType", 0).Value;
            CamPose.position = configFile.Bind(SectionName, "pose Position", Vector3.zero).Value;
            CamPose.rotation = configFile.Bind(SectionName, "pose Rotation", Quaternion.identity).Value;
            CamPose.fov = configFile.Bind(SectionName, "pose Fov", 0.0f).Value;
            CamPose.near = configFile.Bind(SectionName, "pose Near", 0.0f).Value;
            CamPose.far = configFile.Bind(SectionName, "pose Far", 0.0f).Value;
            UPosition = configFile.Bind(SectionName, "uPosition", Vector3.zero).Value;
        }

        public void Export()
        {
            ConfigFile configFile = Plugin.ConfigFile;
            configFile.Bind(SectionName, "Name", "Cam-" + Index).Value = Name;
            configFile.Bind(SectionName, "cameraType", 0).Value = (int)cameraType;
            configFile.Bind(SectionName, "pose Position", Vector3.zero).Value = CamPose.position;
            configFile.Bind(SectionName, "pose Rotation", Quaternion.identity).Value = CamPose.rotation;
            configFile.Bind(SectionName, "pose Fov", 0.0f).Value = CamPose.fov;
            configFile.Bind(SectionName, "pose Near", 0.0f).Value = CamPose.near;
            configFile.Bind(SectionName, "pose Far", 0.0f).Value = CamPose.far;
            configFile.Bind(SectionName, "uPosition", Vector3.zero).Value = UPosition;
        }

        public void SetPlanetCamera()
        {
            cameraType = CameraType.Planet;
            if (Plugin.ViewingCam != null)
            {
                UPosition = Plugin.ViewingCam.UPosition;
                CamPose = Plugin.ViewingCam.CamPose;
                return;
            }
            UPosition = Vector3.zero;
            CamPose = GameCamera.instance.finalPoser.cameraPose;
        }

        public void SetSpaceCamera()
        {
            cameraType = CameraType.Space;
            if (Plugin.ViewingCam != null)
            {
                UPosition = Plugin.ViewingCam.UPosition;
                CamPose = Plugin.ViewingCam.CamPose;
                return;
            }
            UPosition = GameMain.mainPlayer?.uPosition ?? Vector3.zero;
            CamPose = GameCamera.instance.finalPoser.cameraPose;
        }

        public void ApplyToCamera(Camera cam)
        {
            CamPose.ApplyToCamera(cam);
            if (cameraType == CameraType.Space && GameMain.mainPlayer != null)
            {
                if (ModConfig.MovePlayerWithSpaceCamera.Value)
                {
                    GameMain.mainPlayer.uPosition = UPosition;
                }
                else
                {
                    // In universe, the position of main player is alway 0
                    // So we need to calculate the real cam postion with uPos difference
                    var diff = GameMain.mainPlayer.uPosition - UPosition;
                    GameCamera.main.transform.position -= (Vector3)diff;
                    Util.UniverseSimulatorGameTick(UPosition);
                }
            }
        }

        public string GetInfo()
        {
            switch (cameraType)
            {
                case CameraType.Planet:
                    var pos = CamPose.pose.position;
                    return $"P({pos.x:F0},{pos.y:F0},{pos.z:F0})";
                case CameraType.Space:
                    return $"S({UPosition.x:F0},{UPosition.y:F0},{UPosition.z:F0})";
            }
            return "";
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
            if (tmpString != Name) Name = tmpString;
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
                    Util.AddFloatField("x", ref CamPose.pose.position.x, 1f);
                    Util.AddFloatField("y", ref CamPose.pose.position.y, 1f);
                    Util.AddFloatField("z", ref CamPose.pose.position.z, 1f);
                }
                else if (cameraType == CameraType.Space)
                {
                    Vector3 pos = UPosition;
                    Util.AddFloatField("ux", ref pos.x, 100f);
                    Util.AddFloatField("uy", ref pos.y, 100f);
                    Util.AddFloatField("uz", ref pos.z, 100f);
                    UPosition = pos;
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Label("Rotation".Translate());
                Vector3 rot = CamPose.eulerAngles;
                Util.AddFloatField("pitch", ref rot.x, 10f);
                Util.AddFloatField("yaw", ref rot.y, 10f);
                Util.AddFloatField("roll", ref rot.z, 10f);
                CamPose.eulerAngles = rot;
            }
            GUILayout.EndVertical();

            Util.AddFloatField("Fov", ref CamPose.fov, 1f);
        }
    }
}

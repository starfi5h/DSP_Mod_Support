﻿using BepInEx.Configuration;
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
        public string SectionPrefix { get; set; } = "";
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


        string SectionName => SectionPrefix + "cam-" + Index;

        public CameraPoint(int index)
        {
            Index = index;
        }

        public void Import(ConfigFile configFile = null)
        {
            if (configFile == null) configFile = Plugin.ConfigFile;
            Name = configFile.Bind(SectionName, "Name", "Cam-" + Index).Value;
            cameraType = (CameraType)configFile.Bind(SectionName, "cameraType", 0).Value;
            CamPose.position = configFile.Bind(SectionName, "pose Position", Vector3.zero).Value;
            CamPose.rotation = configFile.Bind(SectionName, "pose Rotation", Quaternion.identity).Value;
            CamPose.fov = configFile.Bind(SectionName, "pose Fov", 0.0f).Value;
            CamPose.near = configFile.Bind(SectionName, "pose Near", 0.0f).Value;
            CamPose.far = configFile.Bind(SectionName, "pose Far", 0.0f).Value;
            UPosition = configFile.Bind(SectionName, "uPosition", VectorLF3.zero).Value;
        }

        public void Export(ConfigFile configFile = null)
        {
            if (configFile == null) configFile = Plugin.ConfigFile;
            configFile.Bind(SectionName, "Name", "Cam-" + Index).Value = Name;
            configFile.Bind(SectionName, "cameraType", 0).Value = (int)cameraType;
            configFile.Bind(SectionName, "pose Position", Vector3.zero).Value = CamPose.position;
            configFile.Bind(SectionName, "pose Rotation", Quaternion.identity).Value = CamPose.rotation;
            configFile.Bind(SectionName, "pose Fov", 0.0f).Value = CamPose.fov;
            configFile.Bind(SectionName, "pose Near", 0.0f).Value = CamPose.near;
            configFile.Bind(SectionName, "pose Far", 0.0f).Value = CamPose.far;
            configFile.Bind(SectionName, "uPosition", VectorLF3.zero).Value = UPosition;
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


        static int positionType = 0;
        static readonly string[] positionTypeTexts = { "Cartesian", "Polar" };

        public void ConfigWindowFunc()
        {
            string tmpString;

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set to Current View".Translate()))
            {
                if (GameMain.localPlanet != null) SetPlanetCamera();
                else SetSpaceCamera();
            }
            if (GUILayout.Button(Plugin.FreePoser.Enabled ? "[Adjusting]".Translate() : "Adjust Mode".Translate()))
            {
                if (Plugin.FreePoser.Enabled)
                {
                    Plugin.FreePoser.Enabled = false;
                }
                else if (CanView)
                {
                    Plugin.FreePoser.Enabled = true;
                    Plugin.ViewingCam = this;
                }
                else
                {
                    UIRealtimeTip.Popup("Camera type mismatch to current environment!".Translate());
                }
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
            cameraType = (CameraType)GUILayout.Toolbar((int)cameraType, cameraTypeTexts);
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                if (cameraType == CameraType.Planet)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Position".Translate(), GUILayout.MinWidth(70));
                    positionType = GUILayout.Toolbar(positionType, positionTypeTexts);
                    GUILayout.EndHorizontal();
                    if (positionType == 0)
                    {
                        Util.AddFloatField("x", ref CamPose.pose.position.x, 1f);
                        Util.AddFloatField("y", ref CamPose.pose.position.y, 1f);
                        Util.AddFloatField("z", ref CamPose.pose.position.z, 1f);
                    }
                    else
                    {
                        var normalizedPos = CamPose.pose.position.normalized;
                        float latitude = Mathf.Asin(normalizedPos.y) * Mathf.Rad2Deg;
                        float longitude = Mathf.Atan2(normalizedPos.x, -normalizedPos.z) * Mathf.Rad2Deg;
                        float altitude = CamPose.pose.position.magnitude;
                        Util.AddFloatField("Log", ref longitude, 1f);
                        Util.AddFloatField("Lat", ref latitude, 1f);
                        Util.AddFloatField("Alt", ref altitude, 1f);
                        CamPose.pose.position = Maths.GetPosByLatitudeAndLongitude(latitude, longitude, altitude);
                    }
                }
                else if (cameraType == CameraType.Space)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Position".Translate(), GUILayout.MinWidth(70));
                    if (GameMain.localStar != null) positionType = GUILayout.Toolbar(positionType, positionTypeTexts);
                    GUILayout.EndHorizontal();
                    if (positionType == 1 && GameMain.localStar != null)
                    {
                        var normalizedPos = (Vector3)(UPosition - GameMain.localStar.uPosition).normalized;
                        float latitude = Mathf.Asin(normalizedPos.y) * Mathf.Rad2Deg;
                        float longitude = Mathf.Atan2(normalizedPos.x, -normalizedPos.z) * Mathf.Rad2Deg;
                        float altitude = (float)(UPosition - GameMain.localStar.uPosition).magnitude;                        
                        Util.AddFloatField("Log", ref longitude, 1f);
                        Util.AddFloatField("Lat", ref latitude, 1f);
                        Util.AddFloatField("Alt", ref altitude, 100f);
                        UPosition = GameMain.localStar.uPosition + (VectorLF3)Maths.GetPosByLatitudeAndLongitude(latitude, longitude, altitude);
                    }
                    else
                    {
                        Vector3 pos = UPosition;
                        Util.AddFloatField("ux", ref pos.x, 100f);
                        Util.AddFloatField("uy", ref pos.y, 100f);
                        Util.AddFloatField("uz", ref pos.z, 100f);
                        UPosition = pos;
                    }
                }
            }
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Rotation".Translate());

                GUILayout.EndHorizontal();
                Vector3 rot = CamPose.eulerAngles;
                Util.AddFloatField("pitch", ref rot.x, 10f);
                Util.AddFloatField("yaw", ref rot.y, 10f);
                Util.AddFloatField("roll", ref rot.z, 10f);
                CamPose.eulerAngles = rot;
            }
            GUILayout.EndVertical();

            Util.AddFloatField("Fov", ref CamPose.fov, 1f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save All".Translate())) Export();
            if (GUILayout.Button("Load All".Translate())) Import();
            GUILayout.EndHorizontal();
        }
    }
}

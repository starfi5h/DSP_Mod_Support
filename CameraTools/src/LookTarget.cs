using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CameraTools
{
    public class LookTarget
    {
        
        public enum TargetType
        {
            None,
            Mecha,
            Planet,
            Space
        }

        public TargetType Type;
        public VectorLF3 Position;

        public void Import(string sectionName, ConfigFile configFile = null)
        {
            if (configFile == null) configFile = Plugin.ConfigFile;
            Type = (TargetType)configFile.Bind(sectionName, "TargetType", 0).Value;
            Position = configFile.Bind(sectionName, "TargetPosition", VectorLF3.zero).Value;
        }

        public void Export(string sectionName, ConfigFile configFile = null)
        {
            if (configFile == null) configFile = Plugin.ConfigFile;
            configFile.Bind(sectionName, "TargetType", 0).Value = (int)Type;
            configFile.Bind(sectionName, "TargetPosition", VectorLF3.zero).Value = Position;
        }


        // UI and Indicator (ping sphere)
        static GameObject markerGo;
        static float markerSize = 3;
        static readonly string[] tragetTypeTexts = { "None", "Mecha", "Planet", "Space" };
        static readonly VectorLF3[] uiPositions = new VectorLF3[4];
        static int positionType = 0;
        static readonly string[] positionTypeTexts = { "Cartesian", "Polar" };
        static Vector2 scrollpos;
        static VectorLF3 lastPosition;

        public static void OnAwake()
        {
            markerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(markerGo.GetComponent<SphereCollider>());            
            markerGo.GetComponent<MeshRenderer>().material = null;
            markerGo.SetActive(false);
        }

        public static void OnDestory()
        {
            Object.Destroy(markerGo);
        }

        public static void OnUpdate()
        {
            if (markerGo == null) return;
            if (UIWindow.EditingTarget == null || markerSize <= 0f)
            {
                markerGo.SetActive(false);
                return;
            }
            var target = UIWindow.EditingTarget;
            switch (target.Type)
            {
                case TargetType.Mecha:
                    markerGo.transform.position = GameMain.mainPlayer.position + (Vector3)target.Position;
                    break;

                case TargetType.Planet:
                    markerGo.transform.position = target.Position;
                    break;

                case TargetType.Space:
                    markerGo.transform.position = target.Position - GameMain.mainPlayer.uPosition;
                    break;

                default:
                    markerGo.SetActive(false);
                    return;
            }
            markerGo.transform.localScale = Vector3.one * markerSize;
            markerGo.SetActive(true);
        }

        public static void OpenAndSetWindow(LookTarget target)
        {
            UIWindow.EditingTarget = target;
            uiPositions[(int)target.Type] = target.Position;
        }

        public Quaternion SetRotation(Vector3 camPos, VectorLF3 camUpos)
        {
            switch (Type)
            {
                case TargetType.Mecha:
                    return Quaternion.LookRotation((GameMain.mainPlayer.position + (Vector3)Position) - camPos, camPos);

                case TargetType.Planet:
                    return Quaternion.LookRotation((Vector3)Position - camPos, camPos);

                case TargetType.Space:
                    if (camUpos == VectorLF3.zero && GameMain.localPlanet != null) // unset: planet camera
                    {
                        // In PlanetData.UpdateRuntimePose:
                        // this.runtimeLocalSunDirection = Maths.QInvRotate(this.runtimeRotation, -vectorLF);
                        camUpos = Maths.QInvRotate(GameMain.localPlanet.runtimeRotation, GameMain.localPlanet.uPosition + (VectorLF3)camPos);
                    }
                    return Quaternion.LookRotation(Position - camUpos, Vector3.up);
            }
            return Quaternion.identity; // Should not reach
        }

        public void ConfigWindowFunc()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type".Translate(), GUILayout.MinWidth(35));

            int targetType = GUILayout.Toolbar((int)Type, Extensions.TL(tragetTypeTexts));
            if (targetType != (int)Type)
            {
                // Set position to the stored uiPosition
                Position = uiPositions[targetType];
                Type = (TargetType)targetType;
            }
            GUILayout.EndHorizontal();

            scrollpos = GUILayout.BeginScrollView(scrollpos);
            bool hasChanged = false;
            var beforePosition = Position;
            GUILayout.BeginVertical(GUI.skin.box);
            {
                switch (Type)
                {
                    case TargetType.None:
                    {
                        if (GameMain.localPlanet != null)
                        {
                            GUILayout.BeginVertical(GUI.skin.box);
                            GUILayout.Label("Local Mecha Coordinates Info".Translate());
                            var normalizedPos = GameMain.mainPlayer.position.normalized;
                            float latitude = Mathf.Asin(normalizedPos.y) * Mathf.Rad2Deg;
                            float longitude = Mathf.Atan2(normalizedPos.x, -normalizedPos.z) * Mathf.Rad2Deg;
                            float altitude = (float)GameMain.mainPlayer.position.magnitude;
                            GUILayout.Label("xyz:\t".Translate() + GameMain.mainPlayer.position.ToString("F1"));
                            GUILayout.Label("polar:\t".Translate() + $"({longitude:F1}, {latitude:F1}, {altitude:F1})");
                            GUILayout.EndVertical();
                        }

                        GUILayout.BeginVertical(GUI.skin.box);
                        GUILayout.Label("Universal Coordinates Info".Translate());
                        GUILayout.Label("Meaha:\t".Translate() + Util.ToString(GameMain.mainPlayer.uPosition));
                        if (GameMain.localPlanet != null)
                        {
                            
                            GUILayout.Label("Local Planet:\t".Translate() + Util.ToString(GameMain.localPlanet.uPosition));
                        }
                        if (GameMain.localStar != null)
                        {
                            GUILayout.Label("Local Star:\t".Translate() + Util.ToString(GameMain.localStar.uPosition));
                        }
                        GUILayout.EndVertical();
                        break;
                    }
                    case TargetType.Mecha:
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Offset to Mecha".Translate(), GUILayout.MinWidth(70));
                        positionType = GUILayout.Toolbar(positionType, Extensions.TL(positionTypeTexts));
                        GUILayout.EndHorizontal();
                        if (positionType == 0)
                        {
                            hasChanged |= Util.AddDoubleField("x", ref Position.x, 1f);
                            hasChanged |= Util.AddDoubleField("y", ref Position.y, 1f);
                            hasChanged |= Util.AddDoubleField("z", ref Position.z, 1f);
                        }
                        else
                        {
                            var normalizedPos = (Vector3)Position.normalized;
                            float latitude = Mathf.Asin(normalizedPos.y) * Mathf.Rad2Deg;
                            float longitude = Mathf.Atan2(normalizedPos.x, -normalizedPos.z) * Mathf.Rad2Deg;
                            float altitude = (float)Position.magnitude;
                            hasChanged |= Util.AddFloatField("Log", ref longitude, 1f);
                            hasChanged |= Util.AddFloatField("Lat", ref latitude, 1f);
                            hasChanged |= Util.AddFloatField("Alt", ref altitude, 1f);
                            Position = Maths.GetPosByLatitudeAndLongitude(latitude, longitude, altitude);
                        }
                        break;
                    }
                    case TargetType.Planet:
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Local Position".Translate(), GUILayout.MinWidth(70));
                        positionType = GUILayout.Toolbar(positionType, Extensions.TL(positionTypeTexts));
                        GUILayout.EndHorizontal();
                        if (positionType == 0)
                        {
                            hasChanged |= Util.AddDoubleField("x", ref Position.x, 1f);
                            hasChanged |= Util.AddDoubleField("y", ref Position.y, 1f);
                            hasChanged |= Util.AddDoubleField("z", ref Position.z, 1f);
                        }
                        else
                        {
                            var normalizedPos = (Vector3)Position.normalized;
                            float latitude = Mathf.Asin(normalizedPos.y) * Mathf.Rad2Deg;
                            float longitude = Mathf.Atan2(normalizedPos.x, -normalizedPos.z) * Mathf.Rad2Deg;
                            float altitude = (float)Position.magnitude;
                            hasChanged |= Util.AddFloatField("Log", ref longitude, 1f);
                            hasChanged |= Util.AddFloatField("Lat", ref latitude, 1f);
                            hasChanged |= Util.AddFloatField("Alt", ref altitude, 1f);
                            Position = Maths.GetPosByLatitudeAndLongitude(latitude, longitude, altitude);
                        }
                        break;
                    }
                    case TargetType.Space:
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Space Position".Translate(), GUILayout.MinWidth(70));
                        if (GameMain.localStar != null) positionType = GUILayout.Toolbar(positionType, Extensions.TL(positionTypeTexts));
                        GUILayout.EndHorizontal();
                        if (positionType == 1 && GameMain.localStar != null)
                        {
                            var normalizedPos = (Vector3)(Position - GameMain.localStar.uPosition).normalized;
                            float latitude = Mathf.Asin(normalizedPos.y) * Mathf.Rad2Deg;
                            float longitude = Mathf.Atan2(normalizedPos.x, -normalizedPos.z) * Mathf.Rad2Deg;
                            float altitude = (float)(Position - GameMain.localStar.uPosition).magnitude;
                            hasChanged |= Util.AddFloatField("Log", ref longitude, 1f);
                            hasChanged |= Util.AddFloatField("Lat", ref latitude, 1f);
                            hasChanged |= Util.AddFloatField("Alt", ref altitude, 100f);
                            Position = GameMain.localStar.uPosition + (VectorLF3)Maths.GetPosByLatitudeAndLongitude(latitude, longitude, altitude);
                        }
                        else
                        {
                            hasChanged |= Util.AddDoubleField("ux", ref Position.x, 100f);
                            hasChanged |= Util.AddDoubleField("uy", ref Position.y, 100f);
                            hasChanged |= Util.AddDoubleField("uz", ref Position.z, 100f);
                        }
                        break;
                    }
                }
                uiPositions[targetType] = Position;
            }
            GUILayout.EndVertical();

            if (Type != TargetType.None)
            {
                Util.AddFloatFieldInput("Marker Size".Translate(), ref markerSize, 1f);
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Set to mecha Position".Translate()))
                {
                    switch (Type)
                    {
                        case TargetType.Planet: 
                            Position = GameMain.mainPlayer.position; hasChanged = true;
                            break;
                        case TargetType.Space:
                            Position = GameMain.mainPlayer.uPosition; hasChanged = true;
                            break;
                    }
                }
                if (GUILayout.Button("Undo".Translate()))
                {                    
                    Position = lastPosition;
                    lastPosition = beforePosition;
                }
                if (hasChanged)
                {
                    lastPosition = beforePosition;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }
    }
}

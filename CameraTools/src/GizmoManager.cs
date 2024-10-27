using System.Collections.Generic;
using UnityEngine;
using static CameraTools.LookTarget;

namespace CameraTools
{
    public static class GizmoManager
    {
        public static float TargetMarkerSize = 3f;
        public static float PathMarkerSize = 3f;

        static GameObject targetMarkerGo;
        static LineGizmo cameraPathLine;
        static GameObject cameraObjGroup;
        static readonly List<GameObject> cameraObjs = new();
        static float lastUpdateTime;

        public static void OnAwake()
        {
            targetMarkerGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(targetMarkerGo.GetComponent<SphereCollider>());
            targetMarkerGo.GetComponent<MeshRenderer>().material = null;
            targetMarkerGo.SetActive(false);

            cameraObjGroup = new GameObject();
        }

        public static void OnDestroy()
        {
            Object.Destroy(targetMarkerGo);
            Object.Destroy(cameraObjGroup);
            cameraPathLine?.Close();
        }

        public static void OnPathChange()
        {
            lastUpdateTime = 0;
        }

        public static void OnUpdate()
        {
            UpdateTargetMarker();
            UpdatePathMarker();

            if (Time.time - lastUpdateTime > 1.0f)
            {
                lastUpdateTime = Time.time;
                if (UIWindow.EditingPath != null && UIWindow.EditingTarget != null && PathMarkerSize > 0f)
                {
                    if (cameraPathLine != null)
                    {
                        cameraPathLine.validPointCount = UIWindow.EditingPath.SetPathPoints(cameraPathLine.points);
                        cameraPathLine.width = PathMarkerSize;
                        cameraPathLine.ManualRefresh();
                    }

                    int cameraCount = UIWindow.EditingPath.GetCameraCount();
                    for (int i = cameraObjs.Count; i < cameraCount; i++)
                    {
                        var camGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        camGo.GetComponent<MeshRenderer>().material = null;
                        camGo.transform.parent = cameraObjGroup.transform;
                        cameraObjs.Add(camGo);
                    }
                    cameraCount = UIWindow.EditingPath.SetCameraPoints(cameraObjs);
                    for (int i = 0; i < cameraCount; i++)
                    {
                        cameraObjs[i].transform.localScale = Vector3.one * PathMarkerSize;
                        cameraObjs[i].SetActive(true);
                    }
                    for (int i = cameraCount; i < cameraObjs.Count; i++)
                    {
                        cameraObjs[i].SetActive(false);
                    }
                }
            }
        }

        static void UpdateTargetMarker()
        {
            if (UIWindow.EditingTarget == null || TargetMarkerSize <= 0f)
            {
                targetMarkerGo.SetActive(false);
                return;
            }
            var target = UIWindow.EditingTarget;
            switch (target.Type)
            {
                case TargetType.Mecha:
                    targetMarkerGo.transform.position = GameMain.mainPlayer.position + (Vector3)target.Position;
                    break;

                case TargetType.Planet:
                    targetMarkerGo.transform.position = target.Position;
                    break;

                case TargetType.Space:
                    targetMarkerGo.transform.position = target.Position - GameMain.mainPlayer.uPosition;
                    break;

                default:
                    targetMarkerGo.SetActive(false);
                    return;
            }
            targetMarkerGo.transform.localScale = Vector3.one * TargetMarkerSize;
            targetMarkerGo.SetActive(true);
        }

        static void UpdatePathMarker()
        {
            if (UIWindow.EditingTarget == null || PathMarkerSize <= 0f)
            {
                cameraObjGroup.SetActive(false);
                cameraPathLine?.Close();
                cameraPathLine = null;
                return;
            }
            cameraObjGroup.SetActive(true);
            if (cameraPathLine == null)
            {                
                cameraPathLine = LineGizmo.Create(1, new Vector3[360], 0);
                cameraPathLine.spherical = false;
                cameraPathLine.autoRefresh = false;
                cameraPathLine.width = PathMarkerSize;
                cameraPathLine.color = Color.green;
                cameraPathLine.Open();
            }
        }

    }
}

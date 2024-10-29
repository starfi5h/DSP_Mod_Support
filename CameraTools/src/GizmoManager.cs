using System.Collections.Generic;
using UnityEngine;
using static CameraTools.LookTarget;

namespace CameraTools
{
    public static class GizmoManager
    {
        public static float TargetMarkerSize = 3f;
        public static float PathMarkerSize = 5f;
        public static float PathCameraCubeSize = 2f;
        public const int LinePointCount = 180;

        static GameObject targetMarkerGo;
        static LineGizmo cameraPathLine;
        static LineGizmo lookAtLine;
        static readonly VectorLF3[] lineUPoints = new VectorLF3[LinePointCount];
        static readonly List<VectorLF3> cameraUPointList = new();
        static GameObject cameraObjGroup;
        static readonly List<GameObject> cameraObjs = new();
        static float lastUpdateTime;
        static bool hasErrored;

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
            lookAtLine?.Close();
        }

        public static void OnPathChange()
        {
            lastUpdateTime = 0;
        }

        public static void OnUpdate()
        {
            if (GameMain.mainPlayer == null) return;
            try
            {
                if (Time.time - lastUpdateTime > 0.5f)
                {
                    lastUpdateTime = Time.time;
                    RefreshPathPreview();
                }
                UpdateTargetMarker();
                UpdatePathMarker();
            }
            catch (System.Exception ex)
            {
                if (!hasErrored) Plugin.Log.LogError(ex);
                hasErrored = true;
            }
        }

        static void RefreshPathPreview()
        {
            if (UIWindow.EditingPath is { Preview: true } && PathMarkerSize > 0f)
            {
                if (cameraPathLine == null || cameraPathLine.points == null)
                {
                    cameraPathLine = LineGizmo.Create(1, new Vector3[LinePointCount], 0);
                    cameraPathLine.spherical = false;
                    cameraPathLine.autoRefresh = false;
                    cameraPathLine.width = PathMarkerSize;
                    cameraPathLine.color = Color.green;
                    cameraPathLine.Open();
                }
                if (cameraPathLine != null)
                {
                    cameraPathLine.validPointCount = UIWindow.EditingPath.SetPathPoints(cameraPathLine.points, lineUPoints);
                    cameraPathLine.width = PathMarkerSize;
                    cameraPathLine.RefreshGeometry();
                }

                int cameraCount = UIWindow.EditingPath.GetCameraCount();
                for (int i = cameraObjs.Count; i < cameraCount; i++)
                {
                    var camGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    camGo.GetComponent<MeshRenderer>().material = null;
                    camGo.transform.parent = cameraObjGroup.transform;
                    cameraObjs.Add(camGo);
                }
                cameraCount = UIWindow.EditingPath.SetCameraPoints(cameraObjs, cameraUPointList);
                for (int i = 0; i < cameraCount; i++)
                {
                    cameraObjs[i].transform.localScale = Vector3.one * PathCameraCubeSize;
                    cameraObjs[i].SetActive(true);
                }
                for (int i = cameraCount; i < cameraObjs.Count; i++)
                {
                    cameraObjs[i].SetActive(false);
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

                case TargetType.Local:
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
            if (UIWindow.EditingPath == null || PathMarkerSize <= 0f || !UIWindow.EditingPath.Preview)
            {
                cameraObjGroup.SetActive(false);
                cameraPathLine?.Close();
                cameraPathLine = null;
                lookAtLine?.Close();
                lookAtLine = null;
                return;
            }
            cameraObjGroup.SetActive(true);

            if (GameMain.localPlanet == null && GameMain.mainPlayer != null)
            {
                if (cameraPathLine != null)
                {
                    for (int i = 0; i < LinePointCount; i++)
                    {
                        cameraPathLine.points[i] = lineUPoints[i] - GameMain.mainPlayer.uPosition;
                    }
                    cameraPathLine.RefreshGeometry();
                }
                for (int i = 0; i < cameraUPointList.Count; i++)
                {
                    cameraObjs[i].transform.position = cameraUPointList[i] - GameMain.mainPlayer.uPosition;
                }
            }
            if (lookAtLine == null)
            {
                lookAtLine = LineGizmo.Create(2, Vector3.zero, Vector3.zero);
                lookAtLine.spherical = false;
                lookAtLine.autoRefresh = false;
                lookAtLine.width = 0;
                lookAtLine.color = Color.white;
                lookAtLine.tiling = false;
                lookAtLine.Open();
            }
            if (lookAtLine != null)
            {
                if (UIWindow.EditingPath.SetLookAtLineRealtime(out Vector3 startPoint, out Vector3 dir))
                {
                    lookAtLine.width = PathMarkerSize;
                    lookAtLine.startPoint = startPoint;
                    lookAtLine.endPoint = startPoint + dir * 15;
                    lookAtLine.RefreshGeometry();
                }
            }
        }
    }
}

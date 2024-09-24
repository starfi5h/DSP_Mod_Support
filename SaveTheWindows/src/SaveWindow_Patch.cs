using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace SaveTheWindows
{
    public class SaveWindow_Patch
    {
        [HarmonyPostfix, HarmonyPatch(typeof(UIRoot), nameof(UIRoot.OpenGameUI))]
        public static void OpenGameUI()
        {
            CollectWindows();
            LoadWindowPos();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        public static void OnGameEnd()
        {
            SaveWindowPos();
        }

        readonly static List<UIWindowDrag> _windows = new();

        static void CollectWindows()
        {
            // Unhandled classes:
            // UIWindow where canDrag = true
            Transform windows = UIRoot.instance.overlayCanvas.transform.Find("In Game/Windows");
            if (windows != null)
            {
                _windows.Clear();
                windows.GetComponentsInChildren(true, _windows);
                Plugin.Log.LogInfo($"Collect {_windows.Count} windows");
            }
            else
            {
                Plugin.Log.LogWarning("Can't find In Game/Windows");
            }
        }

        static void LoadWindowPos()
        {
            foreach (var window in _windows)
            {
                if (window?.dragTrans == null) continue;
                var name = window.name;
                var transform = window.dragTrans;
                var pos = Plugin.ConfigFile.Bind("Window Position", name, Vector2.zero).Value;
                if (pos == Vector2.zero) continue;
                transform.anchoredPosition = pos;
            }
        }

        static void SaveWindowPos()
        {
            foreach (var window in _windows)
            {
                if (window?.dragTrans == null) continue;
                var name = window.name;
                var transform = window.dragTrans;
                var pos = new Vector2(Mathf.RoundToInt(transform.anchoredPosition.x), Mathf.RoundToInt(transform.anchoredPosition.y));
                Plugin.ConfigFile.Bind("Window Position", name, Vector2.zero).Value = pos;
            }
        }
    }
}

using HarmonyLib;
using System;

namespace RateMonitor.Patches
{
    public class MainPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogic), nameof(GameLogic.LogicFrame))]
        static void UpdateMonitor(bool __runOriginal)
        {
            // If other mods have stop factory simulation, then skip
            if (Plugin.MainTable == null || !__runOriginal) return;

            try
            {
                Plugin.MainTable.OnGameTick();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                Plugin.MainTable.OnError();
            }
            if (UI.UIWindow.InResizingArea) UICursor.SetCursor(ECursor.Horizontal);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.End))]
        static void OnGameEnd()
        {
            Plugin.MainTable = null;
            UI.UIWindow.SaveUIWindowConfig();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFInput), nameof(VFInput.OnUpdate))]
        static void PreventZoomInWindow()
        {
            if (UI.UIWindow.InWindow) VFInput.mouseWheel = 0f;
        }
    }
}

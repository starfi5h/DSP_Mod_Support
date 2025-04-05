using HarmonyLib;
using UnityEngine;

namespace RateMonitor
{
    public class SelectionTool_Patches
    {
        public static SelectionTool tool;

        [HarmonyPostfix, HarmonyAfter("org.kremnev8.plugin.BlueprintTweaks")]
        [HarmonyPatch(typeof(PlayerAction_Build), nameof(PlayerAction_Build.Init))]
        public static void InitTool(PlayerAction_Build __instance)
        {
            // tool._Init is called on PlayerAction_Build.SetReady
            // To hot-reload, exit the game and load the save again after F6
            BuildTool[] buildTools = __instance.tools;
            if (buildTools == null) return;
            BuildTool[] ourTools = new BuildTool[buildTools.Length + 1];
            buildTools.CopyTo(ourTools, 0);
            tool = new SelectionTool();
            tool.OnSelectionFinish += Plugin.OnSelectionFinish;
            ourTools[ourTools.Length - 1] = tool;
            __instance.tools = ourTools;
            Plugin.Log.LogDebug("Add SelectionTool. Total tools count: " + ourTools.Length);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAction_Build), nameof(PlayerAction_Build.DetermineActive))]
        public static void UpdateCommandState(PlayerAction_Build __instance, ref bool __result)
        {
            // Modify from PlayerController.UpdateCommandState
            if (tool.IsEnable && __instance.activeTool == tool)
            {
                __result = true; // keep PlayerAction_Build active
                return;
            }
            if (VFInput.readyToBuild && VFInput.inScreen && IsHotKey())
            {
                // Modify from OpenBlueprintCopyMode()
                if (__instance.blueprintMode != EBlueprintMode.None) return;

                Plugin.Log.LogDebug("Enable selection tool");
                __instance.player.controller.cmd.SetNoneCommand();
                tool.IsEnable = true;
                __result = true;
            }
        }

        static bool IsHotKey()
        {
#if !DEBUG
            // Modify from VFInput._pasteKey, as it doesn't support modified key
            if (PluginCAPIcompat.IsRegisiter)
            {
                return PluginCAPIcompat.IsPressed();
            }
#endif
            return ModSettings.SelectionToolKey.Value.IsPressed();
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(UIBuildingGrid), nameof(UIBuildingGrid.Update))]
        public static void UpdateGrid(UIBuildingGrid __instance)
        {
            Player mainPlayer = GameMain.mainPlayer;
            PlanetFactory planetFactory = GameMain.localPlanet?.factory;
            if (planetFactory == null) return;
            if (GameMain.localPlanet.type == EPlanetType.Gas) return;
            if (tool == null || !tool.active) return;

            PlayerAction_Build actionBuild = mainPlayer?.controller?.actionBuild;
            if (actionBuild == null) return;
            if (actionBuild.blueprintMode != EBlueprintMode.None) return;

            PlanetGrid planetGrid = null;
            if (GameMain.localPlanet != null && GameMain.localPlanet.aux != null && GameMain.localPlanet.aux.activeGridIndex < GameMain.localPlanet.aux.customGrids.Count)
            {
                planetGrid = GameMain.localPlanet.aux.customGrids[GameMain.localPlanet.aux.activeGridIndex];
            }
            if (planetGrid == null) return;

            // Highlight the selecting area on grid
            Vector4 vector = Vector4.zero;
            if (tool.IsSelecting)
            {
                __instance.gridRnd.enabled = actionBuild.blueprintMode == EBlueprintMode.None;
                __instance.gridRnd.transform.localScale = new Vector3(__instance.displayScale, __instance.displayScale, __instance.displayScale);
                __instance.gridRnd.transform.rotation = planetGrid.rotation;

                // using code part in if(mode == -2) of the upgrade tool
                __instance.material.SetColor("_TintColor", new Color(0.5f, 0.6f, 0f)); // Color of the gird
                __instance.material.SetFloat("_ReformMode", 0f);
                __instance.material.SetFloat("_ZMin", -0.5f);
                vector = (Vector4)tool.selectGratBox;
            }
            else
            {

            }
            __instance.material.SetVector("_CursorGratBox", vector);
        }
    }
}

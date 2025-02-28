using HarmonyLib;
using System;
using UnityEngine;

namespace MassRecipePaste
{
    public class Patches
    {
        public static bool isCustomHotkey = false;
        public static DragPasteTool tool;

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
            tool = new DragPasteTool();
            ourTools[ourTools.Length - 1] = tool;
            __instance.tools = ourTools;
            Plugin.Log.LogDebug("Add DragPasteTool. Total tools count: " + ourTools.Length);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerAction_Build), nameof(PlayerAction_Build.DetermineActive))]
        public static void UpdateCommandState(PlayerAction_Build __instance, ref bool __result)
        {
            // Modify from PlayerController.UpdateCommandState
            if (tool.isEnable && __instance.activeTool == tool)
            {
                __result = true; // keep PlayerAction_Build active
                return;
            }
            if (VFInput.readyToBuild && VFInput.inScreen && IsHotKey())
            {
                // Modify from OpenBlueprintCopyMode()
                if (__instance.blueprintMode != EBlueprintMode.None) return;

                Plugin.Log.LogDebug("Enter PasteMode: " + BuildingParameters.clipboard.type + " - " + BuildingParameters.clipboard.recipeType);
                __instance.player.controller.cmd.SetNoneCommand();
                tool.isEnable = true;
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
            if (isCustomHotkey)
            {
                return Plugin.MassPasteKey.Value.IsPressed();
            }

            if (!VFInput.override_keys[31].IsNull())
            {
                return VFInput.control && VFInput._InputValueNoCombatOrFullscreenOrScreenshot(VFInput.axis_combine_key, 31).onDown;
            }
            return VFInput.control && VFInput._InputValueNoCombatOrFullscreenOrScreenshot(VFInput.axis_button, 33).onDown;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIRealtimeTip), nameof(UIRealtimeTip.Popup), new Type[] { typeof(string), typeof(bool), typeof(int) } )]
        public static bool Blocker()
        {
            if (tool.isPasting)
            {
                tool.pastedCount++;
                return false;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
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

            Vector4 vector = Vector4.zero;
            if (tool.isSelecting)
            {
                __instance.gridRnd.enabled = actionBuild.blueprintMode == EBlueprintMode.None;
                __instance.gridRnd.transform.localScale = new Vector3(__instance.displayScale, __instance.displayScale, __instance.displayScale);
                __instance.gridRnd.transform.rotation = planetGrid.rotation;

                // using code part in if(mode == -2) of the upgrade tool
                __instance.material.SetColor("_TintColor", new Color(0.4f, 0.5f, 0f)); // Color of the gird
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

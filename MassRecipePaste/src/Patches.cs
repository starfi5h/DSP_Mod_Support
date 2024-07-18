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
            // Modify from VFInput._pasteKey, as it doesn't support modified key
            if (PluginCAPIcompat.IsRegisiter)
            {
                return PluginCAPIcompat.IsPressed();
            }
            else if (isCustomHotkey)
            {
                return Plugin.MassPasteKey.Value.IsPressed();
            }

            if (!VFInput.override_keys[31].IsNull())
            {
                return VFInput.control && VFInput._InputValueNoCombatOrFullscreen(VFInput.axis_combine_key, 31).onDown;
            }
            return VFInput.control && VFInput._InputValueNoCombatOrFullscreen(VFInput.axis_button, 33).onDown;
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


        static readonly int cursorGratBox = Shader.PropertyToID("_CursorGratBox");
        static readonly int selectColor = Shader.PropertyToID("_SelectColor");
        static readonly int tintColor = Shader.PropertyToID("_TintColor");
        static readonly int showDivideLine = Shader.PropertyToID("_ShowDivideLine");

        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        [HarmonyPatch(typeof(UIBuildingGrid), nameof(UIBuildingGrid.Update))]
        public static void UpdateGrid(UIBuildingGrid __instance)
        {
            Player mainPlayer = GameMain.mainPlayer;
            PlanetFactory planetFactory = GameMain.localPlanet?.factory;
            if (planetFactory == null) return;
            if (GameMain.localPlanet.type == EPlanetType.Gas) return;

            PlayerAction_Build actionBuild = mainPlayer?.controller.actionBuild;
            if (actionBuild == null) return;
            if (actionBuild.blueprintMode != EBlueprintMode.None) return;

            if (!tool.active) return;

            if (tool.isSelecting)
            {
                __instance.blueprintMaterial.SetColor(tintColor, __instance.blueprintColor); // Color.clear
                __instance.blueprintMaterial.SetVector(cursorGratBox, (Vector4)tool.selectGratBox);
                __instance.blueprintMaterial.SetVector(selectColor, __instance.buildColor); 
                __instance.blueprintMaterial.SetFloat(showDivideLine, 0f);
                __instance.blueprintGridRnd.enabled = true;
            }
            else
            {
                __instance.blueprintMaterial.SetColor(tintColor, __instance.blueprintColor);
                __instance.blueprintMaterial.SetVector(cursorGratBox, Vector4.zero);
                __instance.blueprintMaterial.SetVector(selectColor, Vector4.one);
                __instance.blueprintMaterial.SetFloat(showDivideLine, 0f);
                __instance.blueprintGridRnd.enabled = false;
            }

            for (int l = 0; l < 64; l++)
            {
                __instance.blueprintMaterial.SetVector($"_CursorGratBox{l}", Vector4.zero);
                __instance.blueprintMaterial.SetFloat($"_CursorGratBoxInfo{l}", 0f);
            }
        }
    }
}

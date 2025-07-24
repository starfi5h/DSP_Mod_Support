﻿using BepInEx;
using HarmonyLib;
using ModFixerOne.Mods;
using System;

namespace ModFixerOne
{
    public static class Fixer_Patch
    {
        public static string ErrorMessage = "";

        public static void OnAwake()
        {
            try
            {
                Harmony harmony = Plugin.Instance.Harmony;
                /*
                if (GameConfig.gameVersion < new Version(0, 10, 33))
                {
                    ErrorMessage = $"Modfixer " + Plugin.VERSION + " only support game version 0.10.33 or higher!\nModfixer仅支援0.10.33以上游戏版本!";
                    Plugin.Log.LogWarning(ErrorMessage);
                    harmony.PatchAll(typeof(Fixer_Patch));
                    return;
                }
                */
                if (!IsVaild())
                {
                    ErrorMessage = "Can't find injected fields or methods!\n Please make sure that ModFixerOnePreloader.dll is installed in BepInEx\\patchers.";
                    Plugin.Log.LogWarning(ErrorMessage);
                    harmony.PatchAll(typeof(Fixer_Patch));
                    return;
                }

                //Nebula_Patch.OnAwake(harmony);
                if (ErrorMessage != "")
                {
                    ErrorMessage = "Error occurred when patching following mods:" + ErrorMessage;
                    Plugin.Log.LogWarning(ErrorMessage);
                    harmony.PatchAll(typeof(Fixer_Patch));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        public static void OnStart()
        {
            try
            {
                Harmony harmony = Plugin.Instance.Harmony;
                if (!IsVaild())
                {
                    ErrorMessage = "Can't find injected fields or methods!\n Please make sure that ModFixerOnePreloader.dll is installed in BepInEx\\patchers.";
                    Plugin.Log.LogWarning(ErrorMessage);
                    harmony.PatchAll(typeof(Fixer_Patch));
                    return;
                }

                harmony.PatchAll(typeof(Common_Patch));
                LDBTool_Patch.Init(harmony);
                if (ErrorMessage != "")
                {
                    ErrorMessage = "Error occurred when patching following mods:" + ErrorMessage;
                    harmony.PatchAll(typeof(Fixer_Patch));
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        internal static void InvokeOnLoadWorkEnded()
        {
            try
            {
                ShowMessageBox();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        private static void ShowMessageBox()
        {
            UIMessageBox.Show("Mod Fixer One Error", ErrorMessage, "确定".Translate(), 3);
        }

        private static bool IsVaild()
        {
            var type = AccessTools.TypeByName("Language");
            if (type == null) return false;

            type = AccessTools.TypeByName("StringTranslate");
            if (type == null) return false;

            type = AccessTools.TypeByName("StringProto");
            if (type == null) return false;

            return true;
        }
    }
}

using HarmonyLib;
using System;
using System.Reflection;

namespace ModFixerOne
{
    public static class Fixer_Patch
    {
        public static string ErrorMessage = "";
        public static bool initialized = false;

        public static void OnAwake()
        {
            Plugin.Instance.Harmony.PatchAll(typeof(Fixer_Patch));
#if DEBUG
            Init();
            OnUIGameInit(UIRoot.instance.uiGame);
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void Init()
        {
            if (initialized) return;

            //Harmony harmony = Plugin.Instance.Harmony;
            if (ErrorMessage != "")
            {
                ErrorMessage = "Error occurred when patching following mods:" + ErrorMessage;
                UIMessageBox.Show("Mod Fixer One Error", ErrorMessage, "确定".Translate(), 3);
            }
            initialized = true;
            Plugin.Log.LogDebug($"Version: {Plugin.VERSION}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnInit))]
        public static void OnUIGameInit(UIGame __instance)
        {
            if (initialized) return;

            try
            {
                Plugin.Log.LogDebug(typeof(UIGame).GetField("inventory"));
                AccessTools.Field(typeof(UIGame), "inventory").SetValue(__instance, (ManualBehaviour)__instance.inventoryWindow);
            }
            catch (Exception e)
            {
                ErrorMessage = "Can't patch UIGame.inventory!\nCheck if the preloader is installed correctly";
                ErrorMessage += e.ToString();
                Plugin.Log.LogError(ErrorMessage);
            }
        }
    }
}

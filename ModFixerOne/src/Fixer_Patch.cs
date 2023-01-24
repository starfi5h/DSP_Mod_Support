using BepInEx;
using HarmonyLib;
using ModFixerOne.Mods;

namespace ModFixerOne
{
    public static class Fixer_Patch
    {
        public static string ErrorMessage = "";
        public static bool initialized = false;
        public static bool flag = false;

        public static void OnAwake()
        {
            Plugin.Instance.Harmony.PatchAll(typeof(Fixer_Patch));
#if DEBUG
            Init();
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void Init()
        {
            if (initialized) return;

            Harmony harmony = Plugin.Instance.Harmony;

            if (typeof(UIGame).GetField("inventory") == null || typeof(PlanetTransport).GetMethod("RefreshTraffic") == null)
            {
                ErrorMessage = "Can't find added fields or methods!\n Please check ModFixerOnePreloader.dll is installed correctly.";                
                Plugin.Log.LogWarning(ErrorMessage);
                UIMessageBox.Show("Mod Fixer One Preloader Error", ErrorMessage, "确定".Translate(), 3);
                initialized = true;
                return;
            }

            LongArm.Init(harmony);
            PersonalLogistics.Init(harmony);
            AutoStationConfig.Init(harmony);
            Dyson4DPocket.Init(harmony);

            if (ErrorMessage != "")
            {
                ErrorMessage = "Error occurred when patching following mods:" + ErrorMessage;
                UIMessageBox.Show("Mod Fixer One Error", ErrorMessage, "确定".Translate(), 3);
            }
            initialized = true;
        }
    }
}

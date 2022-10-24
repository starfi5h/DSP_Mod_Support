using HarmonyLib;
using ModFixerOne.Mods;

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
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void Init()
        {
            if (initialized) return;

            Harmony harmony = Plugin.Instance.Harmony;

            var fieldInfo = typeof(UIGame).GetField("inventory");
            if (fieldInfo != null)
            {
                LongArm.Init(harmony);
                PersonalLogistics.Init(harmony);
            }
            else
            {
                ErrorMessage = "Can't find UIGame.inventory! Please check if the preloader is installed correctly.";
            }

            if (ErrorMessage != "")
            {
                ErrorMessage = "Error occurred when patching following mods:" + ErrorMessage;
                UIMessageBox.Show("Mod Fixer One Error", ErrorMessage, "确定".Translate(), 3);
            }
            initialized = true;
        }
    }
}

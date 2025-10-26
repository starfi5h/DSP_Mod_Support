using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

[assembly: AssemblyTitle(UITweaks.Plugin.NAME)]
[assembly: AssemblyVersion(UITweaks.Plugin.VERSION)]

namespace UITweaks
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.UITweaks";
        public const string NAME = "UITweaks";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        static Plugin plugin;
        static Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            plugin = this;
            harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(TechTree_Tweaks));
            harmony.PatchAll(typeof(Station_Tweaks));
            harmony.PatchAll(typeof(UILayout_Tweaks));
            UILayout_Tweaks.OnAwake(Config);

#if DEBUG
            TechTree_Tweaks.Init();
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow.OnApplyClick))]
        public static void OnApplyButtonClick()
        {
            // 當按下"應用設定"按鈕時, 重新載入設定
            plugin.Config.Reload();
        }

#if DEBUG
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
            harmony = null;
            TechTree_Tweaks.Free();
            UILayout_Tweaks.OnDestroy(Config);
        }
#endif
    }
}

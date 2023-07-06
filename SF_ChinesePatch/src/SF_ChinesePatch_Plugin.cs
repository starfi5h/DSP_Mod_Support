using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CommonAPI;
using CommonAPI.Systems;

namespace SF_ChinesePatch
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry))]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.SF_ChinesePatch";
        public const string NAME = "SF_ChinesePatch";
        public const string VERSION = "1.0.0";

        public static ManualLogSource Log;
        Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new(GUID);

            Log.LogInfo(LDB.strings);
            NebulaMultiplayer_Patch.OnAwake();
            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(StringManager));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIMainMenu), nameof(UIMainMenu._OnOpen))]
        public static void UIMainMenu_OnOpen_Postfix()
        {
            Log.LogWarning("UIMainMenu._OnOpen");
            Log.LogInfo("New Game (Host)".Translate());
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UIOptionWindow), nameof(UIOptionWindow._OnCreate))]
        public static void UIOptionWindow_OnCreate_Postfix()
        {
            Log.LogWarning("UIOptionWindow_OnCreate");
        }


        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}

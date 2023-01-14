using HarmonyLib;
using NebulaAPI;
using NebulaCompatibilityAssist.Hotfix;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public static class NC_Patch
    {
        public static bool IsClient;
        public static Action OnLogin;
        public static string RequriedPlugins = ""; // plugins required to install on both end
        public static string ErrorMessage = "";
        public static bool initialized = false;

        public static void OnAwake()
        {
            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());
            Plugin.Instance.Harmony.PatchAll(typeof(NC_Patch));
#if DEBUG
            Init();
            IsClient = NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient;
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        public static void Init()
        {
            if (initialized) return;

            Harmony harmony = Plugin.Instance.Harmony;
            LSTM.Init(harmony);
            DSPMarker.Init(harmony);
            DSPStarMapMemo.Init(harmony);
            DSPBeltReverseDirection.Init(harmony);
            DSPTransportStat_Patch.Init(harmony);
            PlanetFinder_Patch.Init(harmony);
            DSPFreeMechaCustom.Init(harmony);
            AutoStationConfig.Init(harmony);
            Auxilaryfunction.Init(harmony);
            DSPOptimizations.Init(harmony);
            FactoryLocator_Patch.Init(harmony);
            SplitterOverBelt.Init(harmony);
            MoreMegaStructure.Init(harmony);
            DSP_Battle_Patch.Init(harmony);
            NebulaHotfix.Init(harmony);

            if (ErrorMessage != "")
            {
                ErrorMessage = "Error occurred when patching following mods:" + ErrorMessage;
                UIMessageBox.Show("Nebula Compatibility Assist Error", ErrorMessage, "确定".Translate(), 3);
            }
            initialized = true;
            Plugin.Instance.Version = MyPluginInfo.PLUGIN_VERSION + RequriedPlugins;
            Log.Debug($"Version: {Plugin.Instance.Version}");
        }

        public static void OnDestory()
        {
            DSP_Battle_Patch.OnDestory();
            ChatManager.OnDestory();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnGameBegin()
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            {
                IsClient = true;
                Log.Debug("OnLogin");
                OnLogin?.Invoke();
            }
            IsClient = false;
        }

    }
}

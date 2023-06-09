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
            BlueprintTweaks.Init(harmony);
            Dustbin_Patch.Init(harmony);
            Bottleneck_Patch.Init(harmony);
            NebulaHotfix.Init(harmony);

            var title = Localization.language == Language.zhCN ? "联机补丁提示" : "Nebula Compatibility Assist Report";
            var errorMessage = Localization.language == Language.zhCN ? "修改以下mod时出错, 在联机模式中可能无法正常运行:" : "Error occurred when patching the following mods:";
            var incompatMessage = Localization.language == Language.zhCN ? "以下mod和联机mod不相容, 可能将导致错误" : "The following mods are not compatible with multiplayer mod:";

            if (ErrorMessage != "")
            {
                errorMessage += ErrorMessage;
                UIMessageBox.Show(title, errorMessage, "确定".Translate(), 3);
            }
            if (TestIncompatMods(ref incompatMessage))
            {
                UIMessageBox.Show(title, incompatMessage, "确定".Translate(), 3);
            }
            initialized = true;
            Plugin.Instance.Version = Plugin.VERSION + RequriedPlugins;
            Log.Debug($"Version: {Plugin.Instance.Version}");
        }

        public static void OnDestory()
        {
            DSP_Battle_Patch.OnDestory();
            Dustbin_Patch.OnDestory();
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

        static bool TestIncompatMods(ref string incompatMessage)
        {
            int count = 0;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("semarware.dysonsphereprogram.LongArm"))
            {
                incompatMessage += "\nLongArm";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("greyhak.dysonsphereprogram.droneclearing"))
            {
                incompatMessage += "\nDSP Drone Clearing";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("Appun.DSP.plugin.BigFormingSize"))
            {
                incompatMessage += "\nDSPBigFormingSize";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.small.dsp.transferInfo"))
            {
                incompatMessage += "\nTransferInfo";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("cn.blacksnipe.dsp.Multfuntion_mod"))
            {
                incompatMessage += "\nMultfuntion mod";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("greyhak.dysonsphereprogram.beltreversedirection"))
            {
                incompatMessage += "\nDSP Belt Reverse";
                count++;
            }            
            return count > 0;
        }

    }
}

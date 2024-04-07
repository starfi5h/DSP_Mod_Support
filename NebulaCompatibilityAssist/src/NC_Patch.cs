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
            DSPStarMapMemo.Init(harmony);
            PlanetFinder_Patch.Init(harmony);
            DSPFreeMechaCustom.Init(harmony);
            AutoStationConfig.Init(harmony);
            Auxilaryfunction.Init(harmony);
            DSPOptimizations.Init(harmony);
            FactoryLocator_Patch.Init(harmony);
            SplitterOverBelt.Init(harmony);
            MoreMegaStructure.Init(harmony);
            BlueprintTweaks.Init(harmony);
            DSPAutoSorter.Init(harmony);
            NebulaHotfix.Init(harmony);

            var title = Localization.isZHCN ? "联机补丁提示" : "Nebula Compatibility Assist Report";
            var errorMessage = Localization.isZHCN ? "修改以下mod时出错, 在联机模式中可能无法正常运行" : "Error occurred when patching the following mods:";
            var incompatMessage = Localization.isZHCN ? "以下mod和联机mod不相容, 可能将导致错误或不同步\n" : "The following mods are not compatible with multiplayer mod:\n";
            var warnMessage = Localization.isZHCN ? "以下mod的部分功能可能导致联机不同步\n" : "The following mods have some functions that don't sync in multiplayer game:\n";
            var message = "";

            if (ErrorMessage != "")
            {
                errorMessage += ErrorMessage;
                message += errorMessage + "\n";
            }
            if (TestIncompatMods(ref incompatMessage))
            {
                message += incompatMessage;
            }
            if (TestWarnMods(ref warnMessage))
            {
                message += warnMessage;
            }

            if (!string.IsNullOrEmpty(message))
                UIMessageBox.Show(title, message, "确定".Translate(), 3);

            initialized = true;
            Plugin.Instance.Version = Plugin.VERSION + RequriedPlugins;
            Log.Debug($"Version: {Plugin.Instance.Version}");
        }

        public static void OnDestory()
        {
            ChatManager.OnDestory();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnGameBegin()
        {
            if (NebulaModAPI.IsMultiplayerActive && NebulaModAPI.MultiplayerSession.LocalPlayer.IsClient)
            {
                Log.Debug("OnLogin");
                IsClient = true;
                OnLogin?.Invoke();
                return;
            }
            IsClient = false;
        }

        static bool TestIncompatMods(ref string incompatMessage)
        {
            int count = 0;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("semarware.dysonsphereprogram.LongArm"))
            {
                incompatMessage += "LongArm\n";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("greyhak.dysonsphereprogram.droneclearing"))
            {
                incompatMessage += "DSP Drone Clearing\n";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.small.dsp.transferInfo"))
            {
                incompatMessage += "TransferInfo\n";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("greyhak.dysonsphereprogram.beltreversedirection"))
            {
                incompatMessage += "DSP Belt Reverse\n";
                count++;
            }            
            return count > 0;
        }

        static bool TestWarnMods(ref string warnMessage)
        {
            int count = 0;
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("cn.blacksnipe.dsp.Multfuntion_mod"))
            {
                warnMessage += "Multfuntion mod\n";
                count++;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("org.soardev.cheatenabler"))
            {
                warnMessage += "CheatEnabler\n";
                count++;
            }
            return count > 0;
        }

    }
}

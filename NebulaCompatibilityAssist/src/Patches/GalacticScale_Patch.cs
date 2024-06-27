using HarmonyLib;
using NebulaAPI;
using System;
using System.Reflection;

namespace NebulaCompatibilityAssist.Patches
{
    public class GalacticScale_Patch
    {
        public const string NAME = "Galactic Scale 2 Plug-In";
        public const string GUID = "dsp.galactic-scale.2";
        public const string VERSION = "2.14.1";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;
            Assembly assembly = pluginInfo.Instance.GetType().Assembly;

            try
            {
                Type classType = assembly.GetType("GalacticScale.PatchOnUnspecified_Debug");

                // 黑霧敵人掉落物
                harmony.Patch(AccessTools.Method(classType, "AddTrashFromGroundEnemy"),
                    new HarmonyMethod(typeof(GalacticScale_Patch).GetMethod(nameof(Block_In_Client_Prefix))));

                // 建築摧毀掉落物
                harmony.Patch(AccessTools.Method(classType, "AddTrashOnPlanet"),
                    new HarmonyMethod(typeof(GalacticScale_Patch).GetMethod(nameof(Block_In_Client_Prefix))));

                // 修復客機在太空載入時的錯誤
                harmony.PatchAll(typeof(GalacticScale_Patch));

                Log.Info($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Log.Warn($"{NAME} - Fail! Last target version: {VERSION}");
                NC_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Log.Warn(e);
            }
        }

        public static bool Block_In_Client_Prefix()
        {
            return !NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.IsServer; // return false in client
        }

        [HarmonyPrefix, HarmonyPriority(Priority.High), HarmonyBefore("dsp.galactic-scale.2")]
        [HarmonyPatch(typeof(GameLoader), nameof(GameLoader.FixedUpdate))]
        public static void FixedUpdate_Prefix(ref GameLoader __instance)
        {
            if (!NebulaModAPI.IsMultiplayerActive || NebulaModAPI.MultiplayerSession.IsServer) return;

            if (__instance.frame == 5 && GameMain.localStar != null)
            {
                Log.Info($"GalacticScale patch: FRAME 5 local star = {GameMain.localStar.displayName}");
                if (GameMain.mainPlayer != null && GameMain.localPlanet != null) // Add null check to local planet
                {
                    Log.Info($"Set player uPosition on " + GameMain.localPlanet.displayName);
                    GameMain.mainPlayer.uPosition = GameMain.localPlanet.uPosition;
                }
                GameMain.localPlanet?.Load();
                GameMain.localStar?.Load();
                __instance.frame++;
            }
        }
    }
}

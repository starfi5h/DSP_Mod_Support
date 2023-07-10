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

            BulletTime_Patch.OnAwake();
            GalacticScale_Patch.OnAwake(harmony);
            LSTM_Patch.OnAwake();
            NebulaMultiplayer_Patch.OnAwake();

            harmony.PatchAll(typeof(StringManager));
        }

        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}

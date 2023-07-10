using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using CommonAPI;
using CommonAPI.Systems;
using System;

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
        public static Plugin Instance;
        Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            Instance = this;
            harmony = new(GUID);

            LoadConfigStrings();
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

        public void LoadConfigStrings()
        {            
            try
            {
                var jsonString = Config.Bind("自定义", "字典", "", @"增加或覆盖翻译。範例格式:\n{""Yes"":""是"",""No"":""否""}").Value;
                var dict = LightweightJsonParser.ParseJSON(jsonString);
                if (dict != null)
                {
                    Log.LogDebug($"Import {dict.Count} strings from config file");
                    foreach (var item in dict)
                    {
                        StringManager.RegisterString(item.Key, (string)item.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError("Import strings from config file fail!");
                Log.LogError(e);
            }
        }
    }
}

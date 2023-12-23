using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

[assembly: AssemblyTitle(SF_ChinesePatch.Plugin.NAME)]
[assembly: AssemblyVersion(SF_ChinesePatch.Plugin.VERSION)]

namespace SF_ChinesePatch
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.SF_ChinesePatch";
        public const string NAME = "SF_ChinesePatch";
        public const string VERSION = "1.3.0";

        public static ManualLogSource Log;
        public static Plugin Instance;
        Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            Instance = this;
            harmony = new(GUID);

            LoadConfigStrings();
            NebulaMultiplayer_Patch.OnAwake();
            GalacticScale_Patch.OnAwake(harmony);
            BulletTime_Patch.OnAwake();
            DSPStarMapMemo_Patch.OnAwake();
            LSTM_Patch.OnAwake(harmony);
            PlanetFinder_Patch.OnAwake(harmony);

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
                var importString = Config.Bind("自定义", "字典", "", "增加或覆盖翻译。格式範例:\n" + @"""Yes"":""是"",""No"":""否""").Value;
                if (!string.IsNullOrEmpty(importString)) 
                {
                    var dict = SimpleParser.Parse(importString);
                    if (dict != null)
                    {
                        Log.LogDebug($"Import {dict.Count} strings from config file");
                        foreach (var item in dict)
                        {
                            StringManager.RegisterString(item.Key, (string)item.Value);
                        }
                    }
                    else
                    {
                        Log.LogError("Import strings from config file fail Format is not correct");
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogError("Import strings from config file fail!");
                Log.LogError(e);
            }
        }

        public static IEnumerable<CodeInstruction> TranslateStrings(IEnumerable<CodeInstruction> instructions)
        {
            // Add .Translate() behind every string
            var translateMethod = AccessTools.Method(typeof(Localization), nameof(Localization.Translate));
            var codeMatcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldstr))
                .Repeat(matcher => matcher
                        .Advance(1)
                        .Insert(new CodeInstruction(OpCodes.Call, translateMethod))
                );
            
            return codeMatcher.InstructionEnumeration();
        }
    }
}

using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SF_ChinesePatch
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.SF_ChinesePatch";
        public const string NAME = "SF_ChinesePatch";
        public const string VERSION = "1.1.0";

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
            DSPStarMapMemo_Patch.OnAwake();
            GalacticScale_Patch.OnAwake(harmony);
            LSTM_Patch.OnAwake(harmony);
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
            var codeMatcher = new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(OpCodes.Ldstr))
                .Repeat(matcher => matcher
                        .Advance(1)
                        .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StringTranslate), nameof(StringTranslate.Translate), new System.Type[] { typeof(string) })))
                );

            return codeMatcher.InstructionEnumeration();
        }
    }
}

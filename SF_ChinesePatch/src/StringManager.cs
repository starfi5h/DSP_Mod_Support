using HarmonyLib;
using System.Collections.Generic;

namespace SF_ChinesePatch
{

    // Reference source: https://github.com/xiaoye97/DSP_LDBTool

    public class StringManager
    {
        private static bool isInject = false;
        private static int lastStringId = 1000;
        private static List<Proto> protos = new ();
        private static HashSet<string> nameIndices = new ();

        public static void RegisterString(string key, string cnTrans, string enTrans = "")
        {
            if (isInject) return;
            if (nameIndices.Contains(key))
            {
                Plugin.Log.LogInfo($"{key}:({cnTrans},{enTrans}) has already been registered!");
                return;
            }

            if (string.IsNullOrEmpty(enTrans)) enTrans = key;
            StringProto proto = new()
            {
                Name = key,
                ENUS = enTrans,
                ZHCN = string.IsNullOrEmpty(cnTrans) ? enTrans : cnTrans,
                FRFR = enTrans,
                ID = -1
            };
            nameIndices.Add(key);
            protos.Add(proto);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(DSPGame), nameof(DSPGame.Awake))] // Before GS2 patch
        public static void InjectStrings()
        {
            if (!isInject)
            {
                isInject = true;
                bool hasLDBTool = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("me.xiaoye97.plugin.Dyson.LDBTool");

                if (hasLDBTool)
                {
                    lastStringId = (int)AccessTools.Field(AccessTools.TypeByName("xiaoye97.LDBTool"), "lastStringId").GetValue(null);
                    Plugin.Log.LogInfo("Starting with LDBTool lastStringId " + lastStringId);
                }

                // GalacticScale.GS2.Init() too early for Localization.language to load, so we assume the language is zhCN first
                Localization.language = Language.zhCN;
                AddProtosToSet(LDB.strings, protos);
                protos = null;
                nameIndices = null;

                if (hasLDBTool)
                {
                    AccessTools.Field(AccessTools.TypeByName("xiaoye97.LDBTool"), "lastStringId").SetValue(null, lastStringId);
                    Plugin.Log.LogInfo("End with LDBTool lastStringId " + lastStringId);
                }
            }
        }

        private static void AddProtosToSet<T>(ProtoSet<T> protoSet, List<Proto> protos) where T : Proto
        {
            var array = protoSet.dataArray;
            protoSet.Init(array.Length + protos.Count);
            for (int i = 0; i < array.Length; i++)
            {
                protoSet.dataArray[i] = array[i];
            }

            for (int i = 0; i < protos.Count; i++)
            {
                protos[i].ID = FindAvailableStringID();
                protoSet.dataArray[array.Length + i] = protos[i] as T;
                //Plugin.Log.LogDebug($"Add {protos[i].ID} {protos[i].Name.Translate()} to {protoSet.GetType().Name}.");
            }
            Plugin.Log.LogDebug($"Add {protos.Count} to protoSet.");

            var dataIndices = new Dictionary<int, int>();
            for (int i = 0; i < protoSet.dataArray.Length; i++)
            {
                protoSet.dataArray[i].sid = protoSet.dataArray[i].SID;
                dataIndices[protoSet.dataArray[i].ID] = i;
            }

            protoSet.dataIndices = dataIndices;
            if (protoSet is StringProtoSet stringProtoSet)
            {
                for (int i = array.Length; i < protoSet.dataArray.Length; i++)
                {
                    stringProtoSet.nameIndices[protoSet.dataArray[i].Name] = i;
                }
            }
        }

        private static int FindAvailableStringID()
        {
            int id = lastStringId + 1;
            while (true)
            {
                if (!LDB.strings.dataIndices.ContainsKey(id))
                {
                    break;
                }
                id++;
            }
            lastStringId = id;
            return id;
        }
    }
}

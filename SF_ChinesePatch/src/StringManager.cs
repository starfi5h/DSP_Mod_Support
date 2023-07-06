using HarmonyLib;
using System.Collections.Generic;

namespace SF_ChinesePatch
{
    public class StringManager
    {
        private static int lastStringId = 1000;
        private static List<Proto> protos = new();
        private static Dictionary<string, int> nameIndices = new ();

        public static void RegisterString(string key, string cnTrans, string enTrans = "")
        {
            if (nameIndices.ContainsKey(key))
                return;

            if (enTrans.Equals("")) enTrans = key;
            StringProto proto = new()
            {
                Name = key,
                ENUS = enTrans,
                ZHCN = cnTrans.Equals("") ? enTrans : cnTrans,
                FRFR = enTrans,
                ID = FindAvailableStringID()
            };
            nameIndices.Add(key, proto.ID);
            protos.Add(proto);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIGame), nameof(UIGame._OnCreate))]
        public static void InjectStrings()
        {
            // UIGame._OnCreate is triggered earlier than LDBTool injection ()
            // In order to make translate() works for mod UI, it need to inject earlier

            AddProtosToSet(LDB.strings, protos);
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("me.xiaoye97.plugin.Dyson.LDBTool"))
            {
                AccessTools.Field(AccessTools.TypeByName("xiaoye97.LDBTool"), "lastStringId").SetValue(null, lastStringId);
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
                protoSet.dataArray[array.Length + i] = protos[i] as T;
                Plugin.Log.LogDebug($"Add {protos[i].ID} {protos[i].Name.Translate()} to {protoSet.GetType().Name}.");
            }

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

        private static bool HasStringIdRegisted(int id)
        {
            if (LDB.strings.dataIndices.ContainsKey(id)) return true;
            return false;
        }

        private static int FindAvailableStringID()
        {
            int id = lastStringId + 1;
            while (true)
            {
                if (!HasStringIdRegisted(id))
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

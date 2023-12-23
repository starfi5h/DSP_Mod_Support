using HarmonyLib;
using System;
using System.Collections.Generic;

namespace SF_ChinesePatch
{

    // Reference source: https://github.com/soarqin/DSP_Mods/blob/master/UXAssist/Common/I18N.cs

    public class StringManager
    {
        private static bool isInject = false;
        private static readonly Dictionary<string, int> nameIndices = new (); // key, index to string tuples
        private static readonly List<Tuple<int, string, string>> stringTuples = new(); // id, en, cn

        public static void RegisterString(string key, string cnTrans, string enTrans = "")
        {
            if (isInject) return;
            if (nameIndices.ContainsKey(key))
            {
                Plugin.Log.LogInfo($"{key}:({cnTrans},{enTrans}) has already been registered!");
                return;
            }

            //Plugin.Log.LogDebug($"Register [{stringTuples.Count}] {key}:({cnTrans},{enTrans})");
            nameIndices.Add(key, stringTuples.Count);
            stringTuples.Add(Tuple.Create(-1, string.IsNullOrEmpty(enTrans) ? key : enTrans, cnTrans));
        }

        private static void AddNamesIndexer()
        {
            var indexer = Localization.namesIndexer;
            var indexCount = indexer.Count;
            foreach (var keypair in nameIndices)
            {
                if (indexer.TryGetValue(keypair.Key, out int index))
                {
                    var (_, en, cn) = stringTuples[keypair.Value];
                    stringTuples[keypair.Value] = Tuple.Create(index, en, cn);
                    Plugin.Log.LogDebug($"Overwrite {keypair.Value} => {cn} {en}");
                }
                else
                {
                    var (_, en, cn) = stringTuples[keypair.Value];
                    stringTuples[keypair.Value] = Tuple.Create(indexCount, en, cn);
                    indexer[keypair.Key] = indexCount;
                    indexCount++;
                }
            }
        }

        private static void ApplyLanguage(int index)
        {
            var strs = Localization.strings?[index];
            if (strs == null)
            {
                Plugin.Log.LogWarning("Can't find Localization.strings " + index);
                return;
            }
            var indexerLength = Localization.namesIndexer.Count;
            if (strs.Length < indexerLength)
            {
                var newStrs = new string[indexerLength];
                Array.Copy(strs, newStrs, strs.Length);
                strs = newStrs;
                Localization.strings[index] = newStrs;
            }
            var floats = Localization.floats[index];
            if (floats != null)
            {
                if (floats.Length < indexerLength)
                {
                    var newFloats = new float[indexerLength];
                    Array.Copy(floats, newFloats, floats.Length);
                    Localization.floats[index] = newFloats;
                }
            }

            var lcId = Localization.Languages[index].lcId;
            if (lcId == Localization.LCID_ZHCN) //cn 2052
            {
                foreach (var tuple in stringTuples)
                    strs[tuple.Item1] = tuple.Item3;
                Plugin.Log.LogInfo($"Add {stringTuples.Count} strings to ZHCN");
            }
            else if (index == Localization.LCID_ENUS) //en 1033
            {
                foreach (var tuple in stringTuples)
                    strs[tuple.Item1] = tuple.Item2;
                Plugin.Log.LogInfo($"Add {stringTuples.Count} strings to ENUS");
            }
        }

        [HarmonyPostfix, HarmonyAfter("dsp.common-api.CommonAPI")]
        [HarmonyPatch(typeof(Localization), nameof(Localization.LoadSettings))]
        private static void Localization_LoadSettings_Postfix()
        {
            if (isInject) return;
            if (stringTuples.Count > 0)
            {
                isInject = true;
                AddNamesIndexer();
            }
        }

        [HarmonyPostfix, HarmonyAfter("dsp.common-api.CommonAPI")]
        [HarmonyPatch(typeof(Localization), nameof(Localization.LoadLanguage))]
        private static void Localization_LoadLanguage_Postfix(int index)
        {
            if (!isInject) return;
            ApplyLanguage(index);
        }
    }
}

using HarmonyLib;
using System;
using xiaoye97;

namespace ModFixerOne.Mods
{
    public static class LDBTool_Patch
    {
        public const string NAME = "LDBTool";
        public const string GUID = "me.xiaoye97.plugin.Dyson.LDBTool";
        public const string VERSION = "3.0.1";

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                harmony.PatchAll(typeof(Warper));
                Plugin.Log.LogInfo($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} - Fail! Last target version: {VERSION}");
                Fixer_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Plugin.Log.LogDebug(e);
            }
        }

#pragma warning disable CS0618
        private class Warper
        {

            [HarmonyPrefix]
            [HarmonyPatch(typeof(LDBTool), nameof(LDBTool.PreAddProto), new Type[] { typeof(ProtoType), typeof(Proto) })]
            static bool PreAddProto_Guard(ProtoType protoType)
            {
                // Skip string translation register for this obsolete function
                return protoType != ProtoType.String;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(ProtoIndex), "GetAllProtoTypes")]
            static void GetAllProtoTypes(ref Type[] __result)
            {
                if (__result[__result.Length - 1].FullName == "StringProto")
                {
                    Plugin.Log.LogDebug("Remove StringProto from LDBTool ProtoTypes array");
                    var newArray = new Type[__result.Length - 1];
                    Array.Copy(__result, newArray, newArray.Length);
                    __result = newArray;
                }
            }
        }
    }
}

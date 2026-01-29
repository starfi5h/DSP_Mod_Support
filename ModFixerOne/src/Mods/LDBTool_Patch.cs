using HarmonyLib;
using System;
using System.Collections.Generic;
using xiaoye97;

namespace ModFixerOne.Mods
{
    public static class LDBTool_Patch
    {
        public const string NAME = "LDBTool";
        public const string GUID = "me.xiaoye97.plugin.Dyson.LDBTool";
        public const string VERSION = "3.0.1";
        private static bool IsGameLoaded = false;

        public static void Init(Harmony harmony)
        {
            if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(GUID, out var pluginInfo))
                return;

            try
            {
                harmony.PatchAll(typeof(Warper));
                if (!(GameConfig.gameVersion < new Version(0, 10, 34)))
                {
                    harmony.PatchAll(typeof(LDBTool_Patch));
                }
                Plugin.Log.LogInfo($"{NAME} - OK");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"{NAME} - Fail! Last target version: {VERSION}");
                Fixer_Patch.ErrorMessage += $"\n{NAME} (last target version: {VERSION})";
                Plugin.Log.LogDebug(e);
            }
        }

        [HarmonyPostfix, HarmonyAfter(GUID)]
        [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
        private static void PreloadAndInitAll()
        {
            if (IsGameLoaded) return;
            IsGameLoaded = true;

            // 只初始化一些必要的項目
            Plugin.Log.LogInfo("PreloadAndInitAll");
            try
            {
                ItemProto.InitFuelNeeds();
                ItemProto.InitConstructableItems();
                ItemProto.InitItemIds();
                ItemProto.InitItemIndices();
                InitRecipeItems();
                SignalProtoSet.InitSignalKeyIdPairs();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Error in PreloadAndInitAll\n" + ex.ToString());
                Fixer_Patch.ErrorMessage += "Error in PreloadAndInitAll\n" + ex.ToString();
            }
        }

        private static void InitRecipeItems()
        {
            // 為了避免Recipe同樣id衝突的情況發生, 在這裡使用修改後的函式
            RecipeProto.recipeExecuteData = new Dictionary<int, RecipeExecuteData>();
            RecipeProto[] dataArray = LDB.recipes.dataArray;
            for (int i = 0; i < dataArray.Length; i++)
            {
                RecipeExecuteData recipeExecuteData = new RecipeExecuteData(dataArray[i].Items, dataArray[i].ItemCounts, dataArray[i].Results, dataArray[i].ResultCounts, dataArray[i].TimeSpend * 10000, dataArray[i].TimeSpend * 100000, dataArray[i].productive);
                if (!RecipeProto.recipeExecuteData.ContainsKey(dataArray[i].ID))
                {
                    RecipeProto.recipeExecuteData.Add(dataArray[i].ID, recipeExecuteData);
                }
                else
                {
                    // 有衝突時, 以先註冊的配方優先。印出後來的配方id和名稱
                    Plugin.Log.LogWarning($"Duplicate RecipeId:{dataArray[i].ID} Name:{dataArray[i].Name}");
                }
            }
        }

        [HarmonyPostfix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.Import))]
        private static void ValidateAndFixRecipeData(ref AssemblerComponent __instance)
        {
            if (__instance.recipeId > 0)
            {
                if (__instance.recipeExecuteData == null)
                {
                    // 原版遊戲假設了recipeId>0時recipeExecuteData必有值
                    // 因此對於已經移除的配方, 將機器設置為無配方狀態
                    Plugin.Log.LogWarning($"Importing assembler {__instance.recipeType}: Can't find recipeId {__instance.recipeId}");
                    __instance.recipeId = 0;
                    __instance.recipeType = ERecipeType.None;
                    __instance.recipeExecuteData = null;
                    __instance.served = null;
                    __instance.incServed = null;
                    __instance.needs = null;
                    __instance.produced = null;
                }
                else
                {
                    // 確保物品數量的數組和recipeExecuteData中的數組長度一致, 避免在UpdateNeeds()中報錯
                    if (__instance.served.Length != __instance.recipeExecuteData.requireCounts.Length ||
                        __instance.incServed.Length != __instance.recipeExecuteData.requireCounts.Length)
                    {
                        Plugin.Log.LogWarning($"Importing assembler {__instance.recipeType}: requireCounts mismatch");
                        __instance.served = new int[__instance.recipeExecuteData.requireCounts.Length];
                        __instance.incServed = new int[__instance.recipeExecuteData.requireCounts.Length];
                    }
                    if (__instance.produced.Length != __instance.recipeExecuteData.productCounts.Length)
                    {
                        Plugin.Log.LogWarning($"Importing assembler {__instance.recipeType}: productCounts mismatch");
                        __instance.produced = new int[__instance.recipeExecuteData.productCounts.Length];
                    }
                }
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

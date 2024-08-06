using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using XUnity.AutoTranslator.Plugin.Core;

[assembly: AssemblyTitle(AutoTranslatorHelper.Plugin.NAME)]
[assembly: AssemblyVersion(AutoTranslatorHelper.Plugin.VERSION)]

namespace AutoTranslatorHelper
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("gravydevsupreme.xunity.autotranslator")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "starfi5h.plugin.AutoTranslatorHelper";
        public const string NAME = "AutoTranslatorHelper";
        public const string VERSION = "5.3.100";

        static bool Isinitial = false;
        static ManualLogSource Log;
        Harmony harmony;

        public void Awake()
        {
            Log = Logger;
            harmony = new Harmony(GUID);
            try
            {
                harmony.PatchAll(typeof(Plugin));
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
#if DEBUG
            OnBegin();
#endif
        }

#if DEBUG
        public void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
#endif

        static void XUAIgnoreGameObjectByName(string path)
        {
            var go = GameObject.Find(path);
            if (go == null)
            {
                Log.LogWarning("Can't find GameObject " + path);
                return;
            }
            if (!go.name.EndsWith("XUAIGNORE"))
            {
                go.name += " XUAIGNORE";
            }
        }

        static void XUAIgnoreGameObjectByName(GameObject go)
        {
            if (go == null)
            {
                Log.LogWarning("Can't find GameObject");
                return;
            }
            if (!go.name.EndsWith("XUAIGNORE"))
            {
                go.name += " XUAIGNORE";
            }
        }

        static void XUAIgnoreTextComponent(Text text)
        {
            // For componenet that are already exist, ignore them by setting the bool flag
            if (text == null) return;
            AutoTranslator.Default.IgnoreTextComponent(text);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameMain), nameof(GameMain.Begin))]
        public static void OnBegin()
        {
            if (Isinitial) return;
            Isinitial = true;

            XUAIgnoreGameObjectByName("UI Root/Overlay Canvas/Top Windows/Load Game Window/list/scroll-view/viewport/content/save-entry/filename");

            // UIZS_ToolPanel, UITrashPanel
            QueueTranslate("垃圾数量格式");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.zScreen.toolPanel.countText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.trashPanel.countText);

            // UIVeinDetailNode
            QueueTranslate("空格个");
            QueueTranslate("储量");
            QueueTranslate("产量");
            foreach (var veinProto in LDB.veins.dataArray)
            {
                var copy = veinProto; // for closure lambda
                QueueTranslate(veinProto.Name, (value) => copy.name = value);
            }

            // UIPlanetGlobe
            QueueTranslate("极昼");
            QueueTranslate("极夜");
            QueueTranslate("热带");
            QueueTranslate("寒带");
            QueueTranslate("夏季半球");
            QueueTranslate("冬季半球");
            QueueTranslate("永昼");
            QueueTranslate("永夜");
            QueueTranslate("格式秒");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.planetGlobe.geoInfoText);

            // UIPlanetDetail, UIStarDetail
            QueueTranslate("空格秒");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.planetDetail.orbitPeriodValueText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.planetDetail.rotationPeriodValueText);
            QueueTranslate("百万亿年");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.starDetail.ageValueText);

            // UIStarmap
            QueueTranslate("空格米");
            QueueTranslate("距离米");
            QueueTranslate("距离日距");
            QueueTranslate("距离光年");
            QueueTranslate("最快秒");
            QueueTranslate("最快分秒");
            QueueTranslate("最快分钟");
            QueueTranslate("最快小时");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.starmap.cursorViewText);

            // UISpaceGuideEntry
            QueueTranslate("光年");
            XUAIgnoreGameObjectByName("UI Root/Overlay Canvas/In Game/Space Guide/Guide Entry Prefab/dist-text");

            // UIMechaWindow
            QueueTranslate("发每秒");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.mechaWindow.ammoRateValue);

            // UITechNode, UITechTip
            QueueTranslate("等级");
            QueueTranslate("努力制作");
            QueueTranslate("空格哈希");
            QueueTranslate("大号哈希块");
            QueueTranslate("相当于一个研究站");
            QueueTranslate("秒工作量");
            QueueTranslate("算力");
            QueueTranslate("剩余哈希数");
            QueueTranslate("双空格分");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.researchQueue.techTip.progressText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.researchQueue.techTip.progressSpeedText);

            // UIPerformancePanel
            QueueTranslate("空格单位");
            QueueTranslate("空格顶点");
            QueueTranslate("理论逻辑帧");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuGraph.mainText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuValueText1);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.cpuValueText2);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.gpuGraph.mainText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.gpuValueText1);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.gpuValueText2);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.dataValueText1);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.statWindow.performancePanelUI.dataValueText2);

            // UIBeltWindow
            QueueTranslate("节点号");
            QueueTranslate("带长度");
            QueueTranslate("带速度");
            QueueTranslate("货物每秒");
            QueueTranslate("线路号");
            QueueTranslate("线路节点数");
            QueueTranslate("线路货物数");
            QueueTranslate("线路总容量");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.beltWindow.idText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.beltWindow.lengthText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.beltWindow.speedText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.beltWindow.pathIdText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.beltWindow.pathNodeCntText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.beltWindow.pathCargoCntText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.beltWindow.pathBufferCntText);

            // UIPowerGenerator
            QueueTranslate("电网负荷");
            QueueTranslate("无法负荷");
            QueueTranslate("风力");
            QueueTranslate("光强");
            QueueTranslate("地热强度");
            QueueTranslate("照射");
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.generatorWindow.elecText);
            XUAIgnoreTextComponent(UIRoot.instance.uiGame.generatorWindow.fuelText0);

            // UIItemTip
            QueueTranslate("增产点数共计");
            QueueTranslate("空格点");
            XUAIgnoreGameObjectByName(Configs.builtin.uiItemTipPrefab.incPointText.gameObject);
        }

        static void QueueTranslate(string keyString, Action<string> onSuccess = null)
        {
            string fromText = keyString.Translate();
            AutoTranslator.Default.TranslateAsync(fromText, result =>
            {
                if (result.Succeeded)
                {                    
                    try
                    {
                        Log.LogDebug($"Get '{fromText}' => '{result.TranslatedText}'");

                        // Test if argument format is correct
                        List<string> formatArguments = new();
                        MatchCollection matches = Regex.Matches(fromText, @"\{.*?\}");
                        foreach (Match match in matches)
                        {
                            formatArguments.Add(match.Value);
                        }
                        matches = Regex.Matches(result.TranslatedText, @"\{.*?\}");
                        if (formatArguments.Count != matches.Count)
                        {
                            Log.LogWarning($"Argument count mismatch: {formatArguments.Count} => {matches.Count}");
                            return;
                        }
                        int i = 0;
                        foreach (Match match in matches)
                        {
                            if (match.Value.Replace(" ", string.Empty) != formatArguments[i].Replace(" ", string.Empty))
                            {
                                Log.LogWarning($"Argument[{i}] content mismatch: {formatArguments[i]} => {match.Value}");
                                return;
                            }
                            i++;
                        }

                        EditLocalization(keyString, result.TranslatedText);
                    }
                    catch (Exception e)
                    {
                        Log.LogWarning(e);
                    }
                    if (onSuccess != null) onSuccess(result.TranslatedText);
                }
                else
                {
                    // Usually due to the string is already translated
                    Log.LogDebug(fromText + ": " + result.ErrorMessage);
                }
            });
        }

        static void EditLocalization(string keyText, string translatedText)
        {
            // Modify from game's Localization.Translate(this string s)
            if (Localization.namesIndexer == null || Localization.currentStrings == null || !Localization.namesIndexer.ContainsKey(keyText))
            {
                Log.LogWarning($"Translation '{keyText}' => '{translatedText}' doesn't register!");
                return;
            }
            Localization.currentStrings[Localization.namesIndexer[keyText]] = translatedText;
            Log.LogDebug($"Key '{keyText}' => '{keyText.Translate()}'");
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(UIVeinDetailNode), "_OnOpen")]
        static void UIVeinDetailNode_OnOpen(UIVeinDetailNode __instance)
        {
            XUAIgnoreTextComponent(__instance.infoText);
        }
    }
}

using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace StatsUITweaks
{
    public class StatsWindowPatch
    {
        public static bool PreserveFilter = true; //保存過濾條件
        public static int SignificantDigits = 3; //有效位數(每秒)
        public static int TimeSliderSlice = 20; //時間滑桿刻度
        public static int ListWidthOffeset = 70; //星球列表寬度
        public static bool DisplayPerSecond = false; //以秒顯示
        public static int RateFontSize = 26; //速率字型大小(不改=0 原版=18)
        public static bool ExtendGraph = false; //展開直方圖

        static bool initialized;
        static bool enable;
        static GameObject perSecGo;
        static Slider timerSlider;
        static InputField filterInput;
        static UIButton locateBtn;
        static GameObject filterGo;
        static GameObject extendGraphGo;

        static string nameFilter;
        static int rawMaterialFilter;
        static int endProductFilter;

        public static void Init(UIStatisticsWindow __instance)
        {
            if (initialized) return;
            initialized = true;

            try
            {
                static void Reposition(Transform astroBoxtTansform, Transform timeBoxTransform)
                {
                    ((RectTransform)astroBoxtTansform).sizeDelta = new Vector2(200f + ListWidthOffeset, 30f);
                    timeBoxTransform.localPosition = new Vector3(310 + 20 - ListWidthOffeset, timeBoxTransform.localPosition.y);
                }

                if (ListWidthOffeset > 0)
                {
                    Reposition(__instance.productAstroBox.transform, __instance.productTimeBox.transform);
                    if (ListWidthOffeset > 40)
                    {
                        ((RectTransform)__instance.productSortBox.transform).sizeDelta = new Vector2(200f - (ListWidthOffeset - 40), 30f);
                        __instance.productSortBox.transform.localPosition = new Vector3(135f - ( ListWidthOffeset - 40 ), __instance.productSortBox.transform.localPosition.y);
                    }
                    Reposition(__instance.powerAstroBox.transform, __instance.powerTimeBox.transform);
                    Reposition(__instance.researchAstroBox.transform, __instance.researchTimeBox.transform);
                    Reposition(__instance.dysonAstroBox.transform, __instance.dysonTimeBox.transform);
                    Reposition(__instance.killAstroBox.transform, __instance.killTimeBox.transform);
                    if (ListWidthOffeset > 40)
                    {
                        ((RectTransform)__instance.killSortBox.transform).sizeDelta = new Vector2(200f - (ListWidthOffeset - 40), 30f);
                        __instance.killSortBox.transform.localPosition = new Vector3(135f - (ListWidthOffeset - 40), __instance.killSortBox.transform.localPosition.y);
                    }
                }
                Utils.EnableRichText(__instance.productAstroBox);
                Utils.EnableRichText(__instance.powerAstroBox);
                Utils.EnableRichText(__instance.researchAstroBox);
                Utils.EnableRichText(__instance.dysonAstroBox);
                Utils.EnableRichText(__instance.killAstroBox);

                GameObject go;
                Text text;
                Slider slider0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.slider0;
                GameObject inputObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Windows/Control Panel Window/filter-group/sub-group/search-filter");
                UIButton uIButton0 = UIRoot.instance.uiGame.researchQueue.pauseButton;
                GameObject checkBoxWithTextTemple = UIRoot.instance.optionWindow.fullscreenComp.transform.parent.gameObject;

                // 單位:每秒按鈕
                perSecGo = GameObject.Instantiate(checkBoxWithTextTemple, __instance.productNameInputField.transform);
                perSecGo.name = "perSecGo";
                perSecGo.transform.localPosition = new Vector3(0, 60, 0);
                GameObject.Destroy(perSecGo.GetComponent<Localizer>());
                text = perSecGo.GetComponent<Text>();
                text.fontSize = 14;
                text.text = "Display /s";
                var toggle_perSec = perSecGo.GetComponentInChildren<UIToggle>().toggle;
                toggle_perSec.onValueChanged.AddListener(new UnityAction<bool>(OnDisplayPerSecondToggleChange));
                go = toggle_perSec.gameObject;
                go.transform.localPosition = new Vector3(70, 0);
                go.transform.localScale = new Vector3(0.75f, 0.75f);

                // 展開直方圖
                extendGraphGo = GameObject.Instantiate(checkBoxWithTextTemple, __instance.productNameInputField.transform);
                extendGraphGo.name = "extendGraphGo";
                extendGraphGo.transform.localPosition = new Vector3(120, 60, 0);
                GameObject.Destroy(extendGraphGo.GetComponent<Localizer>());
                text = extendGraphGo.GetComponent<Text>();
                text.fontSize = 14;
                text.text = "Extend Graph";
                var toggle_extendGraph = extendGraphGo.GetComponentInChildren<UIToggle>().toggle;
                toggle_extendGraph.onValueChanged.AddListener(new UnityAction<bool>(OnExtendGraphToggleChange));
                go = toggle_extendGraph.gameObject;
                go.transform.localPosition = new Vector3(70, 0);
                go.transform.localScale = new Vector3(0.75f, 0.75f);

                // 時間滑桿
                go = GameObject.Instantiate(slider0.gameObject, __instance.productTimeBox.transform);
                go.name = "CustomStats_Ratio";
                go.transform.localPosition = new Vector3(-153f, 8f, 0);
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(155.5f, 13);
                timerSlider = go.GetComponent<Slider>();
                timerSlider.minValue = 0;
                timerSlider.maxValue = TimeSliderSlice;
                timerSlider.wholeNumbers = true;
                timerSlider.value = timerSlider.maxValue;
                timerSlider.onValueChanged.AddListener(new UnityAction<float>(Entry_Patch.OnSliderChange));
                //tmp.transform.GetChild(1).GetComponent<Image>().color = new Color(0.3f, 1.0f, 1.0f, 0.47f); //改成亮藍色
                go.SetActive(true);

                // 星系列表搜尋欄
                filterGo = GameObject.Instantiate(inputObj, __instance.productAstroBox.transform);
                filterGo.name = "CustomStats_Fliter";
                filterGo.transform.localPosition = new Vector3(0f, 20f, 0);
                filterInput = filterGo.GetComponentInChildren<InputField>();
                filterInput.text = "";
                filterInput.onValueChanged.AddListener(new UnityAction<string>(OnInputValueChanged));
                filterGo.GetComponent<RectTransform>().sizeDelta = new Vector2(((RectTransform)__instance.productAstroBox.transform).sizeDelta.x, 28f);
                filterGo.SetActive(true);

                // 星系導引按鈕
                go = GameObject.Instantiate(uIButton0.gameObject, __instance.productAstroBox.transform);
                go.name = "CustomStats_Navi";
                go.transform.localScale = new Vector3(0.33f, 0.33f, 0.33f);
                go.transform.localPosition = new Vector3(2f, -6f, 0f);
                Image img = go.transform.Find("icon")?.GetComponent<Image>();
                if (img != null)
                {
                    UIStarmap starmap = UIRoot.instance.uiGame.starmap;
                    img.sprite = starmap.cursorFunctionButton3.transform.Find("icon")?.GetComponent<Image>()?.sprite;
                }
                locateBtn = go.GetComponent<UIButton>();
                locateBtn.tips.tipTitle = "Locate";
                locateBtn.tips.tipText = "Left click: Navigate to planet\nRight click: Show planet in starmap";
                locateBtn.onClick += OnLocateButtonClick;
                locateBtn.onRightClick += OnLocateButtonRightClick;
                go.SetActive(true);

                enable = true;
                if (__instance.astroBox != __instance.productAstroBox)
                    OnTabButtonClick(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("UI component initial fail!");
                Plugin.Log.LogError(e);
            }
        }

        public static void OnDestory()
        {
            GameObject.Destroy(perSecGo);
            GameObject.Destroy(extendGraphGo);
            GameObject.Destroy(timerSlider?.gameObject);
            GameObject.Destroy(filterInput?.gameObject);
            GameObject.Destroy(locateBtn?.gameObject);
            GameObject.Destroy(filterGo);
            initialized = false;
        }

        static void OnDisplayPerSecondToggleChange(bool value)
        {
            DisplayPerSecond = value;
        }

        static void OnExtendGraphToggleChange(bool value)
        {
            ExtendGraph = value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnOpen))]
        static void OnOpen_Postfix(UIStatisticsWindow __instance)
        {
            Init(__instance);

            if (PreserveFilter)
            {
                __instance.productNameInputField.text = nameFilter;
                __instance.rawMaterialFilter = rawMaterialFilter;
                __instance.endProductFilter = endProductFilter;
                __instance.RefreshFilterTag();
                __instance.ComputeDetailNextTick();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnClose))]
        static void OnClose_Postfix(UIStatisticsWindow __instance)
        {
            if (PreserveFilter)
            {
                nameFilter = __instance.nameFilter;
                rawMaterialFilter = __instance.rawMaterialFilter;
                endProductFilter = __instance.endProductFilter;
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.OnProductAstroBoxItemIndexChanged))]
        static bool PreventClearingFilter()
        {
            return !PreserveFilter; //阻止視窗在切換時間範圍時將過濾條件重設nameFilter, rawMaterialFilter, endProductFilter
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.OnTabButtonClick))]
        static void OnTabButtonClick(UIStatisticsWindow __instance) // 切換頁面時, 重新設置UI元件的父元件
        {
            if (!enable) return;
            if (__instance.timeBox != null)
                timerSlider.gameObject.transform.SetParent(__instance.timeBox.transform);
            if (__instance.astroBox != null)
            {
                filterGo.transform.SetParent(__instance.astroBox.transform);
                locateBtn.gameObject.transform.SetParent(__instance.astroBox.transform);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnUpdate))]
        static void OnUpdate(UIStatisticsWindow __instance)
        {
            if (__instance.isStatisticsTab)
            {
                Utils.DetermineAstroBoxIndex(__instance.astroBox);
            }
        }

        #region 導航按鈕

        static void OnLocateButtonClick(int obj)
        {
            int astroId = UIRoot.instance.uiGame.statWindow.astroFilter;
            if (astroId <= 0) return;

            GameMain.mainPlayer.navigation.indicatorAstroId = astroId;
        }

        static void OnLocateButtonRightClick(int obj)
        {
            int astroId = UIRoot.instance.uiGame.statWindow.astroFilter;
            if (astroId <= 0) return;

            UIRoot.instance.uiGame.OpenStarmap();
            int starIdx = astroId / 100 - 1;
            int planetIdx = astroId % 100 - 1;
            UIStarmap map = UIRoot.instance.uiGame.starmap;
            if (starIdx < 0 || starIdx >= map.starUIs.Length) return;

            if (planetIdx >= 0)
            {
                PlanetData planet = GameMain.galaxy.PlanetById(astroId);
                if (planet != null)
                {
                    map.focusPlanet = null;
                    map.focusStar = map.starUIs[starIdx];
                    map.OnCursorFunction2Click(0);
                    // Extend max dist
                    map.screenCameraController.SetViewTarget(planet, null, null, null, VectorLF3.zero, 
                        planet.realRadius * 0.00025 * 6.0, planet.realRadius * 0.00025 * 16.0, true, false);
                }
            }
            else
            {
                map.focusPlanet = null;
                map.focusStar = map.starUIs[starIdx];
                map.OnCursorFunction2Click(0);
            }
        }

        #endregion

        #region 列表修改

        static string searchStr = "";

        static void OnInputValueChanged(string value)
        {
            searchStr = value;
            UIRoot.instance.uiGame.statWindow.RefreshAstroBox();
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ValueToAstroBox))]
        [HarmonyAfter("Bottleneck")]
        static void FilterList(UIStatisticsWindow __instance, ref int __state)
        {
            if (!__instance.isStatisticsTab) return;

            // 在遊戲中 UIStatisticsWindow.RefreshAstroBox() 設定前面固定的選項
            // [0]:-1, "统计全星系"
            // [1]:0, "统计当前星球" 只有在localPlanet!=null才會有
            // [1~2]:-2 "统计玩家" 在isKillTab
            // [2]:?00, "localSystemLabel" Bottleneck新增的選項
            // 其餘皆為 星系 + 星球1 + 星球2 ..., 以星系的ID排序
            int startIndex = 1;
            if (!__instance.isDysonTab && __instance.gameData.localPlanet != null)
            {
                startIndex = 2;
                if (__instance.astroBox.Items.Count > 2 
                    && (__instance.astroBox.Items[2] == "Local System"
                    || __instance.astroBox.Items[2] == "本地系统"
                    || __instance.astroBox.Items[2] == "localSystemLabel".Translate()))
                    startIndex = 3; // new option in Bottleneck
            }
            Utils.UpdateAstroBox(__instance.astroBox, startIndex, searchStr);
            __state = __instance.astroBox.itemIndex;
            //Plugin.Log.LogDebug(System.Environment.StackTrace);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ValueToAstroBox))]
        static void RestoreItemIndex(UIStatisticsWindow __instance)
        {
            if (!__instance.isStatisticsTab || __instance.astroBox == null) return;

            /*
            // 如果有不同名稱的項目有同一個data(Bottleneck本地系統), 則回復原本的itemIndex
            try
            {
                int length = __instance.astroBox.ItemsData.Count;
                if (__state != __instance.astroBox.itemIndex && __state >= 0 && __state < length && __instance.astroBox.itemIndex < length)
                {
                    if (__instance.astroBox.ItemsData[__state] == __instance.astroBox.ItemsData[__instance.astroBox.itemIndex])
                        __instance.astroBox.itemIndex = __state;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Plugin.Log.LogDebug(e);
#endif
            }
            */

            if (locateBtn == null) return;
            var astroId = __instance.astroFilter;
            if (astroId <= 0 )
            {
                locateBtn.tips.tipTitle = "Locate";
                return;
            }

            string locateString = "Locate " + astroId;
            var planet = GameMain.galaxy.PlanetById(astroId);
            if (planet?.factory != null)
                locateString += " idx=" + planet.factory.index;
            locateBtn.tips.tipTitle = locateString;
        }
        #endregion

        public class Entry_Patch // 時間範圍
        {

            public static void OnSliderChange(float value)
            {
                if (value < 1)
                {
                    timerSlider.value = 1.0f;
                    return;
                }

                ratio = value / timerSlider.maxValue;
                UIRoot.instance.uiGame.statWindow.ComputeDisplayEntriesDetail();
                if (ratio == 1.0f)
                    UIRoot.instance.uiGame.statWindow.ValueToTimeBox(); // Reset string
                else
                    ChangeTimeBoxText(UIRoot.instance.uiGame.statWindow);
            }

            [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ValueToTimeBox))]
            static void ChangeTimeBoxText(UIStatisticsWindow __instance)
            {
                if (ratio == 1.0f || !__instance.isStatisticsTab) return;

                string percentageStr = $" ({100f * ratio:N0}%)";
                var input = UIRoot.instance.uiGame.statWindow.timeBox.m_Input;

                switch (__instance.timeLevel)
                {
                    case 0:
                        input.text = ((int)(60f * ratio + 0.5f)).ToString() + "空格秒".Translate() + percentageStr;
                        break;

                    case 1:
                        input.text = (10f * ratio).ToString("F1") + "空格分钟".Translate() + percentageStr;
                        break;

                    case 2:
                        input.text = (60f * ratio).ToString("F1") + "空格分钟".Translate() + percentageStr;
                        break;

                    case 3:
                        input.text = (10f * ratio).ToString("F1") + "统计10小时".Translate().Replace("10", " ") + percentageStr;
                        break;

                    case 4:
                        input.text = (100f * ratio).ToString("F1") + "统计100小时".Translate().Replace("100", " ") + percentageStr;
                        break;

                    case 5:
                        input.text = "统计总计".Translate() + percentageStr;
                        break;
                }
            }

            struct Data
            {
                public long production;
                public long consumption;
            }

            static float ratio = 1.0f;
            static readonly Dictionary<long[], Data> dict = new();

            [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeDisplayEntriesDetail))]
            static void ClearData()
            {
                dict.Clear();
            }

            // FirstHalf = 直方圖上半部分
            [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeFirstHalfDetail),
                new Type[] { typeof(int), typeof(int), typeof(int), typeof(int[]), typeof(long[]) })]
            static bool ComputeFirstHalfDetail(int endCursor, int lvlen, int cur, int[] detail, long[] targetDetail)
            {
                if (ratio == 1.0f) return true;

                dict.TryGetValue(targetDetail, out var data);
                int start = (int)(lvlen * (1f - ratio) + 0.5f); //擷取最近的ratio%統計數據
                int num = cur + start - 1;
                for (int i = 0; i < lvlen - start; i++)
                {
                    if (++num > endCursor)
                    {
                        num -= lvlen;
                    }
                    long value = detail[num];
                    data.production += value; //有多個facotryStat會累加至同一個UIProductEntry
                    int len = (int)((i + 1) / ratio); //直方圖拉伸
                    for (int j = (int)(i / ratio); j < len; j++)
                        targetDetail[j] += value; //填滿直方圖

                }
                dict[targetDetail] = data;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeFirstHalfDetail),
        new Type[] { typeof(int), typeof(int), typeof(int), typeof(long[]), typeof(long[]) })] // 電力, 研究
            static bool ComputeFirstHalfDetail(int endCursor, int lvlen, int cur, long[] detail, long[] targetDetail)
            {
                if (ratio == 1.0f) return true;

                dict.TryGetValue(targetDetail, out var data);
                int start = (int)(lvlen * (1f - ratio) + 0.5f);
                int num = cur + start - 1;
                for (int i = 0; i < lvlen - start; i++)
                {
                    if (++num > endCursor)
                    {
                        num -= lvlen;
                    }
                    long value = detail[num];
                    data.production += value;
                    int len = (int)((i + 1) / ratio);
                    for (int j = (int)(i / ratio); j < len; j++)
                        targetDetail[j] += value;
                }
                dict[targetDetail] = data;
                return false;
            }

            // SecondHalf = 直方圖下半部分
            [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeSecondHalfDetail),
        new Type[] { typeof(int), typeof(int), typeof(int), typeof(int[]), typeof(long[]) })]
            static bool ComputeSecondHalfDetail(int endCursor, int lvlen, int cur, int[] detail, long[] targetDetail)
            {
                if (ratio == 1.0f) return true;

                dict.TryGetValue(targetDetail, out var data);
                int start = (int)(lvlen * (1f - ratio) + 0.5f);
                int num = cur + start - 1;
                for (int i = 0; i < lvlen - start; i++)
                {
                    if (++num > endCursor)
                    {
                        num -= lvlen;
                    }
                    long value = detail[num];
                    data.consumption += value;
                    int len = (int)((i + 1) / ratio);
                    for (int j = (int)(i / ratio); j < len; j++)
                        targetDetail[lvlen + j] += value;
                }
                dict[targetDetail] = data;
                return false;
            }

            [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeSecondHalfDetail),
    new Type[] { typeof(int), typeof(int), typeof(int), typeof(long[]), typeof(long[]) })]
            static bool ComputeSecondHalfDetail(int endCursor, int lvlen, int cur, long[] detail, long[] targetDetail)
            {
                if (ratio == 1.0f) return true;

                dict.TryGetValue(targetDetail, out var data);
                int start = (int)(lvlen * (1f - ratio) + 0.5f);
                int num = cur + start - 1;
                for (int i = 0; i < lvlen - start; i++)
                {
                    if (++num > endCursor)
                    {
                        num -= lvlen;
                    }
                    long value = detail[num];
                    data.consumption += value;
                    int len = (int)((i + 1) / ratio);
                    for (int j = (int)(i / ratio); j < len; j++)
                        targetDetail[lvlen + j] += value;
                }
                dict[targetDetail] = data;
                return false;
            }

            [HarmonyPostfix, HarmonyAfter("Bottleneck"), HarmonyPriority(Priority.VeryLow)]
            [HarmonyPatch(typeof(UIProductEntry), nameof(UIProductEntry._OnUpdate))] //單項產物UI
            static void UIProductEntry_ShowInText(UIProductEntry __instance)
            {
                if (!__instance.productionStatWindow.isProductionTab) return; //時間範圍限定在生產統計頁面生效

                int level = __instance.productionStatWindow.timeLevel;
                double production, consumption;

                if (level < 5) //level = 5: total總計 不會有速率單位
                {
                    __instance.productUnitLabel.text = __instance.consumeUnitLabel.text = DisplayPerSecond ? "/ s" : "/ min";
                }

                if (RateFontSize > 0) //修改速率/參考速率字型大小
                {
                    __instance.productText.fontSize = __instance.consumeText.fontSize = RateFontSize;
                    __instance.productRefSpeedText.fontSize = __instance.consumeRefSpeedText.fontSize = RateFontSize;
                }

                // 展開直方圖 ExtendGraph
                __instance.statGraph.transform.localScale = ExtendGraph ? new Vector3(2.5f, 1f, 1f) : Vector3.one;
                __instance.importText.transform.parent.gameObject.SetActive(!ExtendGraph);
                __instance.exportText.transform.parent.gameObject.SetActive(!ExtendGraph);
                __instance.importStorageCountText.transform.parent.gameObject.SetActive(!ExtendGraph);
                __instance.exportStorageCountText.transform.parent.gameObject.SetActive(!ExtendGraph);
                __instance.storageCountText.transform.parent.gameObject.SetActive(!ExtendGraph);
                __instance.trashCountText.transform.parent.gameObject.SetActive(!ExtendGraph);

                if (ratio == 1.0f || !dict.TryGetValue(__instance.entryData.productDetail, out var value))
                {
                    if (DisplayPerSecond && level < 5)
                    {
                        // 以下從UIProductEntry.ShowInText修改
                        production = __instance.entryData.production;
                        consumption = __instance.entryData.consumption;
                        if (production < 0.0) production = -production;
                        if (consumption < 0.0) consumption = -consumption;

                        if (level < 5)
                        {
                            var lvDivisor = __instance.lvDivisors[level];
                            production /= lvDivisor;
                            consumption /= lvDivisor;
                            if (DisplayPerSecond) //顯示每秒產量
                            {
                                production /= 60;
                                consumption /= 60;
                            }
                        }
                        __instance.productText.text = ToLevelString(production); //自定義函數
                        __instance.consumeText.text = ToLevelString(consumption);
                    }
                    return;
                }

                production = value.production;
                consumption = value.consumption;
                if (level < 5)
                {
                    production /= __instance.lvDivisors[level] * ratio; //依照時間範圍校正
                    consumption /= __instance.lvDivisors[level] * ratio;
                    if (DisplayPerSecond) //顯示每秒產量
                    {
                        production /= 60;
                        consumption /= 60;
                    }
                }
                if (DisplayPerSecond) //每秒時, 使用自定義函數
                {
                    __instance.productText.text = ToLevelString(production);
                    __instance.consumeText.text = ToLevelString(consumption);
                }
                else
                {
                    __instance.productText.text = __instance.ToLevelString(production, level);
                    __instance.consumeText.text = __instance.ToLevelString(consumption, level);
                }
            }

            readonly static string[] formatF = { "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10" };
            static string ToLevelString(double value) //輸出: {有效位數}+{單位}(K,M)
            {
                if (value == 0.0) return "0";

                string unit = "";
                if (value >= 1000000.0) // value >= 1M
                {
                    value /= 1000000.0;
                    unit = " M";
                }
                else if (value >= 10000.0) // value >= 10k
                {
                    value /= 1000.0;
                    unit = " k";
                }

                int digit = 0;
                if (value >= 1000.0) digit = 3;
                else if (value >= 100.0) digit = 2;
                else if (value >= 10.0) digit = 1;

                digit = SignificantDigits - digit - 1;
                if (digit < 0) digit = 0;
                if (digit >= formatF.Length) digit = formatF.Length - 1;

                if (digit > 0)
                {
                    double fraction = 0.1;
                    for (int i = 0; i < digit; i++)
                    {
                        fraction *= 0.1;
                    }
                    if (value - (int)value < fraction) return value.ToString("F1") + unit; //小數部分皆為0,只保留1位
                }
                return value.ToString(formatF[digit]) + unit;
            }

            [HarmonyPostfix, HarmonyAfter("Bottleneck"), HarmonyPriority(Priority.VeryLow)]
            [HarmonyPatch(typeof(UIKillEntry), nameof(UIKillEntry._OnUpdate))] //擊殺統計
            static void UIKillEntry_ShowInText(UIKillEntry __instance)
            {
                if (ratio == 1.0f) return;
                if (!dict.TryGetValue(__instance.entryData.detail, out var value)) return;

                int timeLevel = __instance.productionStatWindow.timeLevel;
                double production = value.production;
                if (timeLevel < 5)
                {
                    production /= __instance.lvDivisors[timeLevel] * ratio; //依照時間範圍校正除數
                    if (DisplayPerSecond) //顯示每秒產量
                    {
                        production /= 60;
                    }
                }
                __instance.killText.text = __instance.ToLevelString(production, timeLevel);
            }

            [HarmonyPostfix, HarmonyPriority(Priority.VeryLow)]
            [HarmonyPatch(typeof(UIProductEntry), nameof(UIProductEntry.RefreshStatGraphHover))] //直方圖單條彈出提示
            static void RefreshStatGraphHover_Postfix(UIProductEntry __instance, float statGraphWidth)
            {
                if (ratio == 1.0f || __instance.mouseHoverStatGraphTip == null) return;

                Vector3 mousePosition = Input.mousePosition;
                RectTransform rectTransform = __instance.statGraph.rectTransform;
                if (__instance.isPointerEnter)
                {
                    if (UIRoot.ScreenPointIntoRect(mousePosition, rectTransform, out Vector2 vector))
                    {
                        vector = new Vector2(rectTransform.rect.width + vector.x, rectTransform.rect.height * 0.5f + vector.y);
                        if (vector.x > 0f && vector.x < statGraphWidth && vector.y > 0f && vector.y < 100f)
                        {
                            if (__instance.productionStatWindow.isProductionTab)
                            {
                                if (__instance.mouseHoverStatGraphTip.gameObject.activeSelf)
                                {
                                    // 移除第一和第二行, 並用ratio修正後的時間和週期取代
                                    string tipText = __instance.statGraphTipSetting.tipText;
                                    int index = tipText.IndexOf('\n', tipText.IndexOf('\n') + 1);
                                    if (index > 0)
                                    {
                                        // time:依照鼠標位置得到起始時間
                                        int num = (int)(vector.x / (__instance.columnWidth + __instance.columnInterval));
                                        int num2 = (int)((statGraphWidth + __instance.columnInterval) / (__instance.columnWidth + __instance.columnInterval));
                                        long num3 = GameMain.gameTick - __instance.levelTick;
                                        int num4 = __instance.levelTick * __instance.columnDataCount;
                                        long num5 = num3 / num4 * num4 - ((num2 - num - 1) * num4);
                                        if (num5 < 0L)
                                        {
                                            num5 = 0L;
                                        }
                                        uint num6 = (uint)(num5 * 0.016666666666666666f * ratio);
                                        uint num7 = num6 / 3600U;
                                        uint num8 = num6 / 60U % 60U;
                                        uint num9 = num6 % 60U;

                                        __instance.sb3.Clear();
                                        __instance.sb3.Append("00000:00:00");
                                        StringBuilderUtility.WriteUInt(__instance.sb3, 0, 5, num7, 2, ' ');
                                        StringBuilderUtility.WriteUInt(__instance.sb3, 6, 2, num8, 2, ' ');
                                        StringBuilderUtility.WriteUInt(__instance.sb3, 9, 2, num9, 2, ' ');
                                        StringBuilderUtility.TrimStart(__instance.sb3);
                                        __instance.sb3.Insert(0, "起始时间冒号".Translate());

                                        // period:由參數計算時間週期
                                        float timeIntervalTick = (__instance.levelTick * __instance.columnDataCount) * ratio;
                                        uint timeIntervalSec = (uint)(timeIntervalTick * 0.016666666666666666f);
                                        uint num11 = timeIntervalSec / 3600U;
                                        uint num12 = timeIntervalSec / 60U % 60U;
                                        uint num13 = timeIntervalSec % 60U;
                                        uint num14 = (uint)timeIntervalTick % 60;
                                        __instance.sb3.Append("\n").Append("时间周期冒号".Translate());
                                        if (num11 > 0U)
                                        {
                                            __instance.sb3.Append(num11).Append("h ");
                                        }
                                        if (num12 > 0U)
                                        {
                                            __instance.sb3.Append(num12).Append("min ");
                                        }
                                        if (num13 > 0U)
                                        {
                                            __instance.sb3.Append(num13).Append("s ");
                                        }
                                        if (num14 > 0U)
                                        {
                                            __instance.sb3.Append(num14).Append("t");
                                        }

                                        tipText = __instance.sb3.ToString() + tipText.Substring(index, tipText.Length - index);
                                        __instance.statGraphTipSetting.tipText = tipText;
                                    }
                                    __instance.mouseHoverStatGraphTip.UpdateText(ref __instance.statGraphTipSetting, null, null);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

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
        public static bool OrderByName = true;
        public static KeyCode HotkeyListUp = KeyCode.PageUp;
        public static KeyCode HotkeyListDown = KeyCode.PageDown;

        static bool initialized;
        static Slider timerSlider;
        static InputField filterInput;
        static UIButton locateBtn;

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnOpen))]
        public static void Init(UIStatisticsWindow __instance)
        {
            if (!initialized)
            {
                try
                {
                    Slider slider0 = UIRoot.instance.uiGame.dysonEditor.controlPanel.inspector.layerInfo.slider0;
                    GameObject inputObj = GameObject.Find("UI Root/Overlay Canvas/In Game/Globe Panel/name-input");
                    UIButton uIButton0 = UIRoot.instance.uiGame.researchQueue.pauseButton;

                    var go = GameObject.Instantiate(slider0.gameObject, __instance.productTimeBox.transform);
                    go.name = "CustomStats_Ratio";
                    go.transform.localPosition = new Vector3(-155f, 8f, 0);
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 13);
                    timerSlider = go.GetComponent<Slider>();
                    timerSlider.minValue = 0;
                    timerSlider.maxValue = 20;
                    timerSlider.wholeNumbers = true;
                    timerSlider.value = timerSlider.maxValue;
                    timerSlider.onValueChanged.AddListener(new UnityAction<float>(OnSliderChange));
                    //tmp.transform.GetChild(1).GetComponent<Image>().color = new Color(0.3f, 1.0f, 1.0f, 0.47f); //改成亮藍色
                    go.SetActive(true);

                    go = GameObject.Instantiate(inputObj, __instance.productAstroBox.transform);
                    go.name = "CustomStats_Fliter";
                    go.transform.localPosition = new Vector3(-201.5f, 30f, 0);
                    filterInput = go.GetComponent<InputField>();
                    filterInput.text = "";
                    filterInput.onValueChanged.AddListener(new UnityAction<string>(OnInputValueChanged));
                    go.GetComponent<RectTransform>().sizeDelta = new Vector2(203f, 28f);
                    go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
                    // 在聚焦輸入框時無法使用滾輪, 因此不套用以下的聚焦設定
                    // tmp.transform.parent.GetChild(0).GetComponent<Button>().onClick.AddListener(new UnityAction(OnComboBoxClicked));
                    go.SetActive(true);

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

                    initialized = true;

                    if (__instance.astroBox != __instance.productAstroBox)
                        OnTabButtonClick(__instance);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("UI component initial fail!");
                    Plugin.Log.LogError(e);
                }
            }
        }

        public static void OnDestory()
        {
            GameObject.Destroy(timerSlider?.gameObject);
            GameObject.Destroy(filterInput?.gameObject);
            GameObject.Destroy(locateBtn?.gameObject);
            initialized = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.OnTabButtonClick))]
        static void OnTabButtonClick(UIStatisticsWindow __instance) // 切換頁面時, 重新設置UI元件的父元件
        {
            if (!initialized) return;
            if (__instance.timeBox != null)
                timerSlider.gameObject.transform.SetParent(__instance.timeBox.transform);
            if (__instance.astroBox != null)
            {
                filterInput.gameObject.transform.SetParent(__instance.astroBox.transform);
                locateBtn.gameObject.transform.SetParent(__instance.astroBox.transform);
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow._OnUpdate))]
        static void OnUpdate(UIStatisticsWindow __instance)
        {
            if (__instance.isStatisticsTab)
            {
                int itemIndex = __instance.astroBox.itemIndex;
                if (Input.GetKeyDown(HotkeyListUp))
                {
                    if (VFInput.control) // 上一個星系
                    {
                        do
                        {
                            itemIndex = itemIndex > 0 ? itemIndex - 1 : __instance.astroBox.ItemsData.Count - 1;
                            int astroFilter = __instance.astroBox.ItemsData[itemIndex];
                            if ((astroFilter % 100 == 0 && astroFilter > 0) || astroFilter == -1)
                                break;
                        } while (itemIndex != __instance.astroBox.itemIndex);
                        __instance.astroBox.itemIndex = itemIndex;
                    }
                    else // 上一個列表
                    {
                        __instance.astroBox.itemIndex = itemIndex > 0 ? itemIndex - 1 : __instance.astroBox.ItemsData.Count - 1;
                    }
                }
                else if (Input.GetKeyDown(HotkeyListDown))
                {
                    if (VFInput.control)
                    {
                        do
                        {
                            itemIndex = (itemIndex + 1) % __instance.astroBox.ItemsData.Count;
                            int astroFilter = __instance.astroBox.ItemsData[itemIndex];
                            if ((astroFilter % 100 == 0 && astroFilter > 0) || astroFilter == -1)
                                break;
                        } while (itemIndex != __instance.astroBox.itemIndex);
                        __instance.astroBox.itemIndex = itemIndex;
                    }
                    else
                    {
                        __instance.astroBox.itemIndex = (itemIndex + 1) % __instance.astroBox.ItemsData.Count;
                    }
                }
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

            if (planetIdx >= 0)
            {
                PlanetData planet = GameMain.galaxy.PlanetById(astroId);
                if (planet != null)
                {
                    map.focusPlanet = null;
                    map.focusStar = map.starUIs[starIdx];
                    map.OnCursorFunction2Click(0);
                    if (map.focusStar == null)
                    {
                        map.focusPlanet = map.planetUIs[planetIdx];
                        map.OnCursorFunction2Click(0);
                        //map.SetViewStar(star.star, true);
                        map.focusPlanet = map.planetUIs[planetIdx]; //Function Panelを表示させるため
                        map.focusStar = null;
                    }
                }
            }
            else
            {
                map.focusStar = map.starUIs[starIdx];
                map.OnCursorFunction2Click(0);
            }
        }

        #endregion

        #region 列表修改

        static List<ValueTuple<string, int>> systemList = new();
        static List<string> newItems = new();
        static List<int> newItemData = new();
        static string searchStr = "";
        static int startIndex = 2;

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

            if (__instance.astroBox.Items.Count > 2 && __instance.astroBox.Items[2] == "localSystemLabel".Translate())
            {
                //Plugin.Log.LogDebug("Bottleneck: localSystem enabled.");
                startIndex = 3;
            }

            if (OrderByName) // 以星系名稱排序
            {
                systemList.Clear();
                newItems.Clear();
                newItemData.Clear();

                for (int i = startIndex; i < __instance.astroBox.Items.Count; i++)
                {
                    if (__instance.astroBox.ItemsData[i] % 100 == 0)
                        systemList.Add((__instance.astroBox.Items[i], __instance.astroBox.ItemsData[i]));
                }
                systemList.Sort();

                foreach (var tuple in systemList)
                {
                    int starId = tuple.Item2 / 100;
                    for (int i = startIndex; i < __instance.astroBox.Items.Count; i++)
                    {
                        int astroId = __instance.astroBox.ItemsData[i];
                        if (astroId / 100 == starId)
                        {
                            newItems.Add(__instance.astroBox.Items[i]);
                            newItemData.Add(__instance.astroBox.ItemsData[i]);
                        }
                    }
                }
                __instance.astroBox.Items.RemoveRange(startIndex, __instance.astroBox.Items.Count - startIndex);
                __instance.astroBox.ItemsData.RemoveRange(startIndex, __instance.astroBox.ItemsData.Count - startIndex);
                __instance.astroBox.Items.AddRange(newItems);
                __instance.astroBox.ItemsData.AddRange(newItemData);
            }

            if (!string.IsNullOrWhiteSpace(searchStr))
            {
                for (int i = __instance.astroBox.Items.Count - 1; i >= startIndex; i--)
                {
                    if (__instance.astroBox.Items[i].IndexOf(searchStr, StringComparison.OrdinalIgnoreCase) == -1)
                    {
                        __instance.astroBox.Items.RemoveAt(i);
                        __instance.astroBox.ItemsData.RemoveAt(i);
                    }
                }
            }

            __state = __instance.astroBox.itemIndex;
            //Plugin.Log.LogDebug(System.Environment.StackTrace);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ValueToAstroBox))]
        static void RestoreItemIndex(UIStatisticsWindow __instance, int __state)
        {
            if (!__instance.isStatisticsTab || __instance.astroBox == null) return;

            // 如果有不同名稱的項目有同一個data(Bottleneck本地系統), 則回復原本的itemIndex
            try
            {
                int length = __instance.astroBox.ItemsData.Count;
                if (__state != __instance.astroBox.itemIndex && __state < length && __instance.astroBox.itemIndex < length)
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
        }
        #endregion

        #region 時間範圍

        static void OnSliderChange(float value)
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

        [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeFirstHalfDetail),
            new Type[] { typeof(int), typeof(int), typeof(int), typeof(int[]), typeof(long[]) })]
        static bool ComputeFirstHalfDetail(int lastCursor, int lvlen, int cur, int[] detail, long[] targetDetail)
        {
            if (ratio == 1.0f) return true;

            dict.TryGetValue(targetDetail, out var data);
            int start = (int)(lvlen * (1f - ratio) + 0.5f); //擷取最近的ratio%統計數據
            int num = cur + start - 1;
            for (int i = start; i < lvlen; i++)
            {
                if (++num > lastCursor)
                {
                    num -= lvlen;
                }
                targetDetail[i] += detail[num];
                data.production += detail[num]; //有多個facotryStat會累加至同一個UIProductEntry
            }
            dict[targetDetail] = data;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeFirstHalfDetail),
    new Type[] { typeof(int), typeof(int), typeof(int), typeof(long[]), typeof(long[]) })] // 電力, 研究
        static bool ComputeFirstHalfDetail(int lastCursor, int lvlen, int cur, long[] detail, long[] targetDetail)
        {
            if (ratio == 1.0f) return true;

            dict.TryGetValue(targetDetail, out var data);
            int start = (int)(lvlen * (1f - ratio) + 0.5f);
            int num = cur + start - 1;
            for (int i = start; i < lvlen; i++)
            {
                if (++num > lastCursor)
                {
                    num -= lvlen;
                }
                targetDetail[i] += detail[num];
                data.production += detail[num];
            }
            dict[targetDetail] = data;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeSecondHalfDetail),
    new Type[] { typeof(int), typeof(int), typeof(int), typeof(int[]), typeof(long[]) })]
        static bool ComputeSecondHalfDetail(int lastCursor, int lvlen, int cur, int[] detail, long[] targetDetail)
        {
            if (ratio == 1.0f) return true;

            dict.TryGetValue(targetDetail, out var data);
            int start = (int)(lvlen * (1f - ratio) + 0.5f);
            int num = cur + start - 1;
            for (int i = start; i < lvlen; i++)
            {
                if (++num > lastCursor)
                {
                    num -= lvlen;
                }
                targetDetail[i + lvlen] += detail[num];
                data.consumption += detail[num];
            }
            dict[targetDetail] = data;
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIStatisticsWindow), nameof(UIStatisticsWindow.ComputeSecondHalfDetail),
new Type[] { typeof(int), typeof(int), typeof(int), typeof(long[]), typeof(long[]) })]
        static bool ComputeSecondHalfDetail(int lastCursor, int lvlen, int cur, long[] detail, long[] targetDetail)
        {
            if (ratio == 1.0f) return true;

            dict.TryGetValue(targetDetail, out var data);
            int start = (int)(lvlen * (1f - ratio) + 0.5f);
            int num = cur + start - 1;
            for (int i = start; i < lvlen; i++)
            {
                if (++num > lastCursor)
                {
                    num -= lvlen;
                }
                targetDetail[i + lvlen] += detail[num];
                data.consumption += detail[num];
            }
            dict[targetDetail] = data;
            return false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIProductEntry), nameof(UIProductEntry.ShowInText))]
        static void ShowInText(UIProductEntry __instance, int level)
        {
            if (ratio == 1.0f || __instance.productionStatWindow.isPowerTab) return; //電力統計是實時數據, 不受時間範圍影響
            if (!dict.TryGetValue(__instance.entryData.detail, out var value)) return;

            double production = value.production;
            double consumption = value.consumption;
            if (level != 5)
            {
                production /= __instance.lvDivisors[level] * ratio; //依照時間範圍校正
                consumption /= __instance.lvDivisors[level] * ratio;
            }
            __instance.productText.text = __instance.ToLevelString(production, level);
            __instance.consumeText.text = __instance.ToLevelString(consumption, level);
        }

        #endregion
    }
}

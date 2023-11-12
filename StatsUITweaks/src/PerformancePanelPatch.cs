using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace StatsUITweaks
{
    public class PerformancePanelPatch
    {
        static bool initialized;
        static UIButton foldBtn;
        static bool folded;
        static float scrollHeight = 398f;
        static float scrollY = 23.5f;
        static float activeButtonY = 53.5f;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        public static void Init(UIPerformancePanel __instance)
        {
            Plugin.Log.LogDebug("PerformancePanelPatch initial");
            if (!initialized)
            {
                try
                {
                    UIButton uIButton0 = UIRoot.instance.uiGame.researchQueue.pauseButton;

                    var go = GameObject.Instantiate(uIButton0.gameObject, __instance.transform);
                    go.name = "CustomStats_Fold";
                    go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    go.transform.localPosition = new Vector3(-550f, 390f, 0f);
                    Image img = go.transform.Find("icon")?.GetComponent<Image>();
                    if (img != null)
                    {
                        UIStarmap starmap = UIRoot.instance.uiGame.starmap;
                        img.sprite = starmap.cursorFunctionButton2.transform.Find("icon")?.GetComponent<Image>()?.sprite;
                    }
                    foldBtn = go.GetComponent<UIButton>();
                    foldBtn.tips.tipTitle = "Fold 折叠饼图";
                    foldBtn.tips.tipText = "Click to fold/unfold pie chart";
                    foldBtn.onClick += OnFoldButtonClick;
                    go.SetActive(true);

                    // Record original values
                    scrollHeight = __instance.cpuScrollRect.rectTransform.sizeDelta.y;
                    scrollY = __instance.cpuScrollRect.transform.localPosition.y;
                    activeButtonY = __instance.cpuActiveButton.transform.localPosition.y;
                    Plugin.Log.LogDebug($"{scrollHeight} {scrollY} {activeButtonY}");

                    initialized = true;
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError("PerformancePanelPatch initial fail!");
                    Plugin.Log.LogError(e);
                }
            }

            if (initialized)
            {

            }
        }

        public static void OnDestory()
        {
            GameObject.Destroy(foldBtn?.gameObject);
            initialized = false;
        }

        static void OnFoldButtonClick(int obj)
        {
            folded = !folded;
            foldBtn.highlighted = folded;
            Plugin.Log.LogDebug(folded);
            Toggle(folded);
        }

        private static void Toggle(bool extendScroll)
        {
            if (initialized)
            {
                var panel = UIRoot.instance.uiGame.statWindow.performancePanelUI;

                Transform transform = panel.transform;
                Adjust(panel.cpuScrollRect.rectTransform, panel.cpuActiveButton.transform, extendScroll);
                Adjust(panel.gpuScrollRect.rectTransform, panel.gpuActiveButton.transform, extendScroll);
                Adjust(panel.dataScrollRect.rectTransform, panel.dataActiveButton.transform, extendScroll);
            }
        }

        private static void Adjust(RectTransform scrollRect, Transform activeBtnRect, bool extendScroll)
        {
            foreach (Transform child in scrollRect.parent)
            {
                child.gameObject.SetActive(!extendScroll);
            }

            scrollRect.gameObject.SetActive(true);
            scrollRect.localPosition = new Vector3(scrollRect.localPosition.x, extendScroll ? scrollY + 346.5f : scrollY, 0f); // 370f
            scrollRect.sizeDelta = new Vector2(scrollRect.sizeDelta.x, extendScroll ? scrollHeight + 300f : scrollHeight); //698f

            activeBtnRect.gameObject.SetActive(true);
            activeBtnRect.localPosition = new Vector3(activeBtnRect.localPosition.x, extendScroll ? activeButtonY - 583.5f : activeButtonY, 0f); // -530f
        }
    }
}

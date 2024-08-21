using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace StatsUITweaks
{
    public class PerformancePanelPatch
    {
        static bool initialized;
        static UIButton foldBtn;
        static bool folded;
        static float scrollHeight = 398f;
        static float scrollY = 23.5f;

        [HarmonyPostfix, HarmonyPatch(typeof(UIPerformancePanel), nameof(UIPerformancePanel._OnOpen))]
        public static void Init(UIPerformancePanel __instance)
        {
            if (initialized) return;

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
                if (foldBtn.transitions != null)
                    foldBtn.transitions[0].highlightColorOverride = new Color(0.5f, 0.6f, 0.7f, 0.1f); //用於highlighted
                go.SetActive(true);

                // Record original values
                scrollHeight = __instance.cpuScrollRect.rectTransform.sizeDelta.y;
                scrollY = __instance.cpuScrollRect.transform.localPosition.y;

                initialized = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PerformancePanelPatch initial fail!");
                Plugin.Log.LogError(e);
            }
            Toggle(folded);
        }

        public static void OnDestory()
        {
            Toggle(false);
            GameObject.Destroy(foldBtn?.gameObject);
            initialized = false;
        }

        static void OnFoldButtonClick(int obj)
        {
            folded = !folded;
            foldBtn.highlighted = folded;
            Toggle(folded);
        }

        private static void Toggle(bool extendScroll)
        {
            if (initialized)
            {
                var panel = UIRoot.instance.uiGame.statWindow.performancePanelUI;
                Adjust(panel.cpuScrollRect.rectTransform, extendScroll);
                Adjust(panel.gpuScrollRect.rectTransform, extendScroll);
                Adjust(panel.dataScrollRect.rectTransform, extendScroll);
            }
        }

        private static void Adjust(RectTransform scrollRect, bool extendScroll)
        {
            foreach (Transform child in scrollRect.parent)
            {
                child.gameObject.SetActive(!extendScroll);
            }
            scrollRect.gameObject.SetActive(true);
            scrollRect.localPosition = new Vector3(scrollRect.localPosition.x, extendScroll ? scrollY + 362f : scrollY, 0f);
            scrollRect.sizeDelta = new Vector2(scrollRect.sizeDelta.x, extendScroll ? scrollHeight + 372f : scrollHeight);
        }
    }
}

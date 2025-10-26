using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace UITweaks
{
    public class TechTree_Tweaks
    {
        private static UITechNode currentSelectNode;
        private static UIButton locateBtn;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree._OnInit))]
        internal static void Init()
        {
            foreach (var node in UIRoot.instance.uiGame.techTree.nodes.Values)
            {
                AddCostPreview(node);
            }
        }

#if DEBUG
        internal static void Free()
        {
            UnityEngine.Object.Destroy(locateBtn?.gameObject);
            //RemoveCostPreviews();
            foreach (var node in UIRoot.instance.uiGame.techTree.nodes.Values)
            {
                RemoveCostPreview(node);
            }
        }
#endif

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechNode), nameof(UITechNode.DeterminePrerequisiteSuffice))]
        public static void SkipMetadataRequirement(ref bool __result)
        {
            __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(UITechTree), nameof(UITechTree._OnLateUpdate))]
        public static void UpdateNaviBtn(UITechTree __instance)
        {
            try
            {
                if (currentSelectNode == __instance.selected) return;
                // The current select node is changed.
                var lastSelectNode = currentSelectNode;
                currentSelectNode = __instance.selected;

                SetCostPreviewActive(lastSelectNode, true); //重新顯示上一個node的預覽
                SetCostPreviewActive(currentSelectNode, false); //隱藏目前node的預覽, 避免和文字重疊
                if (currentSelectNode == null) //沒有選擇任何node
                {
                    locateBtn?.gameObject.SetActive(false);
                    return;
                }

                // 設置locate按鈕的屬性, 導引至紅字前置科技
                if (locateBtn == null) AddBtn(currentSelectNode.gameObject.transform);
                var preTechId = GameMain.history.ImplicitPreTechRequired(currentSelectNode.techProto?.ID ?? 0);
                if (preTechId != 0)
                {
                    locateBtn.transform.SetParent(currentSelectNode.transform);
                    locateBtn.transform.localPosition = new Vector3(286f, -218f, 0f);
                    locateBtn.gameObject.SetActive(true);
                }
                else
                {
                    locateBtn.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private static void AddBtn(Transform parent)
        {
            var go = UnityEngine.Object.Instantiate(UIRoot.instance.uiGame.researchQueue.pauseButton.gameObject, parent);
            go.name = "ReorderTechQueue_Navi";
            go.transform.localScale = new Vector3(0.33f, 0.33f, 0);
            go.transform.localPosition = new Vector3(286f, -218f, 0f);
            Image img = go.transform.Find("icon")?.GetComponent<Image>();
            if (img != null)
            {
                UIStarmap starmap = UIRoot.instance.uiGame.starmap;
                img.sprite = starmap.cursorFunctionButton3.transform.Find("icon")?.GetComponent<Image>()?.sprite;
            }
            locateBtn = go.GetComponent<UIButton>();
            locateBtn.tips.tipTitle = "Locate";
            locateBtn.tips.tipText = "Navigate to the required tech";
            locateBtn.onClick += OnLocateButtonClick;
        }

        private static void OnLocateButtonClick(int obj)
        {
            if (currentSelectNode == null) return;

            var preTechId = GameMain.history.ImplicitPreTechRequired(currentSelectNode.techProto?.ID ?? 0);
            if (preTechId != 0) UIRoot.instance.uiGame.techTree.SelectTech(preTechId);
        }

        private static void AddCostPreview(UITechNode node)
        {
            if (node == null) return;
            var iconGo = node.gameObject.transform.Find("icon")?.gameObject;
            if (iconGo == null) return;

            var length = Math.Min(node.techProto.itemArray.Length, 6);
            var xoffset = 0;
            for (int i = length - 1; i >= 0; i--)
            {
                if (node.techProto.itemArray[i] == null) continue;

                var go = UnityEngine.Object.Instantiate(iconGo, node.gameObject.transform);
                go.name = "CostPreviewIcon-" + i;
                go.transform.localPosition = new Vector3(190 - (xoffset++) * 15, -124, 0);
                go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                go.transform.GetComponent<Image>().sprite = node.techProto.itemArray[i].iconSprite;
            }
        }

        private static void SetCostPreviewActive(UITechNode node, bool active)
        {
            if (node == null) return;
            for (int i = 0; i < 6; i++)
                node.gameObject.transform.Find("CostPreviewIcon-" + i)?.gameObject.SetActive(active);
        }

        private static void RemoveCostPreview(UITechNode node)
        {
            if (node == null) return;
            for (int i = 0; i < 6; i++)
                UnityEngine.Object.Destroy(node.gameObject.transform.Find("CostPreviewIcon-" + i)?.gameObject);
        }
    }
}

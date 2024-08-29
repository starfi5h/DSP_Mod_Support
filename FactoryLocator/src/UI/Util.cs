using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FactoryLocator.UI
{
    // modify from PlanetFinder mod
    // special thanks to hetima

    public class Util
    {
        public static RectTransform NormalizeRectWithTopLeft(Component cmp, float left, float top, Transform parent = null)
        {
            RectTransform rect = cmp.transform as RectTransform;
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }
            rect.anchorMax = new Vector2(0f, 1f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition3D = new Vector3(left, -top, 0f);
            return rect;
        }

        public static RectTransform NormalizeRectWithMargin(Component cmp, float top, float left, float bottom, float right, Transform parent = null)
        {
            RectTransform rect = cmp.transform as RectTransform;
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }
            rect.anchoredPosition3D = Vector3.zero;
            rect.localScale = Vector3.one;
            rect.anchorMax = Vector2.one;
            rect.anchorMin = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMax = new Vector2(-right, -top);
            rect.offsetMin = new Vector2(left, bottom);
            return rect;
        }

        public static Text CreateText(string label, int fontSize = 14, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            Text txt_;
            Text stateText = UIRoot.instance.uiGame.assemblerWindow.stateText;
            txt_ = GameObject.Instantiate<Text>(stateText);
            txt_.gameObject.name = "txt_" + label;
            txt_.text = label;
            txt_.color = new Color(1f, 1f, 1f, 0.4f);
            txt_.alignment = anchor;
            //txt_.supportRichText = false;
            txt_.fontSize = fontSize;
            return txt_;
        }

        public static UIButton CreateButton(string label, float width = 0f, float height = 0f)
        {
            UIDESwarmPanel swarmPanel = UIRoot.instance.uiGame.dysonEditor.controlPanel.hierarchy.swarmPanel;
            UIButton src = swarmPanel.orbitAddButton;
            UIButton btn = GameObject.Instantiate<UIButton>(src);
            btn.gameObject.name = "btn_" + label;
            if (btn.transitions.Length >= 1)
            {
                btn.transitions[0].normalColor = new Color(0.2392f, 0.6f, 0.9f, 0.078f);
            }

            Text btnText = btn.transform.Find("Text").GetComponent<Text>();
            btnText.text = label;
            btnText.fontSize = 17;
            GameObject.Destroy(btn.transform.Find("Text").GetComponent<Localizer>());
            RectTransform btnRect = btn.transform as RectTransform;
            if (width == 0f || height == 0f)
            {
                btnRect.sizeDelta = new Vector2(btnText.preferredWidth + 14f, 24f); //22
            }
            else
            {
                btnRect.sizeDelta = new Vector2(width, height);
            }

            return btn;
        }

        public static void CreateSignalIcon(out UIButton iconButton, out Image iconImage)
        {
            var bg = UIRoot.instance.uiGame.beltWindow.iconTagButton.transform;
            var go = GameObject.Instantiate(bg.gameObject);

            go.name = "signal-button";
            go.SetActive(true);
            RectTransform rect = (RectTransform)go.transform;
            for (int i = rect.childCount - 1; i >= 0; --i)
                GameObject.Destroy(rect.GetChild(i).gameObject);

            iconButton = rect.GetComponent<UIButton>();
            iconButton.tips.tipTitle = "Signal Icon".Translate();
            iconButton.tips.tipText = "Select a signal to display.".Translate();

            iconImage = rect.GetComponent<Image>();
        }

        public static UIComboBox CreateComboBox(UnityAction OnComboBoxIndexChange, float width, float height = 30f)
        {
            // 創建一個下拉表單
            var comboBoxTemple = UIRoot.instance.uiGame.statWindow.productSortBox; //常駐選項
            var go = GameObject.Instantiate(comboBoxTemple);
            go.name = "FactoryLocator comboBox";
            var transform = go.transform.Find("Dropdown List ScrollBox/Mask/Content Panel/");
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                if (transform.GetChild(i).name == "Item Button(Clone)")
                {
                    // Clean up old itemButtons
                    GameObject.Destroy(transform.GetChild(i).gameObject);
                }
            }
            ((RectTransform)go.transform).sizeDelta = new Vector2(width, height);

            var comboBox = go.GetComponentInChildren<UIComboBox>();
            comboBox.onItemIndexChange.RemoveAllListeners();
            //comboBox.m_Text.supportRichText = true;
            //comboBox.m_EmptyItemRes.supportRichText = true;
            //comboBox.m_ListItemRes.GetComponentInChildren<Text>().supportRichText = true;
            //foreach (var button in comboBox.ItemButtons) button.GetComponentInChildren<Text>().supportRichText = true;
            //comboBox.DropDownCount = 20;
            comboBox.itemIndex = 0;
            comboBox.m_Input.text = "";
            comboBox.onItemIndexChange.AddListener(OnComboBoxIndexChange);

            return comboBox;
        }

        public static int RecipeIdUnderMouse()
        {
            if (UIRoot.instance.uiGame.replicator.active)
            {
                UIReplicatorWindow repWin = UIRoot.instance.uiGame.replicator;
                if (repWin.mouseRecipeIndex >= 0)
                {
                    RecipeProto recipeProto = repWin.recipeProtoArray[repWin.mouseRecipeIndex];
                    if (recipeProto != null)
                    {
                        return recipeProto.ID;
                    }
                }
            }
            return 0;
        }

        public static int ItemIdHintUnderMouse()
        {
            List<RaycastResult> targets = new();
            PointerEventData pointer = new(EventSystem.current)
            {
                position = Input.mousePosition
            };
            EventSystem.current.RaycastAll(pointer, targets);
            foreach (RaycastResult target in targets)
            {
                UIButton btn = target.gameObject.GetComponentInParent<UIButton>();
                if (btn?.tips != null && btn.tips.itemId > 0)
                {
                    return btn.tips.itemId;
                }

                UIStorageGrid grid = target.gameObject.GetComponentInParent<UIStorageGrid>();
                if (grid != null)
                {
                    StorageComponent storage = grid.storage;
                    int mouseOnX = grid.mouseOnX;
                    int mouseOnY = grid.mouseOnY;
                    if (mouseOnX >= 0 && mouseOnY >= 0 && storage != null)
                    {
                        int gridIndex = mouseOnX + mouseOnY * grid.colCount;
                        return storage.grids[gridIndex].itemId;
                    }
                    return 0;
                }

                UIPlayerDeliveryPanel deliveryPanel = target.gameObject.GetComponentInParent<UIPlayerDeliveryPanel>();
                if (deliveryPanel != null)
                {
                    if (deliveryPanel.hoverIndexAbsolute >= 0)
                    {
                        return deliveryPanel.deliveryPackage.grids[deliveryPanel.hoverIndexAbsolute].itemId;
                    }
                }

                UIProductEntry productEntry = target.gameObject.GetComponentInParent<UIProductEntry>();
                if (productEntry != null)
                {
                    if (productEntry.productionStatWindow.isProductionTab)
                    {
                        return productEntry.entryData?.itemId ?? 0;
                    }
                    return 0;
                }
            }
            return 0;
        }
    }

}

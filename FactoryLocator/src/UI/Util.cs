using UnityEngine;
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
                btn.transitions[0].target.color = new Color(0.2392f, 0.6f, 0.9f, 0.078f);
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
    }

}

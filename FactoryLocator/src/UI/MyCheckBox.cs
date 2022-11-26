using UnityEngine;
using UnityEngine.UI;

namespace FactoryLocator.UI
{
    // modify from PlanetFinder mod
    // special thanks to hetima

    public class MyCheckBox : MonoBehaviour
    {
        public UIButton uiButton;
        public Image checkImage;
        public RectTransform rectTrans;
        public Text labelText;
        public bool enable;

        public static MyCheckBox CreateCheckBox(bool startingState, string label = "", int fontSize = 17)
        {
            UIBuildMenu buildMenu = UIRoot.instance.uiGame.buildMenu;
            UIButton src = buildMenu.uxFacilityCheck;

            GameObject go = GameObject.Instantiate(src.gameObject);
            go.name = "my-checkbox";
            MyCheckBox cb = go.AddComponent<MyCheckBox>();
            cb.enable = startingState;
            RectTransform rect = go.transform as RectTransform;
            cb.rectTrans = rect;
            rect.anchorMax = Vector2.zero;
            rect.anchorMin = Vector2.zero;
            rect.anchoredPosition3D = new Vector3(0, 0, 0);

            cb.uiButton = go.GetComponent<UIButton>();
            cb.checkImage = go.transform.Find("checked")?.GetComponent<Image>();

            //text
            Transform child = go.transform.Find("text");
            if (child != null)
            {
                GameObject.DestroyImmediate(child.GetComponent<Localizer>());
                cb.labelText = child.GetComponent<Text>();
                cb.labelText.fontSize = fontSize;
                cb.SetLabelText(label);
            }

            cb.uiButton.onClick += cb.OnClick;
            cb.checkImage.enabled = cb.enable;
            return cb;
        }

        public void SetLabelText(string val)
        {
            if (labelText != null)
            {
                labelText.text = val;
                //rectTrans.sizeDelta = new Vector2(checkImage.rectTransform.sizeDelta.x + 4f + labelText.preferredWidth, rectTrans.sizeDelta.y);
            }
        }

        public void OnClick(int obj)
        {
            enable = !enable;
            checkImage.enabled = enable;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryLocator.UI
{
    // modify from PlanetFinder mod
    // special thanks to hetima

    public static class MyWindowCtl
    {
        public static List<ManualBehaviour> _windows = new(2);
        internal static bool _created = false;

        public static T CreateWindow<T>(string name, string title = "") where T : Component
        {
            var srcWin = UIRoot.instance.uiGame.inserterWindow;
            GameObject src = srcWin.gameObject;
            GameObject go = Object.Instantiate(src, srcWin.transform.parent);
            go.name = name;
            go.SetActive(false);
            Object.Destroy(go.GetComponent<UIInserterWindow>());
            ManualBehaviour win = go.AddComponent<T>() as ManualBehaviour;
            //shadow 
            for (int i = 0; i < go.transform.childCount; i++)
            {
                GameObject child = go.transform.GetChild(i).gameObject;
                if (child.name == "panel-bg")
                {
                    Button btn = child.GetComponentInChildren<Button>();
                    //close-btn
                    if (btn != null)
                    {
                        btn.onClick.AddListener(win._Close);
                    }
                }
                else if (child.name != "shadow" && child.name != "panel-bg")
                {
                    GameObject.Destroy(child);
                }
            }

            SetTitle(win, title);

            win._Create();
            win._Init(win.data);
            _windows.Add(win);
            return win as T;
        }

        public static void SetTitle(ManualBehaviour win, string title)
        {
            Text txt = GetTitleText(win);
            if (txt)
            {
                txt.text = title;
            }
        }
        public static Text GetTitleText(ManualBehaviour win)
        {
            return win.gameObject.transform.Find("panel-bg/title-text")?.gameObject.GetComponent<Text>();
        }

        public static RectTransform GetRectTransform(ManualBehaviour win)
        {
            return win.GetComponent<RectTransform>();
        }

        public static void OpenWindow(ManualBehaviour win)
        {
            win._Open();
            win.transform.SetAsLastSibling();
        }

        public static void CloseWindow(ManualBehaviour win)
        {
            win._Close();
        }
    }
}

using FactoryLocator.Compat;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FactoryLocator.UI
{
    // modify from PlanetFinder mod
    // special thanks to hetima

    public class UILocatorWindow : ManualBehaviour
    {
        public RectTransform windowTrans;
        private RectTransform tab1;
        private Text nameText;
        private UIButton[] queryBtns;
        private UIButton clearAllBtn;
        private UIButton iconBtn;
        private Image iconImage;
        private MyCheckBox warningCheckBox;
        private MyCheckBox autoclearCheckBox;

        private PlanetData veiwPlanet;
        private StarData veiwStar;
        private bool initialized;
        private UIButtonTip statusTip = null; // show power network status
        private string statusText = "";
        private bool autoclear_enable = true; // clear previous results when close or make another query

        public static UILocatorWindow CreateWindow()
        {
            UILocatorWindow win = MyWindowCtl.CreateWindow<UILocatorWindow>("FactoryLocator Window", "Factory Locator");
            return win;
        }

        public void OpenWindow()
        {
            MyWindowCtl.OpenWindow(this);
        }

        public override void _OnCreate()
        {
            windowTrans = MyWindowCtl.GetRectTransform(this);
            windowTrans.sizeDelta = new Vector2(260f, 290f);

            CreateUI();
        }

        internal void CreateUI()
        {
            //tabs
            RectTransform base_ = windowTrans;

            float y_ = 54;
            float x_ = 36f;
            int tabIndex_ = 1;

            RectTransform AddTab(string label)
            {
                GameObject tab = new();
                RectTransform tabRect = tab.AddComponent<RectTransform>();
                Util.NormalizeRectWithMargin(tabRect, 52f, 36f, 0f, 0f, windowTrans);
                tab.name = "tab-" + tabIndex_.ToString();
                return tabRect;
            }

            void AddElement(RectTransform rect_, float deltaX, float deltaY)
            {
                x_ += deltaX;
                y_ += deltaY;
                if (rect_ != null)
                {
                    Util.NormalizeRectWithTopLeft(rect_, x_, y_, base_);
                }
            }

            Text CreateTitleText(string label_)
            {
                Text src_ = MyWindowCtl.GetTitleText(this);
                Text txt_ = GameObject.Instantiate<Text>(src_);
                txt_.gameObject.name = "label";
                txt_.text = label_;
                txt_.color = new Color(1f, 1f, 1f, 0.5f);
                (txt_.transform as RectTransform).sizeDelta = new Vector2(txt_.preferredWidth + 40f, 30f);
                return txt_;
            }

            //General tab
            tab1 = AddTab("General");

            base_ = tab1;
            y_ = 0f;
            x_ = 0f;

            // Planet Name / System Name
            nameText = Util.CreateText("Name", 16, TextAnchor.MiddleCenter);
            nameText.rectTransform.sizeDelta = new Vector2(180f, 20f);
            AddElement(nameText.transform as RectTransform, 0f, 0f);

            // Query Buttons
            x_ = 0f;
            queryBtns = new UIButton[6];

            queryBtns[0] = Util.CreateButton("Building", 90f, 24f);
            AddElement(queryBtns[0].transform as RectTransform, 0f, 30f);
            queryBtns[0].onClick += (_) => OnQueryClick(0);

            queryBtns[1] = Util.CreateButton("Vein", 90f, 24f);
            AddElement(queryBtns[1].transform as RectTransform, 98f, 0f);
            queryBtns[1].onClick += (_) => OnQueryClick(1);

            queryBtns[2] = Util.CreateButton("Product", 90f, 24f);
            AddElement(queryBtns[2].transform as RectTransform, -98f, 32f);
            queryBtns[2].onClick += (_) => OnQueryClick(2);

            queryBtns[3] = Util.CreateButton("Warning", 90f, 24f);
            AddElement(queryBtns[3].transform as RectTransform, 98f, 0f);
            queryBtns[3].onClick += (_) => OnQueryClick(3);

            queryBtns[4] = Util.CreateButton("Storage", 90f, 24f);
            AddElement(queryBtns[4].transform as RectTransform, -98f, 32f);
            queryBtns[4].onClick += (_) => OnQueryClick(4);

            queryBtns[5] = Util.CreateButton("Station", 90f, 24f);
            AddElement(queryBtns[5].transform as RectTransform, 98f, 0f);
            queryBtns[5].onClick += (_) => OnQueryClick(5);

            // Sigal Control settings
            x_ = 0f;
            Text text = CreateTitleText("Signal Icon");
            AddElement(text.transform as RectTransform, 0f, 34f);

            Util.CreateSignalIcon(out iconBtn, out iconImage);
            AddElement(iconBtn.transform as RectTransform, 70f, -3f);
            iconBtn.onClick += OnIconBtnClick;

            clearAllBtn = Util.CreateButton("Clear All", 70f, 24f);
            AddElement(clearAllBtn.transform as RectTransform, 48f, 7f);
            clearAllBtn.onClick += (obj) => WarningSystemPatch.ClearAll();

            // Check box
            x_ = 0f;
            warningCheckBox = MyCheckBox.CreateCheckBox(WarningSystemPatch.Enable, "Display All Warning");
            AddElement(warningCheckBox.rectTrans, 0f, 30f);
            warningCheckBox.uiButton.onClick += OnWarningCheckboxClick;

            // Check box
            x_ = 0f;
            autoclearCheckBox = MyCheckBox.CreateCheckBox(autoclear_enable, "Auto Clear Query");
            AddElement(autoclearCheckBox.rectTrans, 0f, 26f);
            autoclearCheckBox.uiButton.onClick += OnAutoClearCheckoxClick;
        }

        public override void _OnOpen()
        {
            if (!initialized)
            {
                OnSignalPickReturn(401);
                initialized = true;
            }
            SetViewingTarget();
            NebulaCompat.OnOpen();
        }

        public override void _OnClose()
        {
            if (statusTip != null)
                Destroy(statusTip.gameObject);
            if (autoclear_enable)
                WarningSystemPatch.ClearAll();
            UIentryCount.OnClose();
            NebulaCompat.OnClose();
        }

        public void SetViewingTarget()
        {
            veiwStar = null;
            veiwPlanet = null;
            if (UIRoot.instance.uiGame.planetDetail.active)
                veiwPlanet = UIRoot.instance.uiGame.planetDetail.planet;
            else if (UIRoot.instance.uiGame.starDetail.active)
                veiwStar = UIRoot.instance.uiGame.starDetail.star;
            else
                veiwPlanet = GameMain.localPlanet;

            if (veiwStar != null)
                nameText.text = veiwStar.displayName + "空格行星系".Translate();
            else
                nameText.text = veiwPlanet?.displayName ?? "外太空".Translate();

            int factoryCount = Plugin.mainLogic.SetFactories(veiwStar, veiwPlanet);
            for (int i = 0; i < queryBtns.Length; i++)
            {
                queryBtns[i].button.enabled = factoryCount > 0;
            }
        }

        public void SetStatusTipText(float[] consumerRatio, int[] consumerCount)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Power Ratio - Consumer Count");
            for (int i = 0; i < consumerCount.Length; i++)
            {
                sb.AppendLine($"{consumerRatio[i],-3:P0} - {consumerCount[i]}");
            }
            statusText = sb.ToString();
            if (statusTip != null)
                statusTip.subTextComp.text = statusText;
        }


        public override void _OnUpdate()
        {
            if (UIRoot.instance.uiGame.starDetail.active)
            {
                if (UIRoot.instance.uiGame.starDetail.star != veiwStar)
                    SetViewingTarget();
            }
            else if (UIRoot.instance.uiGame.planetDetail.active)
            {
                if (UIRoot.instance.uiGame.planetDetail.planet != veiwPlanet)
                    SetViewingTarget();
            }
            else if (GameMain.localPlanet != veiwPlanet)
            {
                SetViewingTarget();
            }

            Transform transform = nameText.transform;
            if (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, Input.mousePosition, UIRoot.instance.overlayCanvas.worldCamera))
            {
                if (statusTip == null)
                {
                    SetViewingTarget();
                    statusTip = UIButtonTip.Create(true, "Power Network Status", statusText, 1, new Vector2(0, -10), 0, transform, "", "");
                }
            }
            else
            {
                if (statusTip != null)
                {
                    Destroy(statusTip.gameObject);
                    statusTip = null;
                }
            }

            NebulaCompat.OnUpdate();
        }

        private void OnQueryClick(int queryType)
        {
            if (autoclear_enable)
                WarningSystemPatch.ClearAll();
            switch (queryType)
            {
                case 0: Plugin.mainLogic.PickBuilding(0); break;
                case 1: Plugin.mainLogic.PickVein(0); break;
                case 2: Plugin.mainLogic.PickAssembler(0); break;
                case 3: Plugin.mainLogic.PickWarning(0); break;
                case 4: Plugin.mainLogic.PickStorage(0); break;
                case 5: Plugin.mainLogic.PickStation(0); break;
            }
        }

        private void OnIconBtnClick(int _)
        {
            UISignalPicker.Popup(new Vector2(-300f, 250f), OnSignalPickReturn);
        }

        private void OnSignalPickReturn(int signalId)
        {
            Sprite sprite = LDB.signals.IconSprite(signalId);
            if (sprite != null)
            {
                Plugin.mainLogic.SignalId = signalId;
                iconImage.sprite = sprite;
            }
        }

        private void OnWarningCheckboxClick(int _)
        {
            WarningSystemPatch.Enable = !WarningSystemPatch.Enable;
        }

        private void OnAutoClearCheckoxClick(int _)
        {
            autoclear_enable = !autoclear_enable;
        }
    }
}

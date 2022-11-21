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

        private PlanetData veiwPlanet;
        private StarData veiwStar;
        private bool initialized;

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
                Util.NormalizeRectWithMargin(tabRect, 54f, 36f, 0f, 0f, windowTrans);
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
            queryBtns[0].onClick += MainLogic.PickBuilding;

            queryBtns[1] = Util.CreateButton("Vein", 90f, 24f);
            AddElement(queryBtns[1].transform as RectTransform, 98f, 0f);
            queryBtns[1].onClick += MainLogic.PickVein;

            queryBtns[2] = Util.CreateButton("Product", 90f, 24f);
            AddElement(queryBtns[2].transform as RectTransform, -98f, 32f);
            queryBtns[2].onClick += MainLogic.PickAssembler;

            queryBtns[3] = Util.CreateButton("Warning", 90f, 24f);
            AddElement(queryBtns[3].transform as RectTransform, 98f, 0f);
            queryBtns[3].onClick += MainLogic.PickWarning;

            queryBtns[4] = Util.CreateButton("Storage", 90f, 24f);
            AddElement(queryBtns[4].transform as RectTransform, -98f, 32f);
            queryBtns[4].onClick += MainLogic.PickStorage;

            queryBtns[5] = Util.CreateButton("Station", 90f, 24f);
            AddElement(queryBtns[5].transform as RectTransform, 98f, 0f);
            queryBtns[5].onClick += MainLogic.PickStation;

            // Sigal Control settings
            x_ = 0f;
            Text text = CreateTitleText("Signal Icon");
            AddElement(text.transform as RectTransform, 0f, 35f);

            Util.CreateSignalIcon(out iconBtn, out iconImage);
            AddElement(iconBtn.transform as RectTransform, 70f, -3f);
            iconBtn.onClick += OnIconBtnClick;

            clearAllBtn = Util.CreateButton("Clear All", 70f, 24f);
            AddElement(clearAllBtn.transform as RectTransform, 48f, 7f);
            clearAllBtn.onClick += (obj) => WarningSystemPatch.ClearAll();

            // Check box
            x_ = 0f;
            warningCheckBox = MyCheckBox.CreateCheckBox(WarningSystemPatch.Enable, "Display All Warning");
            AddElement(warningCheckBox.rectTrans, 0f, 38f);
            warningCheckBox.uiButton.onClick += OnWarningCheckboxClick;
        }

        public override void _OnOpen()
        {
            if (!initialized)
            {
                OnSignalPickReturn(401);
                initialized = true;
            }
            SetViewingTarget();
        }

        public override void _OnClose()
        {
        }

        public void SetViewingTarget()
        {
            veiwPlanet = GameMain.localPlanet;
            veiwStar = null;
            if (UIRoot.instance.uiGame.planetDetail.active)
            {
                veiwPlanet = UIRoot.instance.uiGame.planetDetail.planet;
                nameText.text = veiwPlanet?.displayName ?? "外太空".Translate();
            }
            else if (UIRoot.instance.uiGame.starDetail.active)
            {
                veiwStar = UIRoot.instance.uiGame.starDetail.star;
                veiwPlanet = null;
                nameText.text = veiwStar?.displayName + "空格行星系".Translate();
            }
            else
            {
                nameText.text = veiwPlanet?.displayName ?? "外太空".Translate();
            }

            int factoryCount = MainLogic.SetFactories(veiwStar, veiwPlanet);
            for (int i = 0; i < queryBtns.Length; i++)
            {
                queryBtns[i].button.enabled = factoryCount > 0;
            }
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
                MainLogic.SignalId = signalId;
                iconImage.sprite = sprite;
            }
        }

        private void OnWarningCheckboxClick(int _)
        {
            WarningSystemPatch.Enable = !WarningSystemPatch.Enable;
        }

    }


}

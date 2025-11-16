using FactoryLocator.Compat;
using System.Collections.Generic;
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
        private Text iconText;
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
        private int currentLanguageLCID = 0;

        private static UIComboBox comboBox = null; // subcatagory 子目錄
        private static List<int> networkIds = null;
        private static int queryingType = 0;
        private static int buildingIndex; // All, Power network #(id)
        private static int veinIndex;     // All, Planned, Unplanned
        private static int assemblerIndex;// All, Lack of material, Product overflow
        private static int warningIndex;  // All, Record Mode
        private static int storageIndex;  // All, Demand, Supply
        private static int stationIndex;  // All, Local Station, Interstellar Station, Local demand, Local supply, Remote demand, Remote supply

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

        public override void _OnDestroy()
        {
            if (comboBox != null)
            {
                Destroy(comboBox.gameObject);
                comboBox = null;
                Log.Debug("_OnDestroy");
            }
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

            queryBtns[2] = Util.CreateButton("Recipe", 90f, 24f);
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
            iconText = CreateTitleText("Signal Icon");
            AddElement(iconText.transform as RectTransform, -20f, 34f);

            Util.CreateSignalIcon(out iconBtn, out iconImage);
            AddElement(iconBtn.transform as RectTransform, 90f, -3f);
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
                // Load the user config
                OnSignalPickReturn(Plugin.config.SignalIconId.Value);
                if (!Plugin.config.AutoClearQuery.Value)
                    autoclearCheckBox.OnClick(0);
                initialized = true;
            }
            SetText();
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
            comboBox?.gameObject.SetActive(false);

            // Save the user config
            Plugin.config.SignalIconId.Value = Plugin.mainLogic.SignalId;
            Plugin.config.AutoClearQuery.Value = autoclear_enable;
        }

        public void SetText()
        {
            if (currentLanguageLCID != Localization.CurrentLanguageLCID)
            {
                currentLanguageLCID = Localization.CurrentLanguageLCID;
                queryBtns[0].transform.Find("Text").GetComponent<Text>().text = "Building".Translate();
                queryBtns[1].transform.Find("Text").GetComponent<Text>().text = "Vein".Translate();
                queryBtns[2].transform.Find("Text").GetComponent<Text>().text = "Recipe".Translate();
                queryBtns[3].transform.Find("Text").GetComponent<Text>().text = "Warning".Translate();
                queryBtns[4].transform.Find("Text").GetComponent<Text>().text = "Storage".Translate();
                queryBtns[5].transform.Find("Text").GetComponent<Text>().text = "Station".Translate();
                iconText.text = "Signal Icon".Translate();
                clearAllBtn.transform.Find("Text").GetComponent<Text>().text = "Clear All".Translate();
                warningCheckBox.labelText.text = "Display All Warning".Translate();
                autoclearCheckBox.labelText.text = "Auto Clear Query".Translate();
            }
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
            sb.AppendLine("Satisfaction - Consumer Count".Translate());
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
                    statusTip = UIButtonTip.Create(true, "Power Network Status".Translate(), statusText, 1, new Vector2(0, -10), 0, transform, "", "");
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
            SetSubcategory(queryType);
            switch (queryType)
            {
                case 0: Plugin.mainLogic.PickBuilding(buildingIndex); break;
                case 1: Plugin.mainLogic.PickVein(veinIndex); break;
                case 2: Plugin.mainLogic.PickAssembler(assemblerIndex); break;
                case 3: Plugin.mainLogic.PickWarning(warningIndex); break;
                case 4: Plugin.mainLogic.PickStorage(storageIndex); break;
                case 5: Plugin.mainLogic.PickStation(stationIndex); break;
            }
        }

        public void SetPowerNetworkDropdownList(List<int> ids)
        {
            networkIds = ids;
        }

        public void SetSubcategory(int queryType)
        {
            if (comboBox == null)
            {
                comboBox = Util.CreateComboBox(OnComboBoxIndexChange, 200f);                
            }
            if (queryType == 2)
            {
                Util.NormalizeRectWithTopLeft(comboBox, 153, 7, UIRoot.instance.uiGame.recipePicker.transform);
            }
            else if (queryType == 3)
            {
                Util.NormalizeRectWithTopLeft(comboBox, 153, 7, UIRoot.instance.uiGame.signalPicker.transform);
            }
            else
            {
                Util.NormalizeRectWithTopLeft(comboBox, 153, 7, UIRoot.instance.uiGame.itemPicker.transform);
            }

            queryingType = queryType;
            comboBox.Items.Clear();
            comboBox.ItemsData.Clear();
            switch (queryType)
            {
                case 0: // PickBuilding
                    buildingIndex = 0;
                    if (networkIds == null) // DropList is not available (multiple factories)
                    {
                        comboBox.Items.Add("All".Translate());
                        comboBox.ItemsData.Add(0);
                        comboBox.itemIndex = 0;
                        comboBox.gameObject.SetActive(false);
                        return;
                    }
                    comboBox.Items.Add("All".Translate() + $" ({networkIds.Count})");
                    comboBox.ItemsData.Add(0);
                    for (int i = 0; i < networkIds.Count; i++)
                    {
                        comboBox.Items.Add("电网号".Translate() + networkIds[i]);
                        comboBox.ItemsData.Add(i + 1);
                    }
                    comboBox.itemIndex = 0;
                    break;

                case 1: // PickVein
                    comboBox.Items.Add("All".Translate());
                    comboBox.ItemsData.Add(0);
                    comboBox.Items.Add("显示正在采集".Translate());
                    comboBox.ItemsData.Add(1);
                    comboBox.Items.Add("显示尚未采集".Translate());
                    comboBox.ItemsData.Add(2);
                    comboBox.itemIndex = veinIndex;
                    break;

                case 2: // PickAssembler
                    comboBox.Items.Add("All".Translate());
                    comboBox.ItemsData.Add(0);
                    comboBox.Items.Add("额外产出".Translate());
                    comboBox.ItemsData.Add(1);
                    comboBox.Items.Add("加速生产".Translate());
                    comboBox.ItemsData.Add(2);
                    comboBox.Items.Add("缺少原材料".Translate());
                    comboBox.ItemsData.Add(3);
                    comboBox.Items.Add("产物堆积".Translate());
                    comboBox.ItemsData.Add(4);
                    comboBox.itemIndex = assemblerIndex;
                    break;

                case 3: // PickWarning
                    comboBox.Items.Add("All".Translate());
                    comboBox.ItemsData.Add(0);
                    comboBox.Items.Add("Recording Mode".Translate());
                    comboBox.ItemsData.Add(1);
                    comboBox.itemIndex = warningIndex;
                    break;

                case 4: // PickStorage
                    comboBox.Items.Add("All".Translate());
                    comboBox.ItemsData.Add(0);
                    comboBox.Items.Add("需求".Translate());
                    comboBox.ItemsData.Add(1);
                    comboBox.Items.Add("供应".Translate());
                    comboBox.ItemsData.Add(2);
                    comboBox.itemIndex = storageIndex;
                    break;

                case 5: // PickStation
                    comboBox.Items.Add("All".Translate());
                    comboBox.ItemsData.Add(0);
                    comboBox.Items.Add("本地站点号".Translate());
                    comboBox.ItemsData.Add(1);
                    comboBox.Items.Add("星际站点号".Translate());
                    comboBox.ItemsData.Add(2);
                    comboBox.Items.Add("本地需求".Translate());
                    comboBox.ItemsData.Add(3);
                    comboBox.Items.Add("本地供应".Translate());
                    comboBox.ItemsData.Add(4);
                    comboBox.Items.Add("星际需求".Translate());
                    comboBox.ItemsData.Add(5);
                    comboBox.Items.Add("星际供应".Translate());
                    comboBox.ItemsData.Add(6);
                    comboBox.itemIndex = stationIndex;
                    break;

                default:
                    comboBox.gameObject.SetActive(false);
                    return;
            }
            comboBox.gameObject.SetActive(true);
        }

        private void OnComboBoxIndexChange()
        {
            bool isPickingItem = UIRoot.instance.uiGame.itemPicker.active;
            bool isPickingRecipe = UIRoot.instance.uiGame.recipePicker.active;
            bool isPickingSignal = UIRoot.instance.uiGame.signalPicker.active;

            switch (queryingType)
            {
                case 0: // PickBuilding
                    buildingIndex = comboBox.itemIndex;
                    if (isPickingItem)
                    {
                        Plugin.mainLogic.OnBuildingPickReturn(null);
                        UIItemPicker.Close();
                        Plugin.mainLogic.PickBuilding(buildingIndex);
                    }
                    return;

                case 1: // PickVein
                    veinIndex = comboBox.itemIndex;
                    if (isPickingItem)
                    {
                        Plugin.mainLogic.OnVeinPickReturn(null);
                        UIItemPicker.Close();
                        Plugin.mainLogic.PickVein(veinIndex);
                    }
                    return;

                case 2: // PickAssembler
                    assemblerIndex = comboBox.itemIndex;
                    if (isPickingRecipe)
                    {
                        Plugin.mainLogic.OnAssemblerPickReturn(null);
                        UIRecipePicker.Close();
                        Plugin.mainLogic.PickAssembler(assemblerIndex);
                    }
                    return;

                case 3: // PickWarning
                    warningIndex = comboBox.itemIndex;
                    if (isPickingSignal)
                    {
                        Plugin.mainLogic.OnWarningPickReturn(0);
                        UISignalPicker.Close();
                        Plugin.mainLogic.PickWarning(warningIndex);
                    }
                    return;

                case 4: // PickStorage
                    storageIndex = comboBox.itemIndex;
                    if (isPickingItem)
                    {
                        Plugin.mainLogic.OnStoragePickReturn(null);
                        UIItemPicker.Close();
                        Plugin.mainLogic.PickStorage(storageIndex);
                    }
                    return;

                case 5: // PickStation
                    stationIndex = comboBox.itemIndex;
                    if (isPickingItem)
                    {
                        Plugin.mainLogic.OnStationPickReturn(null);
                        UIItemPicker.Close();
                        Plugin.mainLogic.PickStation(stationIndex);
                    }
                    return;
            }
        }

        private void OnIconBtnClick(int _)
        {
            UISignalPicker.Popup(new Vector2(-300f, 250f), OnSignalPickReturn);
        }

        private void OnSignalPickReturn(int signalId)
        {
            Sprite sprite = LDB.signals.IconSprite(signalId);
            if (sprite != null && signalId != WarningData.DASHBOARD_SIGNALID) // 518 有特殊的dashboard邏輯,因此不可使用
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

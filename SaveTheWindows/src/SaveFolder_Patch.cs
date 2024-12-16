using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SaveTheWindows
{
    public class SaveFolder_Patch
    {
        static string subfolder = "";
        static GameObject group;
        static UIComboBox subfolderComboBox;

        [HarmonyPostfix, HarmonyPatch(typeof(GameConfig), "gameSaveFolder", MethodType.Getter)]
        public static void GetGameSaveSubfolder(ref string __result)
        {
            if (string.IsNullOrEmpty(subfolder)) return;
            __result = __result + "/" + subfolder + "/";
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIMainMenu), nameof(UIMainMenu._OnOpen))] //主選單開啟時, 初始化子資料夾選單UI
        public static void Init()
        {
            if (group != null) return;

            try
            {
                GameObject go;
                GameObject comboBoxTemple = UIRoot.instance.optionWindow.resolutionComp.transform.gameObject;

                // 創建一個群組包含所有mod的物件
                group = new GameObject("Subfolder_Group");

                // 創建一個下拉表單, 選子資料夾
                go = GameObject.Instantiate(comboBoxTemple, group.transform, false);
                go.name = "Subfolder ComboBox";
                go.transform.localPosition = new Vector3(0, 0, 0);
                var transform = go.transform.Find("Dropdown List ScrollBox/Mask/Content Panel/");
                for (var i = transform.childCount - 1; i >= 0; i--)
                {
                    if (transform.GetChild(i).name == "Item Button(Clone)")
                    {
                        // Clean up old itemButtons
                        GameObject.Destroy(transform.GetChild(i).gameObject);
                    }
                }

                subfolderComboBox = go.GetComponentInChildren<UIComboBox>();
                subfolderComboBox.onItemIndexChange.RemoveAllListeners();
                subfolderComboBox.DropDownCount = 25;
                subfolderComboBox.itemIndex = 0;

                RefreshComboBoxList();
                subfolderComboBox.onItemIndexChange.AddListener(OnComboBoxIndexChange);
                SetGameSaveSubfolder(Plugin.SubFolder.Value);
                Plugin.Log.LogDebug("UI Subfolder init");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Error when creating subfolder UI!");
                Plugin.Log.LogWarning(ex);
            }            
        }

        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(UILoadGameWindow), nameof(UILoadGameWindow._OnOpen))]
        static void SetSubfolderGroupPosition(UILoadGameWindow __instance)
        {
            if (group == null) Init();

            try
            {
                group.transform.SetParent(__instance.showButton.transform.parent);
                group.transform.localPosition = __instance.showButton.transform.localPosition + new Vector3(0, -45, 0);
                group.transform.localScale = Vector3.one;
                RefreshComboBoxList();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }

        [HarmonyPrefix, HarmonyPriority(Priority.High)]
        [HarmonyPatch(typeof(UISaveGameWindow), nameof(UISaveGameWindow._OnOpen))]
        static void SetSubfolderGroupPosition(UISaveGameWindow __instance)
        {
            if (group == null) Init();

            try
            {
                group.transform.SetParent(__instance.showButton.transform.parent);
                group.transform.localPosition = __instance.showButton.transform.localPosition + new Vector3(0, -45, 0);
                group.transform.localScale = Vector3.one;
                RefreshComboBoxList();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }

        public static void OnDestroy()
        {
            GameObject.Destroy(group);
        }

        public static void SetGameSaveSubfolder(string folderName)
        {
            if (subfolder != folderName)
            {
                Plugin.Log.LogDebug("SetGameSaveSubfolder " + folderName);
                Plugin.SubFolder.Value = subfolder = folderName;
                if (UIRoot.instance != null)
                {
                    if (UIRoot.instance.loadGameWindow.active) UIRoot.instance.loadGameWindow.RefreshList();
                    if (UIRoot.instance.saveGameWindow.active) UIRoot.instance.saveGameWindow.RefreshList();
                }
            }
            if (!string.IsNullOrEmpty(subfolder) && !Directory.Exists(GameConfig.gameSaveFolder))
            {
                Plugin.Log.LogInfo("CreateDirectory " + GameConfig.gameSaveFolder);
                Directory.CreateDirectory(GameConfig.gameSaveFolder);
            }
        }

        public static void OnComboBoxIndexChange()
        {
            if (subfolderComboBox.itemIndex >= 0 && subfolderComboBox.itemIndex < subfolderComboBox.Items.Count)
            {
                var name = subfolderComboBox.Items[subfolderComboBox.itemIndex];
                SetGameSaveSubfolder(name);
            }
            else
            {
                SetGameSaveSubfolder("");
            }
        }

        static void RefreshComboBoxList()
        {
            subfolderComboBox.Items.Clear();
            subfolderComboBox.ItemsData.Clear();

            var subfolderExist = false;
            var dirs = new List<string>(Directory.EnumerateDirectories(GameConfig.gameSavePath));
            dirs.Insert(0, ""); // Root: empty name
            for (var index = 0; index < dirs.Count; index++)
            {
                var dir = dirs[index];
                var folderName = Path.GetFileName(dir);
                subfolderComboBox.Items.Add(folderName);
                subfolderComboBox.ItemsData.Add(index);
                if (folderName == subfolder)
                {
                    subfolderComboBox.itemIndex = index;
                    subfolderExist = true;
                }
            }
            if (!subfolderExist)
            {
                subfolderComboBox.itemIndex = 0; // reset subfolder if the folder can't be found
            }
        }
    }
}

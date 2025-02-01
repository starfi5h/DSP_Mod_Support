using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StatsUITweaks
{
    public static class Utils
    {
        public static bool OrderByName = true;
        public static int DropDownCount = 15;
        public static string PlanetPrefix = "ㅤ";
        public static string PlanetPostfix = "";
        public static string SystemPrefix = "<color=yellow>";
        public static string SystemPostfix = "</color>";
        public static KeyCode HotkeyListUp = KeyCode.PageUp;
        public static KeyCode HotkeyListDown = KeyCode.PageDown;

        public static void DetermineAstroBoxIndex(UIComboBox astroBox)
        {
            int itemIndex = astroBox.itemIndex;
            if (Input.GetKeyDown(HotkeyListUp))
            {
                if (VFInput.control) // 上一個星系
                {
                    do
                    {
                        itemIndex = itemIndex > 0 ? itemIndex - 1 : astroBox.ItemsData.Count - 1;
                        int astroFilter = astroBox.ItemsData[itemIndex];
                        if ((astroFilter % 100 == 0 && astroFilter > 0) || astroFilter == -1)
                            break;
                    } while (itemIndex != astroBox.itemIndex);
                    astroBox.itemIndex = itemIndex;
                }
                else // 上一個列表
                {
                    astroBox.itemIndex = itemIndex > 0 ? itemIndex - 1 : astroBox.ItemsData.Count - 1;
                }
            }
            else if (Input.GetKeyDown(HotkeyListDown))
            {
                if (VFInput.control)
                {
                    do
                    {
                        itemIndex = (itemIndex + 1) % astroBox.ItemsData.Count;
                        int astroFilter = astroBox.ItemsData[itemIndex];
                        if ((astroFilter % 100 == 0 && astroFilter > 0) || astroFilter == -1)
                            break;
                    } while (itemIndex != astroBox.itemIndex);
                    astroBox.itemIndex = itemIndex;
                }
                else
                {
                    astroBox.itemIndex = (itemIndex + 1) % astroBox.ItemsData.Count;
                }
            }
        }

        public static void EnableRichText(UIComboBox uIComboBox)
        {
            uIComboBox.m_Text.supportRichText = true;
            uIComboBox.m_EmptyItemRes.supportRichText = true;
            uIComboBox.m_ListItemRes.GetComponentInChildren<Text>().supportRichText = true;
            foreach (var button in uIComboBox.ItemButtons)
                button.GetComponentInChildren<Text>().supportRichText = true;
            uIComboBox.DropDownCount = DropDownCount;
        }

        static readonly Dictionary<int, int> astroIndex = new();
        static readonly List<ValueTuple<string, int>> systemList = new();
        static readonly List<string> newItems = new();
        static readonly List<int> newItemData = new();

        public static void UpdateAstroBox(UIComboBox astroBox, int startIndex, int localStarAstroId, string searchStr = "")
        {
            if (astroBox.Items.Count <= startIndex) return;

            // Planet filter
            astroIndex.Clear();
            systemList.Clear();
            newItems.Clear();
            newItemData.Clear();
            bool localStarExist = false;

            if (startIndex + 1 < astroBox.Items.Count && astroBox.Items[startIndex] == "统计当前星系".Translate())
            {
                startIndex++;
                localStarExist = true;
            }

            for (int i = startIndex; i < astroBox.Items.Count; i++)
            {
                int astroId = astroBox.ItemsData[i];
                if (astroId % 100 == 0)
                {
                    if (astroIndex.ContainsKey(astroId))
                    {
                        Plugin.Log.LogDebug($"[{astroId}] => {astroBox.Items[i]}");
                        systemList[astroIndex[astroId]] = (astroBox.Items[i], astroId); //以後來的名稱覆寫
                    }
                    else
                    {
                        astroIndex[astroId] = systemList.Count;
                        systemList.Add((astroBox.Items[i], astroBox.ItemsData[i]));
                    }
                }
            }
            if (OrderByName) // 以星系名稱排序
                systemList.Sort();

            foreach (var tuple in systemList)
            {
                int starId = tuple.Item2 / 100;
                for (int i = startIndex; i < astroBox.Items.Count; i++)
                {
                    int astroId = astroBox.ItemsData[i];
                    if (astroId / 100 == starId)
                    {
                        string itemName = astroBox.Items[i];

                        if (astroId % 100 != 0)
                        {
                            if (!itemName.StartsWith(PlanetPrefix) || !itemName.EndsWith(PlanetPostfix))
                                itemName = PlanetPrefix + itemName + PlanetPostfix;
                        }
                        else
                        {
                            if (!itemName.StartsWith(SystemPrefix) || !itemName.EndsWith(SystemPostfix))
                                itemName = SystemPrefix + itemName + SystemPostfix;
                        }

                        newItems.Add(itemName);
                        newItemData.Add(astroBox.ItemsData[i]);
                    }
                }
            }
            astroBox.Items.RemoveRange(startIndex, astroBox.Items.Count - startIndex);
            astroBox.ItemsData.RemoveRange(startIndex, astroBox.ItemsData.Count - startIndex);

            if (localStarAstroId != 0 && !localStarExist)
            {
                astroBox.Items.Add("统计当前星系".Translate());
                astroBox.ItemsData.Add(localStarAstroId);
            }

            astroBox.Items.AddRange(newItems);
            astroBox.ItemsData.AddRange(newItemData);

            if (!string.IsNullOrEmpty(searchStr))
            {
                for (int i = astroBox.Items.Count - 1; i >= startIndex; i--)
                {
                    int nameStart = astroBox.ItemsData[i] % 100 == 0 ? SystemPrefix.Length : PlanetPrefix.Length;
                    int nameEnd = astroBox.Items[i].Length - (astroBox.ItemsData[i] % 100 == 0 ? SystemPostfix.Length : PlanetPostfix.Length);
                    int result = astroBox.Items[i].IndexOf(searchStr, nameStart, StringComparison.OrdinalIgnoreCase);
                    if (result == -1 || (result + searchStr.Length) > nameEnd)
                    {
                        astroBox.Items.RemoveAt(i);
                        astroBox.ItemsData.RemoveAt(i);
                    }
                }
            }
        }
    }
}

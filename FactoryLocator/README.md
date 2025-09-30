# Factory Locator

The Factory Locator mod enhances your gameplay by helping you efficiently locate and manage buildings in the game. With this mod, you can quickly find the positions of specified buildings and highlight them with warning icons.   

## Features

![search local planet](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo3.gif) 

### Hotkeys and Controls
![keybinds](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/keybind.jpg)  
**Open Mod Window**: Press `Ctrl + F` to open the mod window (default hotkey, configurable in game keybinds settings).  
**Mouse Over Filter**: If an item/recipe is found under your mouse pointer when opening the window, it will be used as a filter (e.g. Inventory, Storage, Replicator, Statistics Panel and more places where item icon is displayed).  

### Main Window
![status tip](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo3.jpg)  
**Power Status**: Hover over planet name to show power status of all networks.  
**Signal Icon**: Click on the icon to set the icon for search results. (signal-518 is not available)  
**Display All Warning**: Toggle to show or hide all warnings.  
**Auto Clear Query**: When disabled, the created signals will remain until Clear button is clicked.  

### Picker Window
![subcategory](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/subcategory.png)  
**Draggable Window**: The item/recipe picker window can be moved around by dragging the window background.  
**Subcategory**: The dropdown can select the subcategory for extra search criteria:  
- Building: Power network groups  
- Vein: Collection Planned, Not Yet Planned  
- Recipe: Extra products, Production speedup, Lack of material, Product overflow  
- Warning: All, Recording Mode  
- Stroage: Distributor Demand, Distributor Supply  
- Station: Local station, Interstellar station, Local demand, Local supply, Remote demand, Remote supply  
  
**Icon and Entity Count**: Icons show all available options, with numbers indicating the count of buildings or items.  
**Proliferator Mode Indicators**: The color of the numbers in the recipe selection window represents the proliferator mode for assemblers:
- Light Blue: Extra output
- Light Yellow: Production speedup
- Light Red: Mixed

**Extra signal in Warning picker**
- 404: Blueprint error
- 600: Spray coaster has no spray
- 601: Spray coaster has no input belt (spray)  
- 602: Spray coaster has no output belt (cargo)  


### Search Planets
![search remote planets](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo2.gif)  
**Local Planet Search**: By default, the mod searches for buildings on your current planet.  
**Remote Planet Search**: When in starmap view, the mod adapts the search based on your selection:  
- Selecting a planet will search for factories on that planet.
- Selecting a star will search all planets within that star system.

### Warning Icons Extention
![iteration entities wtih camera](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo4.gif)  
- **Left Click**: Displays the planet locations of the warnings. Click elsewhere to close the details.
- **Ctrl + Left Click**: Loops the camera through all relevant entities on the local planet.
- **Right Click**: Removes the selected query group from the list.

## Installation
Via [r2modman](https://thunderstore.io/c/dyson-sphere-program/p/ebkr/r2modman/), or manual download the file and put `FactoryLocator.dll` in `BepInEx/plugins` folder.  
Requrie [CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI/) as dependency.  

## Mod compatibility
This mod is compat with the following mods:
- [GenesisBook](https://dsp.thunderstore.io/package/HiddenCirno/GenesisBook/) v3.1.0  
- [BetterWarningIcons](https://dsp.thunderstore.io/package/Raptor/BetterWarningIcons/) v0.0.5  

### Nebula multiplayer mod compat  
The mod don't have to install on both host and client. Some behaviors will be different though.  
- Host : The temporary guiding warning will sync with clients.  
- Client : Only loaded planets are searchable. When the mod window is opened, the warning icon will stop syncing with host temporarily.   

Install [NebulaCompatibilityAssist](https://dsp.thunderstore.io/package/starfi5h/NebulaCompatibilityAssist/) to get full functionality for clients.  

### Acknowledgements
All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
Special thanks for Semar's LongArm mod for inspiration, hetima's mods for UI design and Raptor for mod idea.  

----

# 工厂定位

![互动窗口](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/icon_c.jpg)  
查找特定建筑物的位置，并用警报图标指示它们。  
鼠标悬停星球名称可以显示所有电网的电力供应状态。  
显示所有警报提示: 关闭时会隐藏所有警报。  
自动清除搜寻结果: 关闭窗口时搜寻的信标将会保留, 直到按下清空按钮。  

![键位设置](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/keybind_c.jpg)  
打开窗口的默认热键是Ctrl + F。可以在"游戏设置-键位-打开FactoryLocator窗口"更改。  
打开面板时自动将鼠标的指向物品或配方设为筛选条件。  
默认情况下，该模组将在本地星球上搜索建筑物。  
在星图视图中，如果选中的星球上有工厂，该模组将更改搜索星球。选择恒星时，它将搜索星系中的所有星球。  

![选择窗口](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/subcategory_c.png)  
在选择窗口中，将显示所有可能的搜索选项。数字显示符合条件的建筑物数量，或者所有物流塔及储物仓中的物品数量。  
选择窗口可以拖曳移动，下拉列表可以选择子类别：   
- 建筑：电网组  
- 矿脉：已规划采集、未规划采集  
- 配方：额外产出、加速生产、缺少原材料、产物堆积  
- 警报：持续记录模式  
- 储物仓：(物流配送器)需求、((物流配送器))供应  
- 物流塔：本地站点、星际站点、本地需求、本地供应、星际需求、星际供应  

配方选择窗口中的数字颜色代表机器的增产策略：  
- 淡蓝: 额外产出
- 淡黄: 生产加速
- 淡红: 混合
  
在警报-全部子类别中, 蓝图标红建筑可以用红X(404)信号找寻  
警报-全部可以查询当下射线接收站状态  
- 黄电(信号503): 射线持续接受率不足99.9%  
- 燃料不足(信号508): 缺乏透镜  
  
此外，数字标记0~2代表喷涂机状态:  
- 0(信号600): 无增产剂  
- 1(信号601): 缺失增产剂输入  
- 2(信号602): 缺失增产剂输出  


![中键循序镜头](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo4.gif)
- 左键单击信标详细信息图标可以显示警报位置。单击其他区域以关闭它。  
- Ctrl+左键单击信标可以将镜头移动到本地星球上信标的位置。  
- 右键单击搜索信标可以移除该群组。  

## 安装
管理器: 安装[r2modman](https://thunderstore.io/c/dyson-sphere-program/p/ebkr/r2modman/)后, 在Online线上列表找到此mod下载安装即可, 点Start modded启动游戏  
手动下载: BepInEx框架的安装请参考网上的教学, 建议用5.4.17稳定版本  
从此页面下载mod最新版本, 将`FactoryLocator.dll`放入`BepInEx/plugins`文件夹。  
请注意这个mod还有三个前置mod需要安装  
- [LDBTool](https://dsp.thunderstore.io/package/xiaoye97/LDBTool/)
- [DSPModSave](https://dsp.thunderstore.io/package/CommonAPI/DSPModSave/)
- [CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI/)

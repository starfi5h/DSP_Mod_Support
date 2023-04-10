# Factory Locator

Find the positions of specified buildings and indicate them with warning icons.  
  
![search local planet](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo3.gif)  
The default hotkey to open the window is Ctrl + F. Keybind can be configured in game control settings.  
In picker window, it will show all possible search options among all buildings. The yellow number shows how many buildings qualify for the condition, or how many items are inside all station/box storage.  
Display All Warning: Toggle to show/hide all warnings.  
Auto Clear Query: When disable, the created signals will stay until Clear button is clicked.  
  
![search remote planets](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo2.gif)  
By default, the mod will search for buildings on the local planet.  
When in starmap view, the mod will change the searching planet if the selecting planet has factory on it. When selecting a star, it will search all planets in the star system.  
Click on a warning detail icon to show where the warning locate. Click on other areas to close it.  
  
![status tip](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo3.jpg)  
Mouse over name to show power status of all networks.  
  
Special thanks for Semar's LongArm mod for inspiration, hetima's mods for UI design and Raptor for mod idea.  

## Nebula multiplayer mod compat
The mod don't have to install on both host and client. Some behaviors will be different though.  
- Host : The temporary guiding warning will sync with clients.  
- Client : Only loaded planets are searchable. When the mod window is opened, the warning icon will stop syncing with host temporarily.   
Install [NebulaCompatibilityAssist](https://dsp.thunderstore.io/package/starfi5h/NebulaCompatibilityAssist/) to get full functionality for clients.  

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `FactoryLocator.dll` in `BepInEx/plugins` folder.  
Requrie [CommonAPI](https://dsp.thunderstore.io/package/CommonAPI/CommonAPI/) as dependency.  

----

![互动窗口](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/icon_c.jpg)  
查找特定建筑物的位置，并用警报图标指示它们。

![键位设置](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/keybind_c.jpg)  
打开窗口的默认热键是Ctrl + F。可以在"游戏设置-键位-打开FactoryLocator窗口"更改。  
在选择窗口中，将显示所有可能的搜索选项。黄色数字显示符合条件的建筑物数量，或者所有物流塔及储物仓中的物品数量。  
显示所有警报提示: 关闭时会隐藏所有警报。  
自动清除搜寻结果: 关闭时搜寻的信标将会保留, 直到按下清空按钮。  

默认情况下，该模组将在本地星球上搜索建筑物。  
在星图视图中，如果选中的星球上有工厂，该模组将更改搜索星球。选择恒星时，它将搜索星系中的所有星球。  
鼠标悬停星球可以显示所有电网的电力供应状态。  

单击警报详细信息图标可以显示警报位置。单击其他区域以关闭它。  

# SaveTheWindows

Add a drop-down list in save/load window to let the player switch between subfolders in `Dyson Sphere Program\Save` folder.  
The player can use different subfolder to categorize their saves.  
The selected subfolder will be use as directory to save/load saves from, and it will be remembered in the mod config file.  
The top item with empty name is the original save folder.  
在保存/加载窗口添加一个下拉列表，让玩家在`Dyson Sphere Program\Save`中的子文件夹之间切换加载/保存位置。  
玩家可以使用不同的子文件夹对他们的存档进行分类。  
最上面的空名项目是原始保存文件夹。  
![subfolder](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/SaveTheWindows/img/subfolder.png)  

Fix a quality of life issue with the UI windows where the window positions are not persisted between sessions.  
Instead of resetting to the default locations, they will appear where they were last when they were closed.  
用户界面窗口在关闭游戏后会保存，重开游戏时会回复到上次关闭时的位置。  

Allow UI windows go outside of screen (partly) so they don't block the whole screen.  
允许用户界面窗口稍微超出边框，这样它们就不会遮挡整个屏幕。  


## Installation 安装

Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `SaveTheWindows.dll` in `BepInEx/plugins` folder.  
通过管理器[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)，或者手动下载文件并将`SaveTheWindows.dll`放入`BepInEx/plugins`文件夹。  

## Configuration 配置文件
Run the game one time to generate `BepInEx\config\starfi5h.plugin.SaveTheWindows.cfg` file.  
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。    
To reset saved window position, delete the config file.  
删除配置文件以重置保存的窗口位置  

[Config]  
`Enable Save Window Position`: 启用窗口位置保存  
`Enable Drag Window Offset`: 允许窗口部分超出边框   
`Enable Save Subfolder`: 允许存档子文件夹功能  
`Save Subfolder`: 当前存档子文件夹名称(空字串=原位置)  

## ChangeLogs
- v1.1.0: Add save subfolder function. (DSP 0.10.31.24710)
- v1.0.0: Initial released. (DSP 0.10.30.23430)  

## Acknowledgements

All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
Original mod [SaveTheWindows](https://thunderstore.io/c/dyson-sphere-program/p/Therzok/SaveTheWindows/) by [Therzok](https://thunderstore.io/c/dyson-sphere-program/p/Therzok/)  
<a href="https://www.flaticon.com/free-icons/push-pin" title="push pin icons">Push Pin icons created by Yogi Aprelliyanto - Flaticon</a>  
# SaveTheWindows

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

## ChangeLogs
- v1.0.0: Initial released. (DSP 0.10.30.23430)  

## Acknowledgements

All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
Original mod [SaveTheWindows](https://thunderstore.io/c/dyson-sphere-program/p/Therzok/SaveTheWindows/) by [Therzok](https://thunderstore.io/c/dyson-sphere-program/p/Therzok/)  
<a href="https://www.flaticon.com/free-icons/push-pin" title="push pin icons">Push Pin icons created by Yogi Aprelliyanto - Flaticon</a>  
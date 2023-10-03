# StatsUITweaks

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/demo1.jpg)  

Adds QoL features to the statistics panel.  
- Drag the slider to change time interval.  
- Sort the astro list by system names.  
- Type in the input field to filter the astro list.  
- PageUp/PageDown to go to the next item on the list. Ctrl + PageUp/PageDown to go to the next system.  
- Left-click the navigate button to navigate to the select planet. Right-click to show it in starmap.  


## Configuration
Run the game one time to generate `BepInEx\config\starfi5h.plugin.StatsUITweaks.cfg` file.  
Check [Unity - KeyCode](https://docs.unity3d.com/2018.4/Documentation/ScriptReference/KeyCode.html) for available hotkey names.  

| | Default | Description |
| :----- | :------ | :---------- |
| StatsUITweaks | | |
| `OrderByName`   | true     | Order astro list by system name. |
| `HotkeyListUp`  | PageUp   | Move to previous item in astro list |
| `HotkeyListUp`  | PageDown | Move to next item in astro list |

----

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/demo1.jpg)  

增加统计面板UI的便利性

- 可控时距  
拉动滑杆可以将时间范围缩小至原本的100%~5%。可能会有些微误差

- 以星系名称排序列表  
原本游戏是以到达的先后顺序排列下拉清单列表中的星系。启用后会改以星系名称来排序

- 过滤星球名称  
在输入框中输入字串, 将会过滤列表中的星系和星球

- 导航按钮  
左键点击会导航至选取的星球。右键点击会开启星图模式显示该星球位置。

- 热键切换  
PageUp/PageDown可以切换至列表的上/下一个项目。压住Ctrl时, 会切换至上/下一个星系。



## 配置   
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。    
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.StatsUITweaks` -> Edit Config  
热键名称请参考Unity手册的[KeyCode](https://docs.unity3d.com/2018.4/Documentation/ScriptReference/KeyCode.html)  
 
| | 默认值 | 说明 | 
| :----- | :------ | :---------- |
| StatsUITweaks | | |
| `OrderByName`   | true     | 以星系名称排序列表 |
| `HotkeyListUp`  | PageUp   | 切换至列表中上一个项目的热键 |
| `HotkeyListUp`  | PageDown | 切换至列表中下一个项目的热键 |

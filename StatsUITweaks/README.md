# StatsUITweaks

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/demo1.jpg)  

Adds QoL features to the statistics panel.  
- Drag the slider to change time interval.  
- Sort the astro list by system names.  
- Type in the input field to filter the astro list.  
- Custom prefix/postfix for planet/system names in the astro list. Use [Unity rich text](https://docs.unity3d.com/2018.4/Documentation/Manual/StyledText.html) to change text.  
- PageUp/PageDown to go to the next item on the list. Ctrl + PageUp/PageDown to go to the next system.  
- Left-click the navigate button to navigate to the select planet. Right-click to show it in starmap. Show astroId and index of factory in the button tip.  


## Configuration
Run the game one time to generate `BepInEx\config\starfi5h.plugin.StatsUITweaks.cfg` file.  
Check [Unity - KeyCode](https://docs.unity3d.com/2018.4/Documentation/ScriptReference/KeyCode.html) for available hotkey names.  

| | Default | Description |
| :----- | :------ | :---------- |
| AstroBox | | |
| `OrderByName`     | true           | Order the list by system name |
| `DropDownCount`   | 15             | Number of items shown in drop-down list |
| `SystemPrefix`    | `<color=yellow>` | Prefix string of star system in the list |
| `SystemPostfix`   | `</color>`       | Postfix string of star system in the list |
| `PlanetPrefix`    | `ㅤ`             | Prefix string of planet in the list |
| `PlanetPostfix`   |                | Postfix string of planet in the list |
| StatsUITweaks | | |
| `SignificantDigits`| 0     | Significant figures of production/consumption (Default=0) |
| `TimeSliderSlice`| 20      | The number of divisions of the time range slider |
| `ListWidthOffeset`| 70     | Increase width of the list |
| `HotkeyListUp`  | PageUp   | Move to previous item in astro list |
| `HotkeyListUp`  | PageDown | Move to next item in astro list |
| `NumericPlanetNo` | false  | Convert planet no. from Roman numerals to numbers |
| PerformancePanel | | |
| `FoldButton`      | true    | Add a button to fold pie chart |

----

![demo2](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/demo2.jpg)  

增加统计面板UI的便利性

- 可控时距  
拉动滑杆可以将时间范围缩小至原本的100%~5%。可能会有些微误差

- 以星系名称排序列表  
原本游戏是以到达的先后顺序排列下拉清单列表中的星系。启用后会改以星系名称来排序

- 过滤星球名称  
在输入框中输入字串, 将会过滤列表中的星系和星球

- 自定义星球/星系名称前缀后缀  
使用者可以用[Unity富文本](https://docs.unity3d.com/2018.4/Documentation/Manual/StyledText.html)自由配置字体样式

- 导航按钮  
左键点击会导航至选取的星球。右键点击会开启星图模式显示该星球位置。按钮提示astroId和factory.index(idx)方便除错  

- 热键切换  
PageUp/PageDown可以切换至列表的上/下一个项目。压住Ctrl时, 会切换至上/下一个星系。

## 配置   
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。    
管理器安装: 左边选项Config editor -> 找到`starfi5h.plugin.StatsUITweaks` -> Edit Config  
热键名称请参考Unity手册的[KeyCode](https://docs.unity3d.com/2018.4/Documentation/ScriptReference/KeyCode.html)  
 
| | 默认值 | 说明 | 
| :----- | :------ | :---------- |
| AstroBox | | |
| `OrderByName`     | true           | 以星系名称排序列表 |
| `DropDownCount`   | 15             | 下拉列表显示的个数 |
| `SystemPrefix`    | `<color=yellow>` | 星系名称前缀 |
| `SystemPostfix`   | `</color>`       | 星系名称后缀 |
| `PlanetPrefix`    | `ㅤ`             | 星球名称前缀 |
| `PlanetPostfix`   |                | 星球名称后缀 |
| StatsUITweaks | | |
| `SignificantDigits`| 0       | 产量有效位数(默认=0) |
| `TimeSliderSlice` | 20       | 时间范围滑杆的分割数 |
| `ListWidthOffeset`| 70       | 增加列表栏位的宽度 |
| `HotkeyListUp`    | PageUp   | 切换至列表中上一个项目的热键 |
| `HotkeyListUp`    | PageDown | 切换至列表中下一个项目的热键 |
| `NumericPlanetNo` | false    | 将星球序号从罗马数字转为十进位数字 |
| PerformancePanel | | |
| `FoldButton`      | true    | 在性能面板加入一个折叠饼图的按钮 |

![demo3](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/demo3.jpg)  

## ChangeLogs

\- v1.4.5: Add `SignificantDigits` config option (DSP 0.10.29.22015)  
\- v1.4.4: Add compat to Bottleneck's displayPerSecond. Support time slider in kill count (DSP 0.10.29.21950)  
\- v1.4.3: Fix right-click on navi button. Display astorId and factoryIdx on its tip (DSP 0.10.29.21904)  
\- v1.4.2: Fix star system duplicate in the filter with Bottleneck local system label (DSP 0.10.28.21172)  
\- v1.4.1: Fix error in OnLocateButtonRightClick  
\- v1.4.0: Add `DropDownCount` config option. Fix compat with Bottleneck 1.0.16  
\- v1.3.1: Support DSP 0.10.28.20779 (no changes in functions)  
\- v1.3.0: Add `FoldButton` config option.  
\- v1.2.1: Fix astro list in outersapce.  
\- v1.2.0: Add `TimeSliderSlice` config options. Fix error when opening dyson tab when there is only one system.  
\- v1.1.0: Add `ListWidthOffeset`, `NumericPlanetNo`, prefixes & postfixes config options. Stretch histogram.  
\- v1.0.0: Initial released. (DSP 0.9.27.15466)  

# StatsUITweaks


Adds QoL features to the statistics panel.  
![mainWindow](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/mainWindow.png)
- Toggle to display production/consumption rate in per second.
- Toggle to extend the histogram to 2.5x width.
- Slider to change the time interval.
- Set production/consumption rate and reference rate text font size larger.
- Preserve product filter when switching time range or closing the window.

![astroBox](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/astroBox.jpg)  
- Add local planetary system.  
- Sort the astro list by system names.  
- Type in the input field to filter the astro list.  
- Custom prefix/postfix for planet/system names in the astro list. Use [Unity rich text](https://docs.unity3d.com/2018.4/Documentation/Manual/StyledText.html) to change text.  
- PageUp/PageDown to go to the next item on the list. Ctrl + PageUp/PageDown to go to the next system.  
- Left-click the navigate button to navigate to the select planet. Right-click to show it in starmap. Show astroId and index of factory in the button tip.  

![controlPanel](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/controlPanel.png)  
- Support astro list rich text and hotkey in Logistics Control Panel (I) too.  
- Prevent windows from closing when opening dashboard.
- Prevent control panel from closing when pressing E key.

Others
- Add more layout reference height options (500~800) so it can go lower than 900 to scale the overall UI larger.  

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
| `HotkeyListUp`    | PageUp   | Move to previous item in astro list |
| `HotkeyListDown`    | PageDown | Move to next item in astro list |
| Dashboard | | |
| `TabSizeSwitch`   | true   | When hovering on an item, Tab to switch size, Ctrl+Tab to swtich title. |
| MainWindow | | |
| `TimeSliderSlice` | 20     | The number of divisions of the time range slider |
| `ListWidthOffeset`| 70     | Increase width of the list |
| `RateFontSize`    | 26     | Adjust the font size of production rate. (Vanilla=18) |
| `RefRateTweak`    | false  | The reference rate (maximum theoretical value) is always applied proliferator settings, regardless the material |
| `RefRateMinerLimit`| 14400 | Set reference rate max limit for pump and oil extractor |
| Other | | |
| `FoldButton`      | true   | Add a button in perforamnce test panel to fold pie chart |
| `NumericPlanetNo` | false  | Convert planet no. from Roman numerals to numbers |
| `HideLitterNotification` | false    | Don't show trash notification (still visable in Z mode) |
| `HideSoilNotification` | false    | Don't show soil notification |

----

增加统计面板UI的便利性

\- 打开仪表板时不再关闭现有视窗  
\- 在物流总控面板开启时，按E键不再关闭面板  

- Display /s  
将生产/消耗速率以秒显示

- Extend Graph  
延伸直条图的宽度至2.5倍

- 字体大小  
将生产/消耗速率和参考速率的字体放大(18->26)

- 保存过滤条件  
在切换时间范围或关闭视窗时保存产物的过滤条件，再开启时不会重置

- 统计当前星系  
在下拉清单列表中加入本地星系项目

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

- 更多UI布局选项  
添加更多布局参考高度选项（500~800），使它可以被设置低于900来放大整体UI。  


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
| `HotkeyListUp`    | PageUp   | 切换至列表中上一个项目的热键 |
| `HotkeyListDown`  | PageDown | 切换至列表中下一个项目的热键 |
| Dashboard | | |
| `TabSizeSwitch`   | true     | 鼠标悬停在某个统计项上时，Tab键切换尺寸，Ctrl+Tab键切换标题 |
| MainWindow | | |
| `ListWidthOffeset`| 70       | 增加列表栏位的宽度 |
| `TimeSliderSlice` | 20       | 时间范围滑杆的分割数 |
| `RateFontSize`    | 26       | 生产速率和参考速率的字体大小(原版=18) |
| `RefRateTweak`    | false    | 参考速率(最大理论值)一律套用增产剂设定，无论原料是否已喷涂 |
| `RefRateMinerLimit`| 14400   | 為抽水機和油井的参考速率設定上限 |
| Other | | |
| `FoldButton`      | true     | 在性能面板加入一个折叠饼图的按钮 |
| `NumericPlanetNo` | false    | 将星球序号从罗马数字转为十进位数字 |
| `HideLitterNotification` | false    | 隐藏平常模式的垃圾提示(Z模式仍可见) |
| `HideSoilNotification` | false    | 隐藏沙土数量变动的提示 |

![demo3](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/demo3.jpg)  

![extendPowerDetail](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/StatsUITweaks/img/extendPowerDetail.png)  
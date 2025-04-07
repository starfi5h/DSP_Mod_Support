# Rate Monitor 速率仪

Rate Monitor is a Dyson Sphere Program mod inspired by [Rate Calculator](https://mods.factorio.com/mod/RateCalculator) from Factorio. It lets you select machines in an area (using Alt + X) to calculate their input/output rates, track working ratios, and discover why some machines may not be running at full capacity.  
本模组受到Factorio的[Rate Calculator](https://mods.factorio.com/mod/RateCalculator)启发，通过按下 Alt + X 区域框选建筑，即可计算产出和消耗速率，监控工作效率，并快速找出机器停工的原因。  

## Key Features 核心功能

- Area Selection (Alt + X)
  - Drag a box around production or mining facilities, matrix labs, ray receivers, and more.
  - Once selected, a GUI window shows production and consumption rates, plus a real-time working ratio.
  - If you’re in space, pressing Alt + X will reopen the last selection’s data.

- Production/Consumption Overview
  - Quickly see net rates (production minus consumption) for raw materials, intermediate goods, and final products.
  - Shows both theoretical maximum rates and adjusted rates based on actual machine efficiency.  
  - Identify bottlenecks: find which resources are in deficit and why certain machines aren’t operating at 100%.

- Recipe & Machine Details
  - Click an item icon to “focus” on it, filtering the data to show only recipes that produce or consume that item.
  - Click a building icon to expand its recipe details, revealing each machine’s status and reasons for underperformance.
  - Toggle 'Detail' to view the recorded idle reasons and navigate to the machine location via the button.

- 区域框选（Alt + X）
  - 框选制造设备、采矿设备、矩阵研究站、射线接收站、戴森球设施等。  
  - 框选后会弹出一个数据窗口，展示实时产出和消耗速率，以及各类机器的工作比率。  
  - 如果你在太空中，按 Alt + X 会重新打开上次的框选结果。  
  - 可以在操作面板框选本地/外地星球的全部建筑

- 产出/消耗总览
  - 快速查看原料、中间产物、最终产品的净产率（产出 - 消耗）。
  - 同时展示理论最大产能与实际运行效率下的估算产能。  
  - 识别瓶颈：找出资源不足的项目，以及哪些机器没有满负荷运行。  

- 配方与机器详情
  - 点击物品图标可以“聚焦”该物品，仅显示与其相关的生产和消耗条目。
  - 点击建筑图标可展开配方详细信息，查看具体机器的状态及未满负荷的原因。
  - 点击“检视详情“查看记录的待机原因，并通过按钮快速导航至机器位置。


## Area Selection Tool 区域框选工具

![area selection](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/RateMonitor/img/select.gif)  

Area select machines with Alt + X. Support machine types:  
1. production facility (assembler, smlter, fractionator, etc)
2. mining facility (mining machine, water pump, oil extractor)
3. matrix lab
4. energy exchanger, ray receiver
5. ejector, silo  

Alt + X 区域框选建筑。支持的建筑种类有:  
1. 生产建筑(制造台、熔炉、分馏塔等)  
2. 采集建筑(矿机、大矿机、抽水站等)  
3. 研究站(生产模式 & 科研模式)
4. 生产物品的电力设施(能量枢纽、射线接收站)
5. 戴森球设施(电磁弹射器、垂直发射井)  

![expand entry](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/RateMonitor/img/expand.gif)  

After selection done, a gui window will appear. When in space, hotkey Alt + X will open the window with the last selection.  
Click on the item icon to focus, click on the building icon to expand that recipe entry.  
选择完成后，将出现一个 GUI 窗口。当不在星球上时，热键Alt + X会用上次的框选打开窗口。  
单击项目图标以聚焦，过滤消耗/生产该物品的项目。可以检视总消耗量和生产量，以及净机器数目。  
单击建筑图标以展开该配方条目，可以检视效率不足的原因。  

## Interface Overview

When opened, the Rate Monitor window displays multiple panels showing your factory’s statistics.  
![windowE](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/RateMonitor/img/RM1-E.png)

<details>
<summary>Click to expand content</summary>


### Header

- Rate Unit: The time unit for the data. Common settings include “per minute”(1/min) or “per second”(60/min)  
- Proliferator Level: Shows the current level of proliferator effects (if applicable) on the production lines being tracked.  
- Entity Count: Displays how many machines or entities (assemblers, smelters, chemical plants, etc.) are included in this monitor's calculation.  
- Operate: Open the operation panel to do actions.  
- Config: Open the settings panel to change the config file.  

### Material / Intermediate / Product Panel

- Material: Lists raw or input materials (e.g., iron ore, copper ore, coal).  
- Intermediate: Lists partially processed goods (e.g., steel, plastic).  
- Product: Lists final products (e.g., energetic graphite, refined oil).  
    
The left numbers are theoretical net rate (maximum production - maximum consumption), assuming all machines run at full speed.  
The right numbers are estimated net rate, calculated by (machine working ratio * theoretical net rate).  
Clicking on the item icon can focus on the item, filter to only show the recipes related to it.  

### Recipe Info Panel 

The top will show the cost of all listed entries. Click on 'Expanded' toggle to filter the only expanded entries.  
Click on the middle building icon button of the entry to expand the recipe content.  
The entry format is:  
```
Production net rate (working ratio %) = Total machine count (wokring machine count) x Net rate per machine  
```
### Expanded Entry

The first line show the recipe name. If it is using proliferator, it will append the proliferator strategy.  
If there are some machines not fully 100% working, it will show the status summary in the second line.  
Click on the 'Detail' toggle to see all the records. Click on those button will move Icarus to the target machine.  

### Item Focus Mode

When clicking on item icon, that item will be yellow highlighted. Click again to unfocus.  
The entry content will display the Net Machine Count, formula is:  
```
All total net rate / Net rate per machine  
```
The value indicates if the machine-recipe groups have a net surplus/deficit. Negative values mean the machines are under-supplied.  

</details>

## 介面说明


![windowC](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/RateMonitor/img/RM1-C.png)

<details>
<summary>点击展开内容</summary>

### 标题区域

- **速率单位**：用于统计数据的时间单位。常见为“每分钟（1/min）”或“每秒（60/min）”  
- **增产剂等级**：显示当前监控的生产线中，增产剂的使用等级（如有）  
- **实体数量**：计算中包含的机器数量（如组装机、熔炉、化工厂等）  
- **操作**：打开操作面板，执行相关操作  
- **配置**：打开设置面板，修改配置文件  

### 原料 / 中间产物 / 成品 面板

- **原料**：列出原始输入资源（如铁矿、铜矿、煤等）  
- **中间产物**：列出在内部同时是产物及原料的物品（如钢材、塑料）  
- **成品**：列出最终产出的产品（如高能石墨、精炼油）  

数值说明：  
- **左侧数值**：理论净速率 = 最大产量 - 最大消耗（假设所有机器满负荷运行）  
- **右侧数值**：实际估算净速率 = 理论净速率 × 机器工作比率  

点击物品图标可以聚焦该物品，并自动筛选只显示与之相关的配方  

### 配方信息面板

- 顶部会显示所有项目的总资源消耗  
- 点击“已展开”开关可切换是否只显示已展开的配方条目  
- 点击条目中间的建筑图标按钮可以展开该配方的详细信息  

配方格式如下：  
```净产出速率（工作比率 %） = 总机器数（正在工作的数量） × 每台机器的净产出速率```

### 展开条目说明

- **第一行**：显示配方名称；如使用了增产剂，将显示所使用的增产策略  
- **第二行**：如果部分机器未满负荷工作，会显示运行状态概览  
- 点击“详细”按钮可查看所有相关记录，点击按钮可指引伊卡洛斯前往目标机器位置  

### 物品聚焦模式

- 点击物品图标可高亮该物品（黄色），再次点击可取消聚焦  
- 在聚焦状态下，配方条目会额外显示“净机器数”，计算公式如下：  
```总净产出速率 / 每台机器的净产出速率```
- 此数值反映该机器是否盈余或短缺。负值代表为达到产物平衡，机器需要补充的数目    

</details>


## Installation 安装

Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manually download the file and put `RateMonitor.dll` in `BepInEx/plugins` folder.  
通过管理器[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)，或者手动下载文件并将`RateMonitor.dll`放入`BepInEx/plugins`文件夹。  

## Configuration 配置文件
Run the game one time to generate `BepInEx\config\starfi5h.plugin.RateMonitor.cfg` file.  
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。    

<details>
<summary>Click to expand content</summary>

```
## Plugin GUID: starfi5h.plugin.RateMonitor

[General]

## Level of proliferator [1,2,4]. Auto dectect: -1
## 增产效果等级[1,2,4] 自动侦测:-1
# Setting type: Int32
# Default value: -1
# Acceptable value range: From -1 to 10
Proliferator Level = -1

## The theoretical max rate always apply proliferator, regardless the material.
## 计算理论上限时是否强制套用增产设定(否=依照当下原料决定)
# Setting type: Boolean
# Default value: false
Force Proliferator = false

## The theoretical max rate always apply gravity lens.
## 计算射线接收站时总是套用透镜(否=依照当下决定)
# Setting type: Boolean
# Default value: false
Force Gravity Lens in Ray Receiver = false

[KeyBinds]

## Hotkey to toggle area selection tool
## 启用框选工具的热键
# Setting type: KeyboardShortcut
# Default value: X + LeftAlt
SelectToolKey = X + LeftAlt

[UI]

## Timescale unit (x item per minute)
## 速率单位(每分钟x个物品)
# Setting type: Int32
# Default value: 1
# Acceptable value range: From 1 to 14400
Rate Unit = 1

## Show Real-time Monitoring Rate
## 显示即时监控速率
# Setting type: Boolean
# Default value: true
Show Realtime Rate = true

## Show Real-time Working Rate in percentage
## 以百分比显示工作效率
# Setting type: Boolean
# Default value: true
Show Working Rate in percentage = true
```

</details>


## Acknowledgements

Specail thanks to following mods and their authors:  
[Rate Calculator](https://mods.factorio.com/mod/RateCalculator): Main mod idea inspiration.  
[DSPCalculator](https://thunderstore.io/c/dyson-sphere-program/p/jinxOAO/DSPCalculator/): Rate calculation.  

All trademarks, copyright, and resources related to Dyson Sphere Project itself, remain the property of Gamera Game and Youthcat Studio as applicable according to the license agreement distributed with Dyson Sphere Program.  
# UITweaks

A BepInEx mod for Dyson Sphere Program that provides quality-of-life improvements to the game's UI.  
戴森球计划的BepInEx模组,为游戏UI提供多项实用改进。

## Features

### 1. Tech Tree Enhancements 科技树UI增强
![Material Indicators](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/refs/heads/dev/UITweaks/img/MaterialIndicators.png)  
- **Research Material Indicators**: Displays small icons of required items on each tech node for quick reference
- **Skip Metadata Requirement**: Removes the metadata prerequisite check, allowing you to research techs without needing to collect metadata first
- **Navigate to Prerequisites**: Adds a locate button when viewing a tech that has missing prerequisites, allowing quick navigation to the required tech

- **科研材料图标**: 在科技节点上显示所需物品的小图标,方便快速查看
- **跳过元数据需求**: 移除元数据前置要求检查,无需收集元数据即可研究蓝图科技
- **定位前置科技**: 查看有隐式前置的科技时,会显示定位按钮,可快速导航到所需的前置科技

### 2. Station Storage Shortcuts 物流站快捷操作
Adds mouse button shortcuts to station storage UI for quickly switching storage modes:
- **Right Click**: Switch between Demand and Supply
- **Middle Click**: Switch between None and Demand

为物流站存储UI的运输逻辑按钮添加鼠标快捷键,快速切换模式:  
- **右键点击**:在'需求'和'供应'之间切换
- **中键点击**:在'仓储'和'需求'之间切换

### 3. Custom UI Layout Height 自定义UI布局高度
Override the game's UI layout height settings:
- Configurable via the mod's config file
- Lower values create larger UI layouts
- Range: 480-900 (default: 900)

覆盖游戏的UI布局高度设置:
- 可通过配置文件自定义
- 较低的数值会创建更大的UI布局
- 范围:480-900(默认:900)

## Installation 安装

Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)/[GaleModManager](https://thunderstore.io/c/dyson-sphere-program/p/Kesomannen/GaleModManager/), or manually download the file and put `UITweaks.dll` in `BepInEx/plugins` folder.  
通过管理器[r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/)/[GaleModManager](https://thunderstore.io/c/dyson-sphere-program/p/Kesomannen/GaleModManager/)，或者手动下载文件并将`UITweaks.dll`放入`BepInEx/plugins`文件夹。  

## Configuration 配置文件

Run the game one time to generate `BepInEx\config\starfi5h.plugin.UITweaks.cfg` file.  
If you're using mod manager, you can find the file in its Config editor.  
配置文件(BepInEx\config\starfi5h.plugin.UITweaks.cfg)需要先运行过游戏一次才会出现。  
如果是用管理器, 可以在左边点开Config editor页面找到该文件

### Available Settings

```ini
[UI Layout]
# Enable custom UI layout height override
Enable Overwrite = false

# Custom UI layout height value (lower = larger UI)
# Range: 480-900
UI Layout Height = 900
```

To use custom UI layout height:
1. Set `Enable Overwrite = true`
2. Adjust `UI Layout Height` to your preferred value
3. Go to the in-game settings page, click 'Apply' button to apply the new settings
  
When used together with BepInEx.ConfigurationManager v17.0, you can set the value in real-time.  
![uilayout](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/refs/heads/dev/UITweaks/img/uilayout.png)  


# Error Analyzer

A diagnostic tool for Dyson Sphere Program that enhances error reporting and helps identify possible problematic mods.


## Features
- **Close button**: The top-left X button can close the error window and continue playing. (Note: the autosave is still disabled).  
- **Copy button**: The middle button can copy the error message to the clipboard in style format.
- **Inspect button**: Left click to navigate to the erroring machine, right click to toggle the DEBUG tracking mode.
- **Enhance Message**: Show the possible candidates of the mods that may cause the error, and related patches on the stack trace.

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/ErrorAnalyzer/img/demo.png)   

## Usage

### Understanding Error Messages
When an error occurs, you'll see an enhanced error message with:
- Game version and loaded mod plugins count
- Possible mod candidates that might be causing the issue
- Error details with a cleaned-up stack trace
- Details of any related Harmony patches

### Troubleshooting Mods
The error dialog will show "possible candidates" for problematic mods. This isn't always conclusive, but provides a good starting point for troubleshooting. Try:
- Disabling the suggested mods (if it is not save-dependent)
- Updating mods to their latest versions
- Checking for known conflicts between the identified mods

### Navigating to Problem Entities
If an entity (factory machines, belt, etc.) is identified as part of the running error:
- Click on the [Inspect button] to enter DEBUG mode (the button will turn green)
- When the error occur again, it will capture the location and move mecha to that entity in the game world
- If the entity is not on the same planet, a navigation line will guide to the planet
- Some types of entity can only be tracked in single-threaded mode

### Sharing Error Information
To report errors to mod developers:
- When an error occurs, click the [Copy button] in the dialog
- Hold Shift while clicking to include the full mod list if need
- Paste the copied text in Discord, GitHub issues, or mod forums

## Example
Below is an example of the error message that can be copied to your clipboard:
```ini
Error report: Game version 0.10.32.25783 with 10 mods used.
possible candidates: [ErrorAnalyzer1.3.0][ErrorTester1.0.0]
IndexOutOfRangeException: Index was outside the bounds of the array.
StationComponent.DetermineDispatch (float shipSailSpeed, float shipWarpSpeed, int shipCarries, int priorityIndex, StationComponent[] gStationPool, FactoryProductionStat[] factoryStatPool, PlanetFactory[] factories, GalaxyData galaxy, TrafficStatistics tstat); (IL_0DB2)
GalacticTransport.GameTick (long time); (IL_00FD)
GameData.GameTick (long time); (IL_01DC)
GameMain.FixedUpdate (); (IL_017B)
[== Mods on stack trace ==]: [ErrorAnalyzer]
void ErrorAnalyzer.Testing.Plugin::DetermineDispatch_Postfix(); DetermineDispatch(Postfix)
```
Explanation:
- The first line shows the current game version and how many mod plugins loaded.
- The second line shows the mod plugins that may be involved ([ErrorAnalyzer1.3.0][ErrorTester1.0.0]).
- The error was caused by a IndexOutOfRangeException in `StationComponent.DetermineDispatch` function.  
- "Mods on stack trace" shows the assembly name that appear on the stack trace.
- Under it shows the harmony patches on the stack trace. In this case, `ErrorAnalyzer.Testing.Plugin::DetermineDispatch_Postfix()` is harmony postfix to `StationComponent.DetermineDispatch`, which is likely the cause of the error.

Notes:
- The namespace of the function typically corresponds to the mod name and the assembly name.
- The functions listed in the patch section may not directly relate to the error’s root cause. Generally, functions higher in the stack are more likely to be the cause, but other patches not listed in the stack trace may also be involved.
- Removing the erroring machine may or may not solve the issues, so backup the save before dismantling. Sometimes you'll need more powerful purge tools e.g. `Re-intialize planet` in [UXAssist](https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/). If they still not work then you'll have to roll back to the previous normal save.  
- When the following conditions are matched, the mods will be determinated as possible candidates:  
1. Mods that appear directly in the stack trace (judge by namespace)  
2. The first patched method on the stack trace, which is not the common patched functions (VFPreload, GameMain, ThreadManager.ProcessFrame)  
3. Mods that modify the same type as the first function in the stack trace, if the first function is not mod or patched function  


## Config
The config file is `BepInEx\config\aaa.dsp.plugin.ErrorAnalyzer.cfg`, which can be found in mod manager's Config editor.  
```
## Settings file was created by plugin ErrorAnalyzer
## Plugin GUID: aaa.dsp.plugin.ErrorAnalyzer

[DEBUG Mode]

## Enable DEBUG mode to track the entity when starting up the game
# Setting type: Boolean
# Default value: false
Enable = false

[Message]

## Show all mod patches on the stacktrace (By default it will not list GameData.Gametick() and below methods)
# Setting type: Boolean
# Default value: false
Show All Patches = false

## Dump Harmony patches of all mods when the game load in BepInEx\LogOutput.log
# Setting type: Boolean
# Default value: false
Dump All Patches = false
```
When the DEBUG tracking mode is on, the navigate button will change color. In this mode, it will track the entity that throws errors and move the player to the location when it is on the same planet. Errors in the following methods will be handled too in this mode to let the player dismantle the corrupted building:  
`CargoTraffic.SetBeltState`, `CargoContainer.RemoveCargo`, `CargoTraffic.PickupBeltItems`  

----

# 错误分析

错误分析器mod一款适用于戴森球计划的诊断工具，可增强错误报告并帮助识别可能有问题的模组。  

## 功能
- **关闭按钮(Close)**：左上角的 X 按钮可以关闭错误窗口并继续游戏（注意：自动保存仍然处于禁用状态）。  
- **复制按钮(Copy)**：中间按钮可以将格式化的错误信息复制到剪贴板。  
- **导航按钮(Navi)**：左键点击可导航至出错的机器，右键点击可切换 DEBUG 追踪模式。  
- **增强信息列表**：显示可能导致错误的模组候选项，以及堆栈跟踪(stack trace)中的相关补丁。  
![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/ErrorAnalyzer/img/demo.png)  

## 使用方法

### 理解错误信息
当发生错误时，您将看到增强的错误信息，包含：
- 游戏版本和已加载的模组插件数量
- 可能导致问题的模组候选项
- 经过整理的堆栈跟踪的错误详情
- 相关Harmony Patch的详细信息

### 排查模组问题
错误信息将显示问题模组的"可能候选项"。这并非总是确定的，但为故障排除提供了良好的起点。尝试：
- 禁用建议的模组（如果存档不依赖于它）
- 将模组更新至最新版本
- 检查已识别模组之间的已知冲突

### 导航至问题机器
如果某个实体（工厂机器、传送带等）被识别为运行错误的一部分：
- 点击[导航按钮]进入 DEBUG 模式（按钮将变为绿色）
- 当错误再次发生时，它将捕获位置并在游戏世界中将机甲移动到该实体处
- 如果该实体不在同一行星上，将有一条导航线引导至该行星
- 某些类型的实体只能在单线程模式下追踪

### 分享错误信息
向模组开发者报告错误：
- 发生错误时，点击对话框中的[复制按钮]
- 如果有需要，按住 Shift 键的同时点击可包含完整模组列表
- 将复制的文本粘贴到 Discord、GitHub 问题或模组论坛中

## 示例

以下是可以复制到剪贴板的错误信息示例：
```ini
Error report: Game version 0.10.32.25783 with 10 mods used.
possible candidates: [ErrorAnalyzer1.3.0][ErrorTester1.0.0]
IndexOutOfRangeException: Index was outside the bounds of the array.
StationComponent.DetermineDispatch (float shipSailSpeed, float shipWarpSpeed, int shipCarries, int priorityIndex, StationComponent[] gStationPool, FactoryProductionStat[] factoryStatPool, PlanetFactory[] factories, GalaxyData galaxy, TrafficStatistics tstat); (IL_0DB2)
GalacticTransport.GameTick (long time); (IL_00FD)
GameData.GameTick (long time); (IL_01DC)
GameMain.FixedUpdate (); (IL_017B)
[== Mods on stack trace ==]: [ErrorAnalyzer]
void ErrorAnalyzer.Testing.Plugin::DetermineDispatch_Postfix(); DetermineDispatch(Postfix)
```
解释：
- 第一行显示当前游戏版本和已加载的模组插件数量。
- 第二行显示可能涉及的模组插件([ErrorAnalyzer1.3.0][ErrorTester1.0.0])。
- 错误由 `StationComponent.DetermineDispatch` 函数中的 IndexOutOfRangeException 引起。
- "堆栈跟踪上的模组"显示堆栈跟踪中出现的程序集名称。
- 下面显示堆栈跟踪上的Harmony补丁。在这种情况下，`ErrorAnalyzer.Testing.Plugin::DetermineDispatch_Postfix()` 是 `StationComponent.DetermineDispatch` 的后缀补丁(HarmonyPostfix)，这很可能是错误的原因。

**注意**
- 通常，函数的命名空间通常对应模组名称，因此可以根据函数名称来判断哪个模组可能引发了错误。
- 补丁部分列出的函数可能与错误的根本原因没有直接关系。通常，堆栈中较高位置的函数更可能是原因，但堆栈跟踪中未列出的其他补丁也可能参与其中。
- 移除出错的机器可能会也可能不会解决问题，因此在拆除前请备份存档。有时您需要更强大的清除工具，例如 [UXAssist](https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/)  中的"重新初始化行星"功能。如果这些方法仍然不起作用，那么您将不得不回退到之前的正常存档。
- 当满足以下条件时，模组将被确定为可能的候选项：
1. 直接出现在堆栈跟踪中的模组（根据命名空间判断）
2. 堆栈跟踪上的第一个被补丁修改的方法，且不是常见的被修补函数（VFPreload、GameMain、ThreadManager.ProcessFrame）
3. 如果堆栈跟踪中的第一个函数不是模组函数或被补丁修改的函数，则修改与第一个函数相同类型的模组将加入
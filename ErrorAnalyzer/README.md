# Error Analyzer

The Error Analyzer mod enhances the error reporting in game by adding useful features to help you identify and resolve issues caused by mods.

## Features
- **Close button**: The top-left X button can close the error window.
- **Copy button**: The middle button can copy the error message to the clipboard in style format.
- **Navi button**: Left click to navigate to the erroring machine, right click to toggle the DEBUG tracking mode.
- **Mod Function Listing**: Displays a list of mod functions on the call stack within the stack trace, helping to identify which mods might be causing errors.  
![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/ErrorAnalyzer/img/demo2.png)   

## Example
Below is an example of the error message that can be copied to your clipboard:
```ini
An error has occurred! Game version 0.10.30.23350
5 Mods used: [Script Engine10.0] [IlLine1.0.0] [UnityExplorer4.8.2] [DemoPlugin1.0.0] [ErrorAnalyzer1.1.0] 
Thread Error Exception!!! Thread idx:0 transport Factory idx: Positive Loop System.NullReferenceException: Object reference not set to an instance of an object
  at StationComponent.InternalTickRemote (PlanetFactory factory, System.Int32 timeGene, System.Single shipSailSpeed, System.Single shipWarpSpeed, System.Int32 shipCarries, StationComponent[] gStationPool, AstroData[] astroPoses, VectorLF3& relativePos, UnityEngine.Quaternion& relativeRot, System.Boolean starmap, System.Int32[] consumeRegister) [0x02f66] ;IL_2F66 
  at PlanetTransport.GameTick (System.Int64 time, System.Boolean isActive, System.Boolean isMultithreadMode) [0x00213] ;IL_0213 
  at WorkerThreadExecutor.TransportPartExecute () [0x000e4] ;IL_00E4 
[== Mod patches on the stack ==]
void DemoPlugin.Plugin::InternalTickRemote_Postfix(); InternalTickRemote(Postfix)
```
Explanation:
- The game version is 0.10.30.23350, with 5 mods installed.  
- The error was caused by a NullReferenceException in `StationComponent.InternalTickRemote` function.
- Under the "[== Mod patches on the stack ==]" section, you can identify that the error was likely caused by the function `DemoPlugin.Plugin::InternalTickRemote_Postfix()` in the DemoPlugin mod.

Notes:
- The namespace of the function typically corresponds to the mod name, making it easier to identify which mod caused the error.
- The functions listed in the patch section may not directly relate to the error’s root cause. Generally, functions higher in the stack are more likely to be the cause, but other patches not listed in the stack trace may also be involved.
- Removing the erroring machine may or may not solve the issues, so backup the save before dismantling. Sometimes you'll need more powerful purge tools e.g. `Re-intialize planet` in [UXAssist](https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/). If they still not work then you'll have to roll back to the previous normal save.  


## Config
The config file is `BepInEx\config\aaa.dsp.plugin.ErrorAnalyzer.cfg`, which can be found in mod manager's Config editor.  
```
## Settings file was created by plugin ErrorAnalyzer v1.2.2
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


## ChangeLogs
- v1.2.2: Add config. Suppress `CargoTraffic.PickupBeltItems` error in debug tracking mode.  
- v1.2.1: Suppress `CargoTraffic.SetBeltState` and `CargoContainer.RemoveCargo` error in debug tracking mode to dismantle the belts.  
- v1.2.0: Add close button and navi button. (DSP 0.10.30.23350)  
- v1.1.0: Display the first exception that trigger during mods loading. (DSP 0.10.29.21904)  
- v1.0.0: Initial released. (DSP 0.10.28.20779)  

----

# 错误分析

错误分析器mod添加有用的功能来增强游戏中的错误报告，帮助您识别并解决由模组引起的问题。

## 功能
- **关闭按钮(Close)**：左上角的X按钮可以关闭错误窗口。
- **复制按钮(Copy)**：中间的按钮可以将错误信息以格式化的方式复制到剪贴板。
- **导航按钮(Navi)**：左键单击可导航至出错的机器，右键单击可切换DEBUG追踪模式。
- **模组功能列表**：显示调用堆栈(call stack)内的模组函数名称，帮助识别可能导致错误的模组。
![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/ErrorAnalyzer/img/demo2.png)  

## 示例
```ini
An error has occurred! Game version 0.10.30.23350
5 Mods used: [Script Engine10.0] [IlLine1.0.0] [UnityExplorer4.8.2] [DemoPlugin1.0.0] [ErrorAnalyzer1.1.0] 
Thread Error Exception!!! Thread idx:0 transport Factory idx: Positive Loop System.NullReferenceException: Object reference not set to an instance of an object
  at StationComponent.InternalTickRemote (PlanetFactory factory, System.Int32 timeGene, System.Single shipSailSpeed, System.Single shipWarpSpeed, System.Int32 shipCarries, StationComponent[] gStationPool, AstroData[] astroPoses, VectorLF3& relativePos, UnityEngine.Quaternion& relativeRot, System.Boolean starmap, System.Int32[] consumeRegister) [0x02f66] ;IL_2F66 
  at PlanetTransport.GameTick (System.Int64 time, System.Boolean isActive, System.Boolean isMultithreadMode) [0x00213] ;IL_0213 
  at WorkerThreadExecutor.TransportPartExecute () [0x000e4] ;IL_00E4 
[== Mod patches on the stack ==]
void DemoPlugin.Plugin::InternalTickRemote_Postfix(); InternalTickRemote(Postfix)
```
- 第一行回报目前的游戏版本。  
- 第二行回报目前使用的所有mod名称和版本。  
- 第三行回报调用栈(stack trace), 可以在这里找是否有mod的调用函式, 越上面的导致出错的嫌疑越大。  
- "[== Mod patches on the stack ==]"下方显示mod的补丁, 如果调用栈没找到mod函式那就可能在这里。  


以例图来说, 可以得知模组DemoPlugin对于`StationComponent.InternalTickRemote`的前缀补丁`DemoPlugin.Plugin::InternalTickRemote_Postfix`导致了例图中的错误  

**注意**
- 通常，函数的命名空间通常对应模组名称，因此可以根据函数名称来判断哪个模组可能引发了错误。
- 补丁部分列出的函数可能与错误的根本原因无关。一般来说，堆栈中更高的函数更有可能是原因，但堆栈跟踪中未列出的其他补丁也可能涉及。
- 删除出错的机器可能无法解决问题，建议在尝试拆除前先手动备份存档。有时需要更强大的清理工具，例如[UXAssist](https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/)中的“重新初始化星球”功能。如果这些方法仍然无效，就只能回滚之前正常的存档。
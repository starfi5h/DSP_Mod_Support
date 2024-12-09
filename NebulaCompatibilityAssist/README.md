# Nebula Compatibility Assist

Nebula 0.9.12 hotfix:  
- None

[Spreadsheet for Nebula compatible mods list](https://docs.google.com/spreadsheets/d/16bq5RQfjpNnDt4QGPtPp1U17lmx74EIzCzhuEG7sj6k/edit#gid=373515568)  
This mod tries to patch some mods to make them work better in Nebula Multiplayer Mod.  

<details>
<summary>Supported Mods List (click to expand)</summary>

### [AutoStationConfig](https://thunderstore.io/c/dyson-sphere-program/p/Pasukaru/AutoStationConfig/)
- Sync station configuration and drone, ship, warper count.   
- Note: AutoStationConfig v1.4.0 is broken after DSP-0.9.27. Required [ModFixerOne](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/ModFixerOne/) to fix it.  

### [Auxilaryfunction](https://thunderstore.io/c/dyson-sphere-program/p/blacksnipebiu/Auxilaryfunction/)
- Sync auto station config functions.  
- Sync planetary item fill (ships, fuel) functions.  

### [BlueprintTweaks](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/BlueprintTweaks/)
- Set `useFastDismantle` = false in config file to prevent host from crashing.  
- Note: Some players reported issues when using this mod in multiplayer.  

### [DSPAutoSorter](https://thunderstore.io/c/dyson-sphere-program/p/appuns/DSPAutoSorter/)
- Fix error in client when opening storage UI.  
DSPAutoSorter.DSPAutoSorter.UIStorageWindow_OnOpen_Postfix (UIStorageWindow __instance) [0x0004b]  

### [DSPFreeMechaCustom](https://thunderstore.io/c/dyson-sphere-program/p/appuns/DSPFreeMechaCustom/)
- Free mecha appearance now sync correctly.  

### [DSPOptimizations](https://thunderstore.io/c/dyson-sphere-program/p/Selsion/DSPOptimizations/)
- Fix client crash when leaving a system.  

### [DSPStarMapMemo](https://thunderstore.io/c/dyson-sphere-program/p/appuns/DSPStarMapMemo/)
- Memo now sync when players add/remove icons, or finish editing text area.  

### [FactoryLocator](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/FactoryLocator/)
- Client can now see info of remote planet (Require Host to install FactoryLocator too).   

### [GenesisBook](https://thunderstore.io/c/dyson-sphere-program/p/HiddenCirno/GenesisBook/) (WIP)
- Partially support Quantum Depot syncing: Build, Manual User interaction, Transport in the same planet
- Interplanetary cargo transport in Quantum Depot is not supported yet. Client may see different result.

### [LSTM](https://thunderstore.io/c/dyson-sphere-program/p/hetima/LSTM/)
- Client can now see all ILS stations when choosing system/global tab.  

### [MoreMegaStructure](https://thunderstore.io/c/dyson-sphere-program/p/jinxOAO/MoreMegaStructure/)
- Sync data when player change mega structure type in the editor.  
- Sync data when player change star assembler slider.  
- Sync data when player fire star cannon.  
- Disable modification of the stats panel to avoid conflicts.  

### [PlanetFinder](https://thunderstore.io/c/dyson-sphere-program/p/hetima/PlanetFinder/)
- Fix error in multiplayer lobby.  
- Client can now see vein amount and power status on planets not loaded yet. 
- The data is updated everytime client open the window.  

### [SphereOpt](https://thunderstore.io/c/dyson-sphere-program/p/Andy/SphereOpt/) (WIP)
- Fix `SphereOpt.InstDysonShellRenderer.RenderShells` NRE in client when they join game.  

### [SplitterOverBelt](https://thunderstore.io/c/dyson-sphere-program/p/hetima/SplitterOverBelt/)
- Fix that splitters and pilers put by clients can't reconnect belts.  

### [TheyComeFromVoid](https://thunderstore.io/c/dyson-sphere-program/p/ckcz123/TheyComeFromVoid/) (WIP)
- Early testing. There may be bugs.  
- When clint joins, sync the progress from host  
- Sync add/remove meta drives (relic)
- Sync apply/reset authorization point (buff)

### [UXAssist](https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/)
- Sync `Quick dismantle all buildings`, `Quick build Orbital Collectors`, `Re-initialize Dyson Spheres`, `Quick dismantle Dyson Shells`  
- `Re-intialize planet` is not available in multiplayer mode.  

</details>
  
If the syncing patches cause issue, you can try to disable them in the config file: `BepInEx\config\NebulaCompatibilityAssist.cfg`.  
Currently there are options for 3 mods that use DSPModSave: DSPStarMapMemo, MoreMegaStructure, TheyComeFromVoid.  
When disable, the syncing patch will no longer functional. The host's mod data will no longer be loaded in the clients when they join the game.  
  
----

# 联机mod扩充模组(热修补丁+兼容支援)
联机公开版[Nebula multiplayer mod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/)可以在最新版的游戏中运行并且支援战斗  

有些mod和联机模组有冲突，可能造成红字错误或预期效果不正确。  
[联机兼容的模组列表](https://docs.google.com/spreadsheets/d/16bq5RQfjpNnDt4QGPtPp1U17lmx74EIzCzhuEG7sj6k)  
绿勾=无问题, 蓝勾=需两端皆安装, 红标=有严重冲突  
此模组提供以下mod的兼容支援, 主要是让主机和客户端显示的内容可以一致，或著修復建築不同步的問題:  

<details>
<summary>MOD列表 (点击展开)</summary>

### [AutoStationConfig](https://thunderstore.io/c/dyson-sphere-program/p/Pasukaru/AutoStationConfig/)
- 同步物流站自动配置  
- 注意：AutoStationConfigv1.4.0 与 游戏版本v0.9.27 不兼容, 需要安装ModFixerOne修复  

### [Auxilaryfunction](https://thunderstore.io/c/dyson-sphere-program/p/blacksnipebiu/Auxilaryfunction/) [辅助多功能mod](https://www.bilibili.com/video/BV1SS4y1X75n)
- 同步物流站自动配置相关功能  
- 同步一键填充星球上的飞机飞船翘曲器、燃料  

### [BlueprintTweaks](https://thunderstore.io/c/dyson-sphere-program/p/kremnev8/BlueprintTweaks/)
- 在配置文件中设置 `useFastDismantle` = false 以防止主机崩溃。  
- 注意: 此mod在多人游戏中不稳定, 请谨慎使用  

### [DSPAutoSorter](https://thunderstore.io/c/dyson-sphere-program/p/appuns/DSPAutoSorter/)
- 修复打开储物箱时客机的错误  
DSPAutoSorter.DSPAutoSorter.UIStorageWindow_OnOpen_Postfix (UIStorageWindow __instance) [0x0004b]  

### [DSPFreeMechaCustom](https://thunderstore.io/c/dyson-sphere-program/p/appuns/DSPFreeMechaCustom/)
- 同步免费的机甲外观  

### [DSPOptimizations](https://thunderstore.io/c/dyson-sphere-program/p/Selsion/DSPOptimizations/)
- 修复客户端离开星系会使游戏崩溃的错误  

### [DSPStarMapMemo](https://thunderstore.io/c/dyson-sphere-program/p/appuns/DSPStarMapMemo/)
- 同步星球註記  

### [FactoryLocator](https://thunderstore.io/c/dyson-sphere-program/p/starfi5h/FactoryLocator/)
- 让客机能显示远端星球的建物讯息(需求主机也安装mod)  

### [GenesisBook](https://thunderstore.io/c/dyson-sphere-program/p/HiddenCirno/GenesisBook/) (WIP)
- 部分支持量子箱同步功能：建造、手动用户交互、同一星球内运输
- 量子箱的跨星球运送尚不支持。客机可能会看到不同的结果。

### [LSTM](https://thunderstore.io/c/dyson-sphere-program/p/hetima/LSTM/)
- 让客机显示所有星际物流塔的内容  

### [MoreMegaStructure](https://thunderstore.io/c/dyson-sphere-program/p/jinxOAO/MoreMegaStructure/) 更多巨构建筑
- 当巨构类型或星际组装厂配方更改时同步数据  
- 恒星炮开火时同步数据  
- 修复客户端戴森球电力供给和需求不正确的问题  
- 取消统计页面的修改防止冲突  

### [PlanetFinder](https://thunderstore.io/c/dyson-sphere-program/p/hetima/PlanetFinder/)
- 修正在联机大厅(选择星球介面)时的UI错误  
- 让客机能显示远端星球的资源储量和电力状态  

### [SphereOpt](https://thunderstore.io/c/dyson-sphere-program/p/Andy/SphereOpt/) (WIP)
- 修复客机加入游戏后的NRE错误(`SphereOpt.InstDysonShellRenderer.RenderShells`) (测试中, 可能会出现错误)  

### [SplitterOverBelt](https://thunderstore.io/c/dyson-sphere-program/p/hetima/SplitterOverBelt/)
- 让客机在传送带上放置分流器/集装机时,可以正确地重新连接传送带  

### [TheyComeFromVoid](https://thunderstore.io/c/dyson-sphere-program/p/ckcz123/TheyComeFromVoid/) 深空来敌 (WIP)
- 早期测试中, 可能会出现错误  
- 当客机登陆时, 同步主机进度  
- 同步新增/移除元驱动（圣物）
- 同步部属/重置授权点（强化）

### [UXAssist](https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/)
- 同步`快速拆除所有建筑`, `快速建造轨道采集器`, `初始化戴森球`, `快速拆除戴森壳`
- `初始化本行星`功能在联机中不可用  

</details>
  
如果同步补丁导致问题，您可以尝试在配置文件`BepInEx\config\NebulaCompatibilityAssist.cfg`中禁用它们。  
目前有3个使用 DSPModSave 的mod可以禁用补丁：DSPStarMapMemo、MoreMegaStructure、 TheyComeFromVoid  
禁用后，同步补丁将不再起作用。客机加入游戏时将不再载入主机的该mod数据。  
  
----

<a href="https://www.flaticon.com/free-icons/puzzle" title="puzzle icons">Puzzle icons created by Freepik - Flaticon</a>
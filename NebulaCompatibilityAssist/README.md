# Nebula Compatibility Assist

[Nebula 0.9.0 (pre-release version)](https://nightly.link/NebulaModTeam/nebula/workflows/build-winx64/master/build-artifacts-Release.zip) hotfix:  

- Temporarily disable drone syncing.  

This mod tries to patch some mods to make them work better in Nebula Multiplayer Mod.  
To play multiplayer mod, you can either (A) rollback version to play in 0.9.27, or (B) install prerelease-version.  
Check the mod wiki or join Nebula discord for more info!

<details>
<summary>Supported Mods List (click to expand)</summary>

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/)
- Sync station configuration and drone, ship, warper count.   
- Note: AutoStationConfig v1.4.0 is broken in DSP0.9.27.  

### [Auxilaryfunction](https://dsp.thunderstore.io/package/blacksnipebiu/Auxilaryfunction/)
- Sync auto station config functions.  
- Sync planetary item fill (ships, fuel) functions.  

### ~~[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)~~
- Set `useFastDismantle` = false in config file to prevent host from crashing.  

### [DSPFreeMechaCustom](https://dsp.thunderstore.io/package/appuns/DSPFreeMechaCustom/)
- Free mecha appearance now sync correctly.  

### ~~[DSPMarker](https://dsp.thunderstore.io/package/appuns/DSPMarker/)~~
- Markers now sync when players click apply or delete button.  
- Fix red error when exiting game ([issue#8](https://github.com/appuns/DSPMarker/issues/8))   
- Fix icon didn't refresh when arriving another planet.  

### [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)
- Fix client crash when leaving a system.  

### [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/)
- Memo now sync when players add/remove icons, or finish editing text area.  

### [FactoryLocator](https://dsp.thunderstore.io/package/starfi5h/FactoryLocator/)
- Client can now see info of remote planet (Require Host to install FactoryLocator too).   

### [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/)
- Client can now see all ILS stations when choosing system/global tab.  

### ~~[MoreMegaStructure](https://dsp.thunderstore.io/package/jinxOAO/MoreMegaStructure/)~~
- Sync data when player change mega structure type in the editor.  
- Sync data when player change star assembler slider.  

### [PlanetFinder](https://dsp.thunderstore.io/package/hetima/PlanetFinder/)
- Fix error in multiplayer lobby.  
- Client can now see vein amount and power status on planets not loaded yet. 
- The data is updated everytime client open the window.  

### [SplitterOverBelt](https://dsp.thunderstore.io/package/hetima/SplitterOverBelt/)
- Fix that splitters and pilers put by clients can't reconnect belts.  

</details>
  
----

# 联机mod扩充模组(热修补丁+兼容支援)

此mod适配[联机0.9.0先行测试版](https://nightly.link/NebulaModTeam/nebula/workflows/build-winx64/master/build-artifacts-Release.zip), 可以用管理器本地安装zip文件  
目前建设无人机还没同步  
  
有些mod和联机模组Nebula multiplayer mod有冲突，可能造成红字错误或预期效果不正确。  
此模组提供以下mod的兼容支援(删除线的mod尚未完成), 主要是让主机和客户端显示的内容可以一致，或著修復建築不同步的問題:  

<details>
<summary>MOD列表 (点击展开)</summary>

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/)
- 同步物流站自动配置  
- 注意：AutoStationConfigv1.4.0 与 游戏版本v0.9.27 不兼容  

### [Auxilaryfunction](https://dsp.thunderstore.io/package/blacksnipebiu/Auxilaryfunction/) [辅助多功能mod](https://www.bilibili.com/video/BV1SS4y1X75n)
- 同步物流站自动配置相关功能  
- 同步一键填充星球上的飞机飞船翘曲器、燃料  

### ~~[BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)~~
- 在配置文件中设置 `useFastDismantle` = false 以防止主机崩溃。

### [DSPFreeMechaCustom](https://dsp.thunderstore.io/package/appuns/DSPFreeMechaCustom/)
- 同步免费的机甲外观  

### ~~[DSPMarker](https://dsp.thunderstore.io/package/appuns/DSPMarker/)~~
- 同步地图标记  
- 修复离开游戏时的错误 ([issue#8](https://github.com/appuns/DSPMarker/issues/8))  
- 修复到达另一个星球标记没更新的bug  

### [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)
- 修复客户端离开星系会使游戏崩溃的错误  

### [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/)
- 同步星球註記  

### [FactoryLocator](https://dsp.thunderstore.io/package/starfi5h/FactoryLocator/)
- 让客户端能显示远端星球的建物讯息(需求主机也安装mod)  

### [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/)
- 让客户端显示所有星际物流塔的内容  

### ~~[MoreMegaStructure](https://dsp.thunderstore.io/package/jinxOAO/MoreMegaStructure/) 更多巨构建筑~~
- 当巨构类型或星际组装厂配方更改时同步资料  
- 修复客户端戴森球电力供给和需求不正确的问题  

### [PlanetFinder](https://dsp.thunderstore.io/package/hetima/PlanetFinder/)
- 修正在联机大厅(选择星球介面)时的UI错误  
- 让客户端能显示远端星球的资源储量和电力状态  

### [SplitterOverBelt](https://dsp.thunderstore.io/package/hetima/SplitterOverBelt/)
- 让客户端在传送带上放置分流器/集装机时,可以正确地重新连接传送带  

</details>
  
----

<a href="https://www.flaticon.com/free-icons/puzzle" title="puzzle icons">Puzzle icons created by Freepik - Flaticon</a>
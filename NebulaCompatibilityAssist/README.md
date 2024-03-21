# Nebula Compatibility Assist

Nebula 0.9.1 hotfix:
- Make construction drones only launch if the current player is the cloest one or within 15m.  
- Clear old playerSaves when server start.  
- Suppress Enemy TickLogic excpetion to show only once per session.  

[Spreadsheet for Nebula compatible mods list](https://docs.google.com/spreadsheets/d/16bq5RQfjpNnDt4QGPtPp1U17lmx74EIzCzhuEG7sj6k/edit#gid=373515568)  
This mod tries to patch some mods to make them work better in Nebula Multiplayer Mod.  

<details>
<summary>Supported Mods List (click to expand)</summary>

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/)
- Sync station configuration and drone, ship, warper count.   
- Note: AutoStationConfig v1.4.0 is broken in DSP0.9.27.  

### [Auxilaryfunction](https://dsp.thunderstore.io/package/blacksnipebiu/Auxilaryfunction/)
- Sync auto station config functions.  
- Sync planetary item fill (ships, fuel) functions.  

### [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)
- Set `useFastDismantle` = false in config file to prevent host from crashing.  
- Note: Some players reported issues when using this mod in multiplayer.  

### [DSPFreeMechaCustom](https://dsp.thunderstore.io/package/appuns/DSPFreeMechaCustom/)
- Free mecha appearance now sync correctly.  

### [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)
- Fix client crash when leaving a system.  

### [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/)
- Memo now sync when players add/remove icons, or finish editing text area.  

### [FactoryLocator](https://dsp.thunderstore.io/package/starfi5h/FactoryLocator/)
- Client can now see info of remote planet (Require Host to install FactoryLocator too).   

### [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/)
- Client can now see all ILS stations when choosing system/global tab.  

### [MoreMegaStructure](https://dsp.thunderstore.io/package/jinxOAO/MoreMegaStructure/)
- Sync data when player change mega structure type in the editor.  
- Sync data when player change star assembler slider.  
- Sync data when player fire star cannon.  
- Disable modification of the stats panel to avoid conflicts.  

### [PlanetFinder](https://dsp.thunderstore.io/package/hetima/PlanetFinder/)
- Fix error in multiplayer lobby.  
- Client can now see vein amount and power status on planets not loaded yet. 
- The data is updated everytime client open the window.  

### [SplitterOverBelt](https://dsp.thunderstore.io/package/hetima/SplitterOverBelt/)
- Fix that splitters and pilers put by clients can't reconnect belts.  

</details>
  
----

# 联机mod扩充模组(热修补丁+兼容支援)
联机公开版[Nebula multiplayer mod](https://dsp.thunderstore.io/package/nebula/NebulaMultiplayerMod/) v0.9.1可以在最新版的游戏中运行并且支援战斗  

有些mod和联机模组有冲突，可能造成红字错误或预期效果不正确。  
[联机兼容的模组列表](https://docs.google.com/spreadsheets/d/16bq5RQfjpNnDt4QGPtPp1U17lmx74EIzCzhuEG7sj6k)  
绿勾=无问题, 蓝勾=需两端皆安装, 红标=有严重冲突  
此模组提供以下mod的兼容支援, 主要是让主机和客户端显示的内容可以一致，或著修復建築不同步的問題:  

<details>
<summary>MOD列表 (点击展开)</summary>

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/)
- 同步物流站自动配置  
- 注意：AutoStationConfigv1.4.0 与 游戏版本v0.9.27 不兼容, 需要安装ModFixerOne修复  

### [Auxilaryfunction](https://dsp.thunderstore.io/package/blacksnipebiu/Auxilaryfunction/) [辅助多功能mod](https://www.bilibili.com/video/BV1SS4y1X75n)
- 同步物流站自动配置相关功能  
- 同步一键填充星球上的飞机飞船翘曲器、燃料  

### [BlueprintTweaks](https://dsp.thunderstore.io/package/kremnev8/BlueprintTweaks/)
- 在配置文件中设置 `useFastDismantle` = false 以防止主机崩溃。  
- 注意: 此mod在多人游戏中不稳定, 请谨慎使用  

### [DSPFreeMechaCustom](https://dsp.thunderstore.io/package/appuns/DSPFreeMechaCustom/)
- 同步免费的机甲外观  

### [DSPOptimizations](https://dsp.thunderstore.io/package/Selsion/DSPOptimizations/)
- 修复客户端离开星系会使游戏崩溃的错误  

### [DSPStarMapMemo](https://dsp.thunderstore.io/package/appuns/DSPStarMapMemo/)
- 同步星球註記  

### [FactoryLocator](https://dsp.thunderstore.io/package/starfi5h/FactoryLocator/)
- 让客机能显示远端星球的建物讯息(需求主机也安装mod)  

### [LSTM](https://dsp.thunderstore.io/package/hetima/LSTM/)
- 让客机显示所有星际物流塔的内容  

### [MoreMegaStructure](https://dsp.thunderstore.io/package/jinxOAO/MoreMegaStructure/) 更多巨构建筑
- 当巨构类型或星际组装厂配方更改时同步数据  
- 恒星炮开火时同步数据  
- 修复客户端戴森球电力供给和需求不正确的问题  
- 取消统计页面的修改防止冲突  

### [PlanetFinder](https://dsp.thunderstore.io/package/hetima/PlanetFinder/)
- 修正在联机大厅(选择星球介面)时的UI错误  
- 让客机能显示远端星球的资源储量和电力状态  

### [SplitterOverBelt](https://dsp.thunderstore.io/package/hetima/SplitterOverBelt/)
- 让客机在传送带上放置分流器/集装机时,可以正确地重新连接传送带  

</details>
  
----

<a href="https://www.flaticon.com/free-icons/puzzle" title="puzzle icons">Puzzle icons created by Freepik - Flaticon</a>
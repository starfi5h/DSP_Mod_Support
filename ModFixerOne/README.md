# Mod Fixer One

1. Make some outdated mods to work on the new multithreading system game version. (0.10.33+)  
2. Remove process filter of plugins that block Microsoft Store version.  
This solves `[Warning:BepInEx] Skipping ... because of process filters (DSPGAME.exe)` that prevent the mods from loading.  
3. Fix for save that has mecha upgrade drone task points exceeding vanilla limit 4, which causes an error in `ConstructionModuleComponent.InsertBuildTarget`.  


## How does it work
This mod add the following removed classes/fields/methods back to the game assembly via preloader, so the mod that using them will not trigger TypeLoadException/MissingMethodException/MissingFieldException.

```cs
string StationComponent.name
void PlanetTransport.RefreshTraffic(int)
enum Language { zhCN, enUS, frFR, Max }
public static Language Localization.get_language()
public static string StringTranslate.Translate(this string s)
public StringProto
void GameData.GameTick(long)
```
Type forward for UnityEngine.CoreModule => UnityEngine.InputLegacyModule, so mods that using old Input system can find the reference.  
  
## Support Mods

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/) v1.4.0  
- Fix `MissingMethodExcpetion: void PlanetTransport.Refresh(int)` when loading. ([#19](https://github.com/Pasukaru/DSP-Mods/issues/19))  
You can also use the [UXAssist](https://thunderstore.io/c/dyson-sphere-program/p/soarqin/UXAssist/) by soarqin for more features.   

### [LDBTool](https://thunderstore.io/c/dyson-sphere-program/p/xiaoye97/LDBTool/) v3.0.1  
- Fix `TypeLoadException: Could not resolve type with token 0100002d from typeref (expected class 'UnityEngine.Input' in assembly 'UnityEngine.CoreModule`  
It can now run on the public-test branch version (0.10.33.x).  
  
Some other mods are fixed too. Check the wiki in this mod page for more detail!

## Installation
  
### Mod Manager  
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).  
  
### Manual Download  
Download and unzip content from the mod zip file by clicking `Manual Download` button on Thunderstore mod webpage.   
Move `patchers/ModFixerOnePreloader.dll` file into `BepInEx/patchers/` folder.  
Move `plugins/ModFixerOne.dll` file into `BepInEx/plugins/`folder.  

## Changelog

v2.0.0 - Type forward UnityEngine.Input. Compatible to public-test version. (DSP0.10.33.26482)  
v1.3.2 - Remove PersonalLogistics support. Add construction drones task points fix. (DSP0.10.29.21950)  
v1.3.1 - Let ModFixerOne load first. Add Nebula multiplayer mod pre-release version support. (DSP0.10.28.21247)    
v1.3.0 - Update to Dark Fog version. Remove LongArm, 4DPocket support. (DSP0.10.28.21014)  
v1.2.0 - Add 4DPocket support. Fix litter warning icon in PersonalLogistics. (DSP0.9.27.15466)  
v1.1.0 - Remove process filter. Add AutoStationConfig support.  
v1.0.0 - Initial release. (DSP0.9.27.14659)  

----
<a href="https://www.flaticon.com/free-icons/spanner" title="spanner icons">Spanner icons created by Kiranshastry - Flaticon</a>
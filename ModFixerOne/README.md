# Mod Fixer One

1. Make some outdated mods to work on the Dark Fog game version. (0.10.x)  
2. Remove process filter of plugins that block PC game pass version.  
This solves `[Warning:BepInEx] Skipping ... because of process filters (DSPGAME.exe)` that prevent the mods from loading.  
3. Fix for save that has mecha upgrade drone task points exceeding vanilla limit 4, which causes an error in `ConstructionModuleComponent.InsertBuildTarget`.  


## How does it work
This mod add the following removed classes/fields/methods back to the game assembly via preloader, so the mod that using them will not trigger TypeLoadException/MissingMethodException/MissingFieldException.

```cs
UIStorageGrid UIGame.inventory
string StationComponent.name
void PlanetTransport.RefreshTraffic(int)
enum Language { zhCN, enUS, frFR, Max }
public static Language Localization.get_language()
public static string StringTranslate.Translate(this string s)
public StringProto
```

## Support Mods

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/) v1.4.0  
- Fix `MissingMethodExcpetion: void PlanetTransport.Refresh(int)` when loading. ([#19](https://github.com/Pasukaru/DSP-Mods/issues/19))  
You can also use the [1.4.0-fix version](https://github.com/soarqin/DSP_AutoStationConfig/releases/tag/1.4.0-fix) by soarqin for more features.   

Some other mods are fixed too. Check the wiki in this mod page for more detail!

## Installation
  
### Mod Manager  
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).  
  
### Manual Download  
Download and unzip content from the mod zip file by clicking `Manual Download` button on Thunderstore mod webpage.   
Move `patchers/ModFixerOnePreloader.dll` file into `BepInEx/patchers/` folder.  
Move `plugins/ModFixerOne.dll` file into `BepInEx/plugins/`folder.  

## Changelog

v1.3.2 - Remove PersonalLogistics support. Add construction drones task points fix. (DSP0.10.29.21950)  
v1.3.1 - Let ModFixerOne load first. Add Nebula multiplayer mod pre-release version support. (DSP0.10.28.21247)    
v1.3.0 - Update to Dark Fog version. Remove LongArm, 4DPocket support. (DSP0.10.28.21014)  
v1.2.0 - Add 4DPocket support. Fix litter warning icon in PersonalLogistics. (DSP0.9.27.15466)  
v1.1.0 - Remove process filter. Add AutoStationConfig support.  
v1.0.0 - Initial release. (DSP0.9.27.14659)  

----
<a href="https://www.flaticon.com/free-icons/spanner" title="spanner icons">Spanner icons created by Kiranshastry - Flaticon</a>
# Mod Fixer One

1. Make some mods that haven't updated work on the latest game version.  
2. Remove process filter of plugins for xbox game pass version.  
This solve `[Warning:BepInEx] Skipping ... because of process filters (DSPGAME.exe)`, so XGP user don't have to rename `Dyson Sphere Program.exe` to `DSPGAME.exe` and `Dyson Sphere Program_Data` folder to `DSPGAME_Data` to load some mods.  

## Support Mods

### [LongArm](https://dsp.thunderstore.io/package/Semar/LongArm/)  
- v1.4.6:  Fix `MissingFieldException: Field 'UIGame.inventory' not found.` when opening inventory. ([#3](https://github.com/mattsemar/dsp-long-arm/issues/3))  

### [PersonalLogistics](https://dsp.thunderstore.io/package/Semar/PersonalLogistics/)  
- v2.9.10: Fix `MissingFieldException: Field 'UIGame.inventory' not found.` error. ([#42](https://github.com/mattsemar/dsp-personal-logistics/issues/42))  

### [AutoStationConfig](https://dsp.thunderstore.io/package/Pasukaru/AutoStationConfig/)
- v1.4.0: Fix `MissingMethodExcpetion: void PlanetTransport.Refresh(int)` when loading. ([#19](https://github.com/Pasukaru/DSP-Mods/issues/19))  
You can also use the [1.4.0-fix version](https://github.com/soarqin/DSP_AutoStationConfig/releases/tag/1.4.0-fix) by soarqin for more features,   

## Installation
  
### Mod Manager  
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).  
  
### Manual Download  
Download and unzip content from the mod zip file by clicking `Manual Download` button on Thunderstore mod webpage.   
Drag `patchers/ModFixerOnePreloader.dll` file into `BepInEx/patchers/` folder.  
Drag `plugins/ModFixerOne.dll` file into `BepInEx/plugins/`folder.  

## Changelog

v1.1.0 - Remove process filter. Add AutoStationConfig support.  
v1.0.0 - Initial release. (DSP0.9.27.14659)  

----
<a href="https://www.flaticon.com/free-icons/spanner" title="spanner icons">Spanner icons created by Kiranshastry - Flaticon</a>
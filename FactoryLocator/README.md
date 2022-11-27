# Factory Locator

Find the positions of specified buildings and indicate them with warning icons.  
  
![search local planet](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo1.gif)  
The default hotkey to open the window is Ctrl + F. Keybind can be configured in game control settings.  
In picker window, it will show all possible search options among all buildings. The yellow number shows how many buildings qualify for the condition, or how many items are inside all station/box storage.  
The created warning will stay until Clear button is clicked. You can also toggle to show/hide all warnings.  

  
![search remote planets](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/FactoryLocator/doc/demo2.gif)  
By default, the mod will search for buildings on the local planet.  
When in starmap view, the mod will change the searching planet if the selecting planet has factory on it. When selecting a star, it will search all planets in the star system.  
Click on a warning detail icon to show where the warning locate. Click on other areas to close it.  
  
  
Special thanks for Semar's LongArm mod for inspiration, hetima's mods for UI design and Raptor for mod idea.  

## Nebula multiplayer mod compat
The mod don't have to install on both host and client. Some behaviors will be different though.  
- Host : The temporary guiding warning will sync with clients.  
- Client : Only loaded planets are searchable. When the mod window is opened, the warning icon will stop syncing with host temporarily.    

## Installation
Via [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/), or manual download the file and put `FactoryLocator.dll` in `BepInEx/plugins` folder.

----

## Changelog

v1.0.1 - Fix mod dependency error. Fix incorrect planet when searching by warning.  
v1.0.0 - Initial release. (DSP 0.9.27.15033)  
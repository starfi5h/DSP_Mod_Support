# Changelog

## v1.3.3
- IlLine is now built in, and also shown for wrapper dynamic-method.  
- Temporary fix for threaded logString message in 0.10.33.27024  

## v1.3.2
- Fix a bug that after closing a running error when count > 100, the error window won't show up again.  
- Now patches to `ThreadManager.ProcessFrame` don't show if the function is not at the top.  

## v1.3.1
- Support both 0.10.32 and 0.10.33 (public-test).  
- Wrapper dynamic-method (DMD) is now parsable.  

## v1.3.0
Overhaul enhance error message:  
- Title now show "possible candidates" plugin names instead of all installed mods.  
- The error stack trace now replace .Net type names to C# names, and remove the hash string in <>.  
- Bottom extra info now show related assembly names intead of plugin names.  

## v1.2.4
- Copy button now only copy mod list when shift is pressed.  
- Adapt to DSP 0.10.32.25496  

## v1.2.3
- Highlight plugin name on stacktrace.  

## v1.2.2
- Suppress `CargoTraffic.PickupBeltItems` error in debug mode.  

## v1.2.1
- Suppress `CargoTraffic.SetBeltState` and `CargoContainer.RemoveCargo` error in debug mode to dismantle the belts.  

## v1.2.0
- Add close button and inspect button. (DSP 0.10.30.23350)  

## v1.1.0
- Display the first exception that trigger during mods loading. (DSP 0.10.29.21904)  

## v1.0.0
- Initial released. (DSP 0.10.28.20779)  


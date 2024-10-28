#### v0.6.2
- Path: Change UI layout. Add preview option in the config window.
- Path: Fix camera path preview doesn't show correctly in space.
- IO: Fix error when deleting files.

#### v0.6.1
- Re-design import/export window
- Target: Add editing camera path preview when the target window is opened.
- Record: Now has "Start", "Pause", "Resume", "Stop" states.
- Record: Add Auto Create Subfolder and Reset File Index options for image capturing.
- Record: Add video output format and ffmpeg options.
- Cam: Stop right click move or mine order in freecam mode.

#### v0.6.0
- Record: Add video recording by ffmpeg piping.
- Record: Add sync UPS option.

#### v0.5.1
- Target: Add rotation speed option to let camera do circular motion around the target.  

#### v0.5.0
- Add timelapse recording feature to capture screenshots with secondary camera.
- Revert mecha position restore funcion in v0.4.0.
- Add loop option in camera path window.
- Add set to current mecha position option in target window.

#### v0.4.1
- Add keybind `Play Current Path` to play/pause the current editing path.
- "Hide GUI" now stop buildings highlight and overlay when playing the path.

#### v0.4.0
- Path: Add target for camera to look at.
- Config `MovePlayerWithSpaceCamera` default value is now true. When stop viewing, the mecha will go back to the original position in space.
- Add scroll list in I/O window to view and select imported camera or path to add.  
- Add ZHCN translation.

#### v0.3.0
- Fix camera path is not smooth in space. Add VectorLF3 json convertor in TomlTypeConverter.   
- Make windows resizeable by dragging outside area of bottom-right corner.
- Add import/export config file option in config window - IO tab.
- Path: Add curve interpolation.  
- Path: Add insert keyframe option in path config window.  

#### v0.2.0
- Fix flickr when overwritten mecha space position.
- Cam: Add freecam adjust mode and polar coordinate in camera config window.
- Path: Add spherical interpolation, keyframe format switch and keypoint reorder in path config window.
- Add ToggleLastCameraShortcut, CycyleNextCameraShortcut in plugin config.
- Remember window positions after closing them.

#### v0.1.0
- Initial released. (DSP 0.10.30.23430)  
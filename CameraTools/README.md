# CameraTools

![demo](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/demo1.gif)  
There are 3 main systems in this mod:
- Camera List: Save stationary camera position on local planet or in space. Edit and view them anytime.  
- Path List: Craft a camera path to let the viewing camera go through each point smoothly.
- Timelapse Record: Captured screenshots/video in fixed time intervals by main or second camera.
  
Inspired by [Cities Skylines mod Cinematic Camera Extended by SamsamTS](https://steamcommunity.com/sharedfiles/filedetails/?id=785528371).  

## Camera List (Alt+F5)
![camera list](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/camera-list.png)  
Manage a list to store stationary camera positions and angles. Those can apply across saves.  

- Add Camera: If the player is on planet, add planet camera. Otherwise add space camera.  
  Planet camera will only work on the **local planet**. It can't be viewed from space.  
  Space camera is stored by uPosition which home star is the origin. It can't be viewed from planet.  
- View: Set the current main camera to the target camera. Click again or esc to exit.  
- Edit: Edit camera position, rotation, field of view.  

## Camera Config
![camera config](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/camera-config.png)  
To edit the field, click on the edit button first, input new value, then click set button to apply.  
In polar coordinate, the orignal is either local planet or local star (space camera).  

### Adjust Mode with Freecam
![adjust mode](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/demo-freecam.gif)  
Click on the button to modify the editing camera position and angle with free 3D camera.  
- WASD: Pan movement in local frame  
- Scroll up/down: Forward and backward movement  
- Shift: Hold to increase cam movement 10 times  
- Middle mouse: Hold to rotate  
- Right click: Hold to roll  

## Path List
![path list](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/path-list.png)  
The top input field can change the name of the current path.  
Close the window to save the name change.  

## Path Config (Alt+F6)
![path config](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/path-config.png)  
Create a camera path moving from point to point.  
You can edit clip duration, key point progression time and camera settings.  
The path used on planet should not be used in space, vice versa.  
To edit the field, click on the edit button first, input new value, then click set button to apply.  
- Preview: When this option is enabled, show camera path in green line.
- Hide GUI: When playing the path, hide all mod UI windows.
  
### Playback control
The top area is playback contorl. The timeline slider can preview the path (need to enable Viewing first).  
Progress time is [0,1]. The buttons are to start (|<<), play, to end (>>|).  
Duration is time length of the whole path in second.  
Interp can choose from 3 different interpolation: Linear, Spherical, Curve.  
- Linear interpolation is straight line from one point to another.  
- Spherical will make piecewise path into arc by adjusting altitude with interpolated value.  
- Curve will use Unity's AnimationCurve to make a smooth curve path.   

### Keyframe control
The bottom scroll area is for keyframe (camera pose + time) control. To make a valid path, add at least 2 cameras.  
The keyframe foramt can be displayed in either raio [0,1], or in second.  
When Auto Split toggle is on, it will evenly split time ratio for all the keyframes.  
- Insert keyframe will insert the current view into the current progression time.  
- Append keyframe will add the current view at the last of all keyframes.  

### Target
![target](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/demo2.gif)  
The target to look at during playback.  
When the target config window is opened, it shows a pink sphere marker to indicate where the camera is looking at, and green line to indicate the editing camera path.  
Can set it to a fixed point on the planet or space, or a moving point relatived to the mecha.  
![target window](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/target-window.png)  
If rotation speed is set to positive, the camera will counter-clockwise circle around the target, with axis is the normal vector of the target position.  
If rotation speed is set to negative, it will go clockwise. Can set by rotation period too.  


## Timelapse Record (Alt+F7)

Record Type: Choose to save as a sequence of images (png or jpg), or as a video (mp4).  

### Image Capture
![record image window](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/record-image-window.png)  
First, set the screenshot folder path, then click `Start Record` button to take screenshots with the assigned camera path every x second.  
The file format is `%06d.png` or `%06d.jpg` starting with 1. The index reset after the game restart or clicking the reset button.  
- Status Text: When recording, display how long until the next capture. After capture, display file name and encode time.
- Path: Select a camera path from the list to record in the secondary camera. If not selected, it will capture the main camera. Click play button to let the camera move along the path during recording.  
  Note: This secondary camera only works best **on the local planet**. For space timelapse it is adviced to use the main camera.  
- JPG Quality: JPG quality to encode with. [The range is 1 through 100. 1 is the lowest quality.](https://docs.unity3d.com/ScriptReference/ImageConversion.EncodeToJPG.html)  
- Auto Create Subfolder: When capturing start/stop, it will auto create a subfolder in format of `MMdd_HHmmss` to store screenshots.  
  
  
After recording, you can combine the sequence of images into a video. For example with [FFmpeg](https://www.ffmpeg.org/):
```
ffmpeg -framerate 24 -i %06d.jpg -s 1920x1080 output.mp4
```

### Video Capture
![record video window](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/record-video-window.png)  
To record video, you need to install ffmpeg first and add it into PATH environment variables.  
Then set the video save folder path. The video will store in the folder with name format `MMdd_HH-mm-ss.mp4`.  
- `Sync Progress`: the recording session will be sync with camera path duration. It will auto stop when the path progression reach the end.  
  The selecting path progress will be control by the recording state too. Disable this option if you want to alter the progress freely.
- `Sync UPS`: the timer will use UPS time (in-game) instead of FPS time (Unity Time).  
When the time interval is set to 0, it will caputred every screen update frame.  
- Video Extension: Default format is `.mp4`
- FFmpeg Options: FFmpeg options that apply to the output file.

## Installation & Mod Config
![mod config](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/mod-config.png)  
Via [r2modman](https://thunderstore.io/c/dyson-sphere-program/p/ebkr/r2modman/), or manual download the file and put `CameraTools.dll` in `BepInEx/plugins` folder.  
Keyboard shortcuts can be changed in the mod config window.  
All mod config together with stored camera data is in `BepInEx\config\starfi5h.plugin.CameraTools.cfg`.  

### Import/Export camera and path settings
![import/export window](https://raw.githubusercontent.com/starfi5h/DSP_Mod_Support/dev/CameraTools/img/io-window.png)  
In config window - IO tab, it can import/export config file containing the camera and path data.  
Input field to input the folder path. If the directory is not provided, it will use `BepInEx\config\CameraTools\`.  
In export session, you can export the editing camera or path by their name to the folder.  
In button session, it list every `.cfg` files in the folder. After importing the file, you can choose which camera or path to add in the list.  

## Known issues
- The star image will distort when space camera position is different from player's.  
It can be fixed by letting player move along with space camera in mod config.  
- Rotation is not smooth enough in curve camera path with respect to position changes.  
- Flicker in space when using secondary camera to record timelapse.  
Use the main camera to move with path instead.  
- Moving mecha will flicker in output when recording in high framerate.  

<a href="https://www.flaticon.com/free-icons/video-camera" title="video-camera icons">Video-camera icons created by prettycons - Flaticon</a>  
﻿using BepInEx;
using BepInEx.Configuration;
using System;
using System.IO;
using UnityEngine;

namespace CameraTools
{
	public static class CaptureManager
	{
        public static ConfigEntry<float> TimeInterval;
        public static ConfigEntry<string> ScreenshotFolderPath;
        public static ConfigEntry<int> ScreenshotWidth;
        public static ConfigEntry<int> ScreenshotHeight;
        public static ConfigEntry<bool> UsePNG;
        public static ConfigEntry<bool> UseHD;
        public static ConfigEntry<int> JpgQuality;
        public static ConfigEntry<bool> UseSubfolder;

        public static ConfigEntry<float> VideoOutputFps;
        public static ConfigEntry<string> VideoFolderPath;
        public static ConfigEntry<string> VideoFFmpegOptions;
        public static ConfigEntry<string> VideoExtension;

        public static CameraPath CapturingPath { get; private set; }

        static bool recording;   //是否在錄製中
        static float timer;      //累積計時器
        static bool syncUPS;     //同步邏輯幀
        static bool syncProgress = true; //同步播放進度
        static double lastTimeF; //上次邏輯幀的時間timef
        static bool inSession;   //還在錄製區段中
        static string folderPath = ""; //儲存截圖/錄製影片的資料夾
        static float LOD_Culling_Mul = 1f; //截圖模式LOD倍率(?)

        // Image cature paramters
        static int fileIndex;
        readonly static string subfolderFormatString = "MMdd_HHmmss";
        readonly static string fileFormatString = "{0:D6}";

        // Video catpure paramters
        static bool videoRecordingEnabled = true;
        static FFmpegSession ffmpegSession;
        readonly static string videoFileFormat = "MM-dd_HH-mm-ss";

        // UI properties
        static string statusText = "";
        static Vector2 scrollPosition;
        static bool expandPathListMode = false;


        public static void Load(ConfigFile config)
        {
            TimeInterval = config.Bind("- TimeLapse -", "Time Interval(s)", 30f,
                "Screenshot time interval in seconds");
            ScreenshotFolderPath = config.Bind("- TimeLapse -", "Folder Path", "",
                "The folder path to save screenshots");
            ScreenshotWidth = config.Bind("- TimeLapse -", "Output Width", 1920,
                "Resolution width of timelapse screenshots");
            ScreenshotHeight = config.Bind("- TimeLapse -", "Output Height", 1080,
                "Resolution height of timelapse screenshots");

            UsePNG = config.Bind("- TimeLapse -", "UsePNG", true,
                "If true, the screenshots image format is PNG; otherwise is JPG");
            UseHD = config.Bind("- TimeLapse -", "UseHD", false);
            JpgQuality = config.Bind("- TimeLapse -", "JPG Quality", 95,
                new ConfigDescription("Quality of JPG screenshots", new AcceptableValueRange<int>(0, 100)));
            UseSubfolder = config.Bind("- TimeLapse -", "Use Subfolder", true,
                "Screenshot will be saved in the subfolder in Datetime foramt MMdd_HHmmss");

            VideoOutputFps = config.Bind("- Video Recording -", "Output FPS", 24f,
                "Frame rate of output video");
            VideoFolderPath = config.Bind("- Video Recording -", "Folder Path", "",
                "The folder path to save video recording");
            VideoExtension = config.Bind("- Video Recording -", "Video Extension", ".mp4",
                "Video Format of the output file");
            VideoFFmpegOptions = config.Bind("- Video Recording -", "FFmpeg Options", "-c:v libx264 -pix_fmt yuv420p -preset ultrafast -vf vflip -y",
                "Extra ffmpeg options to output video");
        }

        public static bool SetCameraPath(CameraPath cameraPath)
        {
            // Do not change the CapturingPath by UI during recording
            if (recording) return false;

            CapturingPath = cameraPath;
            return true;
        }

        public static void OnLateUpdate()
        {
            if ((syncProgress && !recording) || GameMain.isPaused) return;

            float deltaTime = Time.deltaTime;
            if (syncUPS)
            {
                deltaTime = (float)(GameMain.instance.timef - lastTimeF);
                lastTimeF = GameMain.instance.timef;
            }
            if (deltaTime < float.Epsilon) return; // logic frame does not advance
            if (CapturingPath != null)
            {
                CapturingPath.OnLateUpdate(deltaTime);
            }
            if (!recording) return;

            timer += deltaTime;
            if (timer >= TimeInterval.Value)
            {
                timer = 0;
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    statusText = "Folder path is empty!";
                    recording = false;
                    return;
                }
                Texture2D texture2D;
                texture2D = SetAndCapture(CapturingPath);
                if (ffmpegSession != null)
                {
                    if (!ffmpegSession.SendToPipe(texture2D, ref statusText))
                    {
                        recording = false; // Stop recording it there is error
                        ffmpegSession.Stop();
                        ffmpegSession = null;
                    }
                    UnityEngine.Object.Destroy(texture2D); // Release the memory
                }
                else
                {
                    var fileName = Path.Combine(folderPath, string.Format(fileFormatString, fileIndex++));
                    if (UsePNG.Value) EncodeAndSavePNG(texture2D, fileName + ".png");
                    else EncodeAndSaveJPG(texture2D, fileName + ".jpg", JpgQuality.Value);
                }

                if (syncProgress && CapturingPath is { IsPlaying: false })
                {
                    StopRecordSession();
                }
            }
        }

        /// <summary>
        /// Set the main camera to CameraPoint and capture the screenshot
        /// </summary>
        static Texture2D SetAndCapture(CameraPath cameraPath)
        {
            var camera = GameCamera.main;
            var cullingMask = camera.cullingMask;
            var position = camera.transform.position;
            var rotation = camera.transform.rotation;
            var fieldOfView = camera.fieldOfView;
            var nearClipPlane = camera.nearClipPlane;
            var farClipPlane = camera.farClipPlane;
            var renderVegetable = true;
            if (GameMain.data.localPlanet != null && GameMain.data.localPlanet.factoryLoaded)
            {
                // Temporary disable renderVegetable to prevent tree/rock from flashing
                renderVegetable = GameMain.data.localPlanet.factoryModel.gpuiManager.renderVegetable;
                GameMain.data.localPlanet.factoryModel.gpuiManager.renderVegetable = false;
            }

            if (cameraPath != null) cameraPath.ApplyToCamera(camera);
            Texture2D texture2D = CaptureTexture2D(ScreenshotWidth.Value, ScreenshotHeight.Value);

            camera.cullingMask = cullingMask;
            camera.transform.position = position;
            camera.transform.rotation = rotation;
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = nearClipPlane;
            camera.farClipPlane = farClipPlane;
            if (GameMain.data.localPlanet != null && GameMain.data.localPlanet.factoryLoaded)
            {                
                GameMain.universeSimulator.planetSimulators[GameMain.data.localPlanet.index].LateUpdate(); // Fix skybox
                GameMain.data.localPlanet.factoryModel.DrawInstancedBatches(camera, true); // Fix building flicker
                GameMain.data.localPlanet.factoryModel.gpuiManager.renderVegetable = renderVegetable;
            }
            if (GameMain.data.spaceSector != null)
            {
                GameMain.data.spaceSector.model.DrawInstancedBatches(camera, true);
            }
            //camera.Render();
            return texture2D;
        }

        /// <summary>
        /// Capture current screenshot as Texture2D
        /// </summary>
        static Texture2D CaptureTexture2D(int width, int height)
        {
            // Modify from GameCamera.CaptureScreenShot(file)
            if (GameMain.data == null) return null;
            try
            {
                //var stopwatch = new HighStopwatch();                
                RenderTexture active = RenderTexture.active;
                Camera camera = GameCamera.main;

                //stopwatch.Begin();
                if (LOD_Culling_Mul != 1f) Shader.SetGlobalFloat("_Global_LOD_Culling_Mul", LOD_Culling_Mul); // Screenshot mode: HD
                RenderTexture renderTexture = new(width, height, 24);
                camera.targetTexture = renderTexture;
                camera.cullingMask = (int)GameCamera.instance.gameLayerMask;
                if (GameMain.data.localPlanet != null && GameMain.data.localPlanet.factoryLoaded)
                {
                    GameMain.universeSimulator.planetSimulators[GameMain.data.localPlanet.index].LateUpdate();
                    GameMain.data.localPlanet.factoryModel.DrawInstancedBatches(camera, true);
                }
                if (GameMain.data.spaceSector != null)
                {
                    GameMain.data.spaceSector.model.DrawInstancedBatches(camera, true);
                }
                camera.Render();
                //Plugin.Log.LogDebug("Render: \t" + stopwatch.duration);

                //stopwatch.Begin();
                if (LOD_Culling_Mul != 1f) Shader.SetGlobalFloat("_Global_LOD_Culling_Mul", 1f); // Screenshot mode: revert
                Texture2D texture2D = new(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);                
                RenderTexture.active = renderTexture;
                texture2D.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0); // Expensive!
                texture2D.Apply();
                RenderTexture.active = active;
                camera.targetTexture = null;
                renderTexture.Release();
                UnityEngine.Object.Destroy(renderTexture);
                //Plugin.Log.LogDebug("Pixels: \t" + stopwatch.duration);

                return texture2D; // Handle Object.Destroy(texture2D) from outside
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError("CaptureTexture2D Failed!\n" + ex);
                return null;
            }
        }

        /// <summary>
        /// Encode Texture2D in JPG format and save as file in multi-thread, then release Texture2D in main thread
        /// </summary>
        /// <param name="jpgQuality">The quality of the jpg image, range from 0 to 100</param>
        static void EncodeAndSaveJPG(Texture2D texture2D, string fileName, int jpgQuality = 100)
        {
            ThreadingHelper.Instance.StartAsyncInvoke(() =>
            {
                try
                {
                    HighStopwatch stopwatch = new(); stopwatch.Begin();
                    var bytes = texture2D.EncodeToJPG(jpgQuality);
                    statusText = $"[{fileIndex:D6}.jpg] {stopwatch.duration * 1000}ms";
                    //Plugin.Log.LogDebug(statusText);
                    File.WriteAllBytes(fileName, bytes);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError("EncodeAndSaveJPG Failed!\n" + ex);
                }
                return () =>
                {
                    UnityEngine.Object.Destroy(texture2D);
                };
            });
        }

        /// <summary>
        /// Encode Texture2D in PNG format and save as file in multi-thread, then release Texture2D in main thread
        /// </summary>
        static void EncodeAndSavePNG(Texture2D texture2D, string fileName)
        {
            ThreadingHelper.Instance.StartAsyncInvoke(() =>
            {
                try
                {
                    HighStopwatch stopwatch = new(); stopwatch.Begin();
                    var bytes = texture2D.EncodeToPNG();
                    statusText = $"[{fileIndex:D6}.png] {stopwatch.duration * 1000}ms";
                    //Plugin.Log.LogDebug(statusText);
                    File.WriteAllBytes(fileName, bytes);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError("EncodeAndSavePNG Failed!\n" + ex);
                }
                return () =>
                {
                    UnityEngine.Object.Destroy(texture2D);
                };
            });
        }

        static void SetShaderParameters()
        {
            if (!UseHD.Value) LOD_Culling_Mul = 1.0f;

            // From HDScreenshot.PrepareHDScreenshot
            float num = Mathf.Tan(GameCamera.screenshotFovX / 2f * 0.017453292f) * 2f / ScreenshotWidth.Value;
            float num2 = 0.0006017031f;
            float num3 = num / num2;
            float num4 = 1f / num3;
            if (num4 < 1f)
            {
                num4 = 1f;
            }
            num4 *= 3f;
            num4 *= num4;
            LOD_Culling_Mul = num4;
            Plugin.Log.LogDebug("[HD] LOD_Culling_Mul = " + LOD_Culling_Mul);
        }

        static void StartRecordSession()
        {
            statusText = "";
            folderPath = videoRecordingEnabled ? VideoFolderPath.Value : ScreenshotFolderPath.Value;
            if (!Directory.Exists(folderPath))
            {
                statusText = "The folder doesn't exist!";
                return;
            }

            if (UseSubfolder.Value && !videoRecordingEnabled)
            {
                try
                {
                    folderPath = Path.Combine(folderPath, DateTime.Now.ToString(subfolderFormatString));
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(ex);
                    statusText = "Error when creating the subfolder!" + ex.Message;
                    folderPath = "";
                    return;
                }
            }

            if (videoRecordingEnabled)
            {
                string videoPath = Path.Combine(folderPath, DateTime.Now.ToString(videoFileFormat) + VideoExtension.Value);
                int videoWidth = ScreenshotWidth.Value;
                int videoHeight = ScreenshotHeight.Value;
                float fps = VideoOutputFps.Value;
                string extraArgs = VideoFFmpegOptions.Value;

                try
                {
                    ffmpegSession = new FFmpegSession(videoPath, videoWidth, videoHeight, fps, extraArgs);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError(ex);
                    statusText = "Error when starting ffmpeg!" + ex.Message;
                    ffmpegSession = null;
                    return;
                }
            }

            recording = true;
            timer = TimeInterval.Value;
            lastTimeF = GameMain.instance.timef;
            inSession = true;
            if (syncProgress && CapturingPath != null)
            {
                CapturingPath.TogglePlayButton(false, true);
            }
        }

        static void StopRecordSession()
        {
            recording = false;
            timer = 0f;
            lastTimeF = 0f;
            inSession = false;
            if (ffmpegSession != null)
            {
                ffmpegSession.Stop();
                ffmpegSession = null;
            }
            if (UseSubfolder.Value)
            {
                fileIndex = 0;
            }
        }



        #region GUI

        public static void ConfigWindowFunc()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            PlayControlPanel();
            Util.ConfigFloatField(TimeInterval);
            PathSelectionBox();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();            
            GUILayout.Label("Record Type".Translate());
            videoRecordingEnabled = !GUILayout.Toggle(!videoRecordingEnabled, "Image".Translate());
            videoRecordingEnabled = GUILayout.Toggle(videoRecordingEnabled, "Video".Translate());
            GUILayout.EndHorizontal();

            if (!videoRecordingEnabled) ImageCaptureSettingPanel();
            else VideoCaptureSettingPanel();
            GUILayout.EndVertical();

            GUILayout.EndScrollView();
        }

        private static void PlayControlPanel()
        {
            GUILayout.BeginHorizontal();
            if (!inSession) // Haven't initial yet
            {
                if (GUILayout.Button("Start Record".Translate()))
                {
                    SetShaderParameters();
                    StartRecordSession();
                }
                syncProgress = GUILayout.Toggle(syncProgress, "Sync Progress".Translate());
                syncUPS = GUILayout.Toggle(syncUPS, "Sync UPS".Translate());                
            }
            else // Is running in session
            {
                if (GUILayout.Button(recording ? "Pause".Translate() : "Resume".Translate()))
                {
                    recording = !recording;
                }
                if (GUILayout.Button("Stop".Translate()))
                {
                    StopRecordSession();
                }
                string countDonwText = string.Format("Next: {0:F1}s".Translate(), (TimeInterval.Value - timer));
                if (syncUPS) countDonwText += " (UPS)";
                GUILayout.Label(countDonwText);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(statusText);
        }

        private static void PathSelectionBox()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Path".Translate());
            if (CapturingPath != null)
            {
                GUILayout.Label($"[{CapturingPath.Index}]" + CapturingPath.Name);
                if (GUILayout.Button(expandPathListMode ? "Clear".Translate() : "Select".Translate()))
                {
                    expandPathListMode = !expandPathListMode;
                    if (!expandPathListMode) SetCameraPath(null);
                }
            }
            else
            {
                GUILayout.Label("None".Translate());
                if (GUILayout.Button(expandPathListMode ? "Clear".Translate() : "Select".Translate()))
                {
                    expandPathListMode = !expandPathListMode;
                }
            }
            GUILayout.EndHorizontal();

            if (expandPathListMode)
            {
                foreach (var path in Plugin.PathList)
                {
                    GUILayout.BeginHorizontal(GUI.skin.box);
                    GUILayout.Label($"[{path.Index}]", GUILayout.MaxWidth(20));
                    GUILayout.Label(path.Name);
                    if (GUILayout.Button("Select".Translate(), GUILayout.MaxWidth(60)))
                    {
                        if (SetCameraPath(path)) expandPathListMode = false;                        
                    }
                    GUILayout.EndHorizontal();
                }
            }
            if (CapturingPath != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(CapturingPath.IsPlaying ? "||" : "▶︎", GUILayout.MaxWidth(40)))
                {
                    CapturingPath.IsPlaying = !CapturingPath.IsPlaying;
                }
                if (GUILayout.Button(Plugin.ViewingPath == CapturingPath ? "[Viewing]".Translate() : "View".Translate()))
                {
                    Plugin.ViewingPath = Plugin.ViewingPath == CapturingPath ? null : CapturingPath;
                }
                if (GUILayout.Button(UIWindow.EditingPath == CapturingPath ? "[Editing]".Translate() : "Edit".Translate()))
                {
                    UIWindow.EditingPath = UIWindow.EditingPath == CapturingPath ? null : CapturingPath;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
        }

        private static void ImageCaptureSettingPanel()
        {            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Image Format".Translate());
            UsePNG.Value = GUILayout.Toggle(UsePNG.Value, "PNG");
            UsePNG.Value = !GUILayout.Toggle(!UsePNG.Value, "JPG");
            UseHD.Value = !GUILayout.Toggle(!UseHD.Value, "(HD mode)".Translate());
            GUILayout.EndHorizontal();

            var tmp = JpgQuality.Value;
            if (!UsePNG.Value) Util.ConfigIntField(JpgQuality);
            Util.ConfigIntField(ScreenshotWidth);
            Util.ConfigIntField(ScreenshotHeight);
            if (tmp != JpgQuality.Value) JpgQuality.Value = JpgQuality.Value > 100 ? 100 : JpgQuality.Value;
            var pathInput = Util.AddTextFieldInput("Folder".Translate(), ScreenshotFolderPath.Value);
            if (!string.IsNullOrWhiteSpace(pathInput))
            {
                try
                {
                    Directory.CreateDirectory(pathInput);
                    if (Directory.Exists(pathInput)) ScreenshotFolderPath.Value = pathInput;
                }
                catch (System.Exception ex)
                {
                    statusText = ex.ToString();
                }
            }
            UseSubfolder.Value = GUILayout.Toggle(UseSubfolder.Value, "Auto Create Subfolder".Translate());
            if (GUILayout.Button("Reset File Index".Translate() + $" [{fileIndex}]"))
            {
                fileIndex = 0;
            }
        }

        private static void VideoCaptureSettingPanel()
        {
            Util.ConfigFloatField(VideoOutputFps);
            Util.ConfigIntField(ScreenshotWidth);
            Util.ConfigIntField(ScreenshotHeight);
            var pathInput = Util.AddTextFieldInput("Folder".Translate(), VideoFolderPath.Value);
            if (!string.IsNullOrWhiteSpace(pathInput))
            {
                try
                {
                    Directory.CreateDirectory(pathInput);
                    if (Directory.Exists(pathInput)) VideoFolderPath.Value = pathInput;
                }
                catch (System.Exception ex)
                {
                    statusText = ex.ToString();
                }
            }
            Util.ConfigStringField(VideoExtension);
            Util.ConfigStringField(VideoFFmpegOptions);
        }

        #endregion
    }
}

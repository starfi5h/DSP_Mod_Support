using BepInEx;
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
        public static ConfigEntry<int> JpgQuality;

        public static ConfigEntry<float> VideoOutputFps;
        public static ConfigEntry<string> VideoFolderPath;
        public static ConfigEntry<string> VideoOutputOptions;

        public static CameraPath CapturingPath { get; private set; }

        static bool recording;   //是否在錄製中
        static float timer;      //累積計時器
        static bool syncUPS;     //同步邏輯幀
        static double lastTimeF; //上次邏輯幀的時間timef

        // Image cature paramters
        static int fileIndex;
        readonly static string fileFormatString = "{0:D6}.jpg";

        // Video catpure paramters
        static bool videoRecordingEnabled = true;
        static FFmpegSession ffmpegSession;
        readonly static string videoFileFormat = "MMdd_HH-mm-ss";
        readonly static string videoExtension = ".mp4";

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
            JpgQuality = config.Bind("- TimeLapse -", "JPG Quality", 95,
                new ConfigDescription("Quality of screenshots", new AcceptableValueRange<int>(0, 100)));

            VideoOutputFps = config.Bind("- Video Recording -", "Output FPS", 24f,
                "Frame rate of output video");
            VideoFolderPath = config.Bind("- Video Recording -", "Folder Path", "",
                "The folder path to save video recording");
            VideoOutputOptions = config.Bind("- Video Recording -", "Output Options", "-preset ultrafast",
                "Extra ffmpeg options to output video");
        }

        public static void OnLateUpdate()
        {
            if (!recording || GameMain.isPaused) return;

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
            timer += deltaTime;
            if (timer >= TimeInterval.Value)
            {
                timer = 0;
                if (ffmpegSession == null && string.IsNullOrWhiteSpace(ScreenshotFolderPath.Value))
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
                    var fileName = Path.Combine(ScreenshotFolderPath.Value, string.Format(fileFormatString, ++fileIndex));
                    EncodeAndSave(texture2D, fileName, JpgQuality.Value);
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
                    //GameMain.data.spaceSector.model.DrawInstancedBatches(camera, true);
                }
                camera.Render();
                //Plugin.Log.LogDebug("Render: \t" + stopwatch.duration);

                //stopwatch.Begin();
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
        static void EncodeAndSave(Texture2D texture2D, string fileName, int jpgQuality = 100)
        {
            ThreadingHelper.Instance.StartAsyncInvoke(() =>
            {
                try
                {
                    HighStopwatch stopwatch = new();
                    stopwatch.Begin();
                    var bytes = texture2D.EncodeToJPG(jpgQuality);
                    statusText = $"[{fileIndex:D6}.jpg] {stopwatch.duration*1000}ms";
                    Plugin.Log.LogDebug(statusText);
                    File.WriteAllBytes(fileName, bytes);
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError("EncodeAndSave Failed!\n" + ex);
                }
                return () =>
                {
                    UnityEngine.Object.Destroy(texture2D);
                };
            });
        }

        #region GUI

        public static void ConfigWindowFunc()
        {
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            PlayControlPanel();
            Util.AddFloatField(TimeInterval);
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
            if (!recording)
            {
                if (GUILayout.Button("Start Record".Translate()))
                {
                    string folderPath = videoRecordingEnabled ? VideoFolderPath.Value : ScreenshotFolderPath.Value;
                    if (!Directory.Exists(folderPath))
                    {
                        statusText = "The folder doesn't exist!";
                        return;
                    }

                    recording = true;
                    timer = TimeInterval.Value;
                    lastTimeF = GameMain.instance.timef;

                    if (videoRecordingEnabled)
                    {
                        string videoPath = Path.Combine(folderPath, DateTime.Now.ToString(videoFileFormat) + videoExtension);
                        int videoWidth = ScreenshotWidth.Value;
                        int videoHeight = ScreenshotHeight.Value;
                        float fps = VideoOutputFps.Value;
                        string extraArgs = VideoOutputOptions.Value;

                        ffmpegSession = new FFmpegSession(videoPath, videoWidth, videoHeight, fps, extraArgs);
                    }
                }
                syncUPS = GUILayout.Toggle(syncUPS, "Sync UPS".Translate());
            }
            else
            {
                if (GUILayout.Button(syncUPS ? "[Recording UPS]".Translate() : "[Recording]".Translate()))
                {
                    recording = false;
                    if (ffmpegSession != null)
                    {
                        ffmpegSession.Stop();
                        ffmpegSession = null;
                    }
                }
                GUILayout.Label(string.Format("Next: {0:F1}s".Translate(), (TimeInterval.Value - timer)));
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
                    if (!expandPathListMode) CapturingPath = null;
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
                        CapturingPath = path;
                        expandPathListMode = false;
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
            var tmp = JpgQuality.Value;
            Util.AddIntField(JpgQuality);
            Util.AddIntField(ScreenshotWidth);
            Util.AddIntField(ScreenshotHeight);
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
        }

        private static void VideoCaptureSettingPanel()
        {
            Util.AddFloatField(VideoOutputFps);
            Util.AddIntField(ScreenshotWidth);
            Util.AddIntField(ScreenshotHeight);
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
        }

        #endregion
    }
}

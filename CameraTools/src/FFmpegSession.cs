using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

// Credit: Piping to ffmpeg.exe with C# by Mathew Sachin
// https://mathewsachin.github.io/blog/2017/07/28/ffmpeg-pipe-csharp.html

namespace CameraTools
{
    public class FFmpegSession
    {        
        private readonly Process process;
        private readonly string fileName;
        private int frameIndex;
        const int ChunkSize = 1024 * 4; // 4KB
        private readonly byte[] buffer = new byte[ChunkSize];

        public FFmpegSession(string filePath, int videoWidth, int videoHeight, float fps, string extraOutputArgs = "")
        {
            var formattedPath = Path.GetFullPath(filePath);
            fileName = Path.GetFileName(filePath);

            var inputArgs = $"-f rawvideo -framerate {fps} -pix_fmt rgb24 -video_size {videoWidth}x{videoHeight} -i -";;
            var outputArgs = $"-vf vflip -r {fps} -y \"{formattedPath}\"";

            Plugin.Log.LogInfo($"Start ffmpeg piping\n{inputArgs}\n{extraOutputArgs} {outputArgs}");
            process = new Process
            {
                StartInfo =
                {
                    FileName = @"ffmpeg.exe",
                    Arguments = $"{inputArgs} {extraOutputArgs} {outputArgs}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                },
            };
            process.EnableRaisingEvents = true;
            process.Start();

            // Get notified when ffmpeg writes to error stream. (it use error to output message)
            process.ErrorDataReceived += (o, e) => { if (!string.IsNullOrEmpty(e.Data)) Plugin.Log.LogDebug("[ffmpeg] " + e.Data); };
            process.BeginErrorReadLine();
            //process.StandardInput.AutoFlush = true;
        }

        public void Stop()
        {
            try
            {
                Plugin.Log.LogInfo("Stop ffmpeg piping");
                process.StandardInput.BaseStream.Flush();
                process.StandardInput.Close();
                process.WaitForExit(0);
                process.Close();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }

        public bool SendToPipe(Texture2D texture2D, ref string status)
        {
            try
            {
                var sw = new HighStopwatch();
                sw.Begin();

                if (process == null) return false;
                var data = texture2D.GetRawTextureData();
                process.StandardInput.BaseStream.Write(data, 0, data.Length);
                // buffer attempt (takes longer time)
                // var array = texture2D.GetRawTextureData<byte>();
                // WriteNativeArray(array, process.StandardInput.BaseStream);

                status = $"{fileName} [{++frameIndex}] {sw.duration * 1000}ms";
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
                status = "ffmpeg pipe fail! " + ex.Message;
                return false;
            }
        }

        public void WriteNativeArray(NativeArray<byte> nativeArray, Stream stream)
        {
            unsafe
            {
                // Pointer to the start of the NativeArray data
                byte* dataPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafePtr(nativeArray);
                int totalLength = nativeArray.Length;
                int offset = 0;

                // Write the NativeArray data in chunks
                while (offset < totalLength)
                {
                    int bytesToCopy = Math.Min(ChunkSize, totalLength - offset);

                    // Copy data from NativeArray pointer to buffer
                    fixed (byte* bufferPtr = buffer)
                    {
                        UnsafeUtility.MemCpy(bufferPtr, dataPtr + offset, bytesToCopy);
                    }

                    stream.Write(buffer, 0, bytesToCopy);
                    offset += bytesToCopy;
                }
            }
        }
    }
}

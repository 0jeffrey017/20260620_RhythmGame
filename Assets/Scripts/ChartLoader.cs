using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RhythmGame
{
    /// <summary>
    /// Loads a chart JSON from StreamingAssets at runtime.
    ///
    /// StreamingAssets is a plain file path on standalone/editor but must be
    /// fetched over UnityWebRequest on Android/WebGL, so we branch on platform.
    /// </summary>
    public static class ChartLoader
    {
        /// <summary>Load the chart whose JSON lives at
        /// StreamingAssets/<fileName> (e.g. "Cat_chart.json").</summary>
        public static async UniTask<ChartData> LoadAsync(
            string fileName, CancellationToken token = default)
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            string json = await ReadAllTextAsync(path, token);
            ChartData chart = ChartData.FromJson(json);
            if (chart == null)
                throw new InvalidDataException($"Failed to parse chart: {fileName}");
            return chart;
        }

        private static async UniTask<string> ReadAllTextAsync(
            string path, CancellationToken token)
        {
            // jar:/http(s) paths (Android, WebGL) need UnityWebRequest.
            bool needsWebRequest =
                path.Contains("://") || path.Contains(":///");

            if (!needsWebRequest)
                return await File.ReadAllTextAsync(path, token);

            using UnityWebRequest req = UnityWebRequest.Get(path);
            await req.SendWebRequest().WithCancellation(token);
            if (req.result != UnityWebRequest.Result.Success)
                throw new IOException($"Chart load failed ({req.result}): {req.error}");
            return req.downloadHandler.text;
        }
    }
}

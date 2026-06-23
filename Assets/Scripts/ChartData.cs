using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// One note in the chart. Field names match the JSON produced by
    /// Tools/analyze_chart.py so Unity's JsonUtility can map them directly.
    /// </summary>
    [Serializable]
    public struct Note
    {
        public float time;       // seconds from audio start
        public float strength;   // normalized onset strength 0..1
        public int intensity;    // discrete level 1..levels
        public int lane;         // 0..lanes-1 (assigned by spectral centroid)
        public float centroidHz; // spectral centroid at the onset
        public float duration;   // 0 = tap, >0 = hold length in seconds (hold head)
        public int noteType;     // 0 = tap, 1 = hold head, 2 = hold tail (visual only)
    }

    /// <summary>
    /// Full chart deserialized from Cat_chart.json. JsonUtility requires the
    /// type to be [Serializable] and the field names to match the JSON keys.
    /// </summary>
    [Serializable]
    public class ChartData
    {
        public string audioFile;
        public int sampleRate;
        public float duration;
        public float bpm;
        public float offset;
        public int lanes;
        public int levels;
        public int noteCount;
        public List<Note> notes = new List<Note>();

        public static ChartData FromJson(string json)
        {
            return JsonUtility.FromJson<ChartData>(json);
        }
    }
}

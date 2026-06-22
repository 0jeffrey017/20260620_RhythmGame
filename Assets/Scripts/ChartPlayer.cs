using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Loads a chart, plays its audio, and raises OnNote as each note's hit
    /// time arrives — synced to the AudioSource's actual playback position
    /// (audio dsp clock), not Time.time, so notes stay locked to the music.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ChartPlayer : MonoBehaviour
    {
        [Tooltip("Chart JSON file name inside StreamingAssets.")]
        [SerializeField] private string chartFileName = "Cat_chart.json";

        [Tooltip("Music clip. Should match chart.audioFile (Cat.wav).")]
        [SerializeField] private AudioSource audioSource;

        [Tooltip("Seconds a note is announced before its hit time (spawn lead).")]
        [SerializeField] private float leadTime = 1.5f;

        /// <summary>Raised when a note crosses (songTime + leadTime). Subscribe
        /// to spawn falling note objects.</summary>
        public event Action<Note> OnNoteSpawn;

        /// <summary>Raised when a note reaches its exact hit time.</summary>
        public event Action<Note> OnNoteHit;
        
        public event Action OnPlay;

        /// <summary>Raised once when the song finishes (audio stopped and all
        /// notes consumed). Used to show the result screen.</summary>
        public event Action OnSongFinished;

        public ChartData Chart { get; private set; }
        public bool IsPlaying { get; private set; }

        /// <summary>Current song position in seconds (audio playback clock).</summary>
        public float SongTime =>
            audioSource != null && audioSource.clip != null
                ? audioSource.time
                : 0f;

        private int _spawnIndex;
        private int _hitIndex;
        private CancellationTokenSource _cts;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }
        
        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        public void StartGame()
        {
            Play().Forget();
        }

        /// <summary>Load and start the given chart file (e.g. "Cat_easy.json").
        /// Used by the difficulty-selection UI.</summary>
        public void PlayChart(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName)) chartFileName = fileName;
            Play().Forget();
        }

        private async UniTaskVoid Play()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            try
            {
                Chart = await ChartLoader.LoadAsync(chartFileName, token);
                Debug.Log($"[ChartPlayer] Loaded {Chart.noteCount} notes, " +
                          $"BPM {Chart.bpm}, duration {Chart.duration:F1}s");

                _spawnIndex = 0;
                _hitIndex = 0;
                IsPlaying = true;

                if (audioSource.clip != null)
                {
                    audioSource.Play();
                                        OnPlay?.Invoke();
                }
                else
                {
                    Debug.LogWarning("[ChartPlayer] No AudioClip assigned; " +
                                                         "running chart clock without sound.");
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Debug.LogError($"[ChartPlayer] Failed to start: {e.Message}");
            }
        }

        private void Update()
        {
            if (!IsPlaying || Chart == null) return;

            float t = SongTime;

            // Announce upcoming notes (lead time before their hit time).
            while (_spawnIndex < Chart.notes.Count &&
                   Chart.notes[_spawnIndex].time - leadTime <= t)
            {
                OnNoteSpawn?.Invoke(Chart.notes[_spawnIndex]);
                _spawnIndex++;
            }

            // Fire notes whose exact hit time has arrived.
            while (_hitIndex < Chart.notes.Count &&
                   Chart.notes[_hitIndex].time <= t)
            {
                OnNoteHit?.Invoke(Chart.notes[_hitIndex]);
                _hitIndex++;
            }

            if (_hitIndex >= Chart.notes.Count &&
                (audioSource.clip == null || !audioSource.isPlaying))
            {
                IsPlaying = false;
                OnSongFinished?.Invoke();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RhythmGame
{
    /// <summary>
    /// Holds the notes that are currently falling, grouped by lane and ordered
    /// by hit time, so a key press can instantly find the oldest unjudged note
    /// in that lane. Judgement logic (Perfect/Good/Miss) is added in Step 2.
    /// </summary>
    public class JudgementManager : MonoBehaviour
    {
        [SerializeField] private ChartPlayer chartPlayer;
        [SerializeField] private float perfectJudgement = 0.05f;
        [SerializeField] private float goodJudgement = 0.12f;
        [SerializeField] private float badJudgement = 0.2f;

        // One queue per lane. Front of the queue is always the next note to hit.
        private Queue<ActiveNote>[] _laneQueues;
        
        public Action<int,JudgeType> OnNoteBeJudged;

        /// <summary>A falling note paired with its on-screen object.</summary>
        public readonly struct ActiveNote
        {
            public readonly Note Note;        // timing data (time, lane, ...)
            public readonly NodeObject Object; // the visual note on screen
            public readonly int UseId;         // Object.UseId captured at spawn

            public ActiveNote(Note note, NodeObject obj)
            {
                Note = note;
                Object = obj;
                UseId = obj != null ? obj.UseId : -1;
            }

            // The visual object still belongs to this note only if it hasn't been
            // reused for another note since (its UseId would have changed).
            public bool IsObjectStillMine =>
                Object != null && Object.UseId == UseId;
        }

        public enum JudgeType
        {   
            None,
            Perfect,
            Good,
            Bad,
            Miss
        }

        private void Awake()
        {
            // Lane count comes from the chart; default to 4 until it loads.
            BuildQueues(4);
        }

        private void Update()
        {
            if (chartPlayer == null || _laneQueues == null) return;

            float now = chartPlayer.SongTime;

            // Any note whose hit window has fully passed without a press is a
            // miss. Front of each queue is the oldest, so we drain from there.
            for (int lane = 0; lane < _laneQueues.Length; lane++)
            {
                var queue = _laneQueues[lane];
                while (queue.Count > 0 &&
                       now - queue.Peek().Note.time > badJudgement)
                {
                    queue.Dequeue();
                    // Leave the visual note falling; it returns itself to the
                    // pool when it crosses the NoteReturnLine.
                    OnNoteBeJudged?.Invoke(lane, JudgeType.Miss);
                }
            }
        }

        private void BuildQueues(int laneCount)
        {
            _laneQueues = new Queue<ActiveNote>[laneCount];
            for (int i = 0; i < laneCount; i++)
            {
                _laneQueues[i] = new Queue<ActiveNote>();
            }
        }

        /// <summary>
        /// Called by NoteSpawner right after it spawns a note's visual object.
        /// Registers it as judgeable in its lane.
        /// </summary>
        public void Register(Note note, NodeObject obj)
        {
            if (note.lane < 0 || note.lane >= _laneQueues.Length)
            {
                Debug.LogWarning($"[Judge] note.lane {note.lane} out of range.");
                return;
            }
            _laneQueues[note.lane].Enqueue(new ActiveNote(note, obj));
        }
        
        /// <summary>
        /// | 判定 | 許容 ||---|---|
        ///| Perfect | ±0.05秒 |
        ///| Good    | ±0.12秒 |
        ///| Bad     | ±0.20秒 |
        ///| 窓外     | 無視（早押し）／ Update で見逃しMiss（遅れ）|
        /// </summary>
        /// <param name="lane"></param>
        /// <param name="time"></param>
        public void NoteJudgement(int lane, float time)
        {
            if (_laneQueues[lane].Count == 0)
            {
                // Empty lane: a stray tap with no note to hit. Ignore.
                return;
            }

            // Peek first — don't consume the note until we know it's in range.
            ActiveNote active = _laneQueues[lane].Peek();
            var diff = Mathf.Abs(active.Note.time - time);

            // Outside the hit window: treat as an early/stray tap and leave the
            // note in the queue. Late notes are missed by Update, not here.
            if (diff > badJudgement)
            {
                return;
            }

            JudgeType result;
            if (diff <= perfectJudgement) result = JudgeType.Perfect;
            else if (diff <= goodJudgement) result = JudgeType.Good;
            else result = JudgeType.Bad;

            // The note is consumed: remove it and clear its on-screen object.
            // Only release the visual if it still belongs to this note — if the
            // pooled instance was already reused for a later note (e.g. the
            // original crossed the return line first), releasing it would wrongly
            // remove that other note from the same lane.
            _laneQueues[lane].Dequeue();
            if (active.IsObjectStillMine) active.Object.ReturnToPool();

            OnNoteBeJudged?.Invoke(lane, result);
        }
    }
}

using System;
using RhythmGame;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private ChartPlayer chartPlayer;
    [SerializeField] private Transform[] noteStartLaneTransform;
    [SerializeField] private Transform[] noteEndLaneTransform;
    [SerializeField] private Transform[] noteDestroyLaneTransform;
    [SerializeField] private NotePool notePool;

    private void Start()
    {   
        chartPlayer.OnNoteSpawn += ChartPlayerOnOnNoteSpawn;
    }

    private void ChartPlayerOnOnNoteSpawn(Note note)
    {
        var noteObj = notePool.Pool.Get();
        noteObj.SetMovement(noteStartLaneTransform[note.lane].position,
            noteEndLaneTransform[note.lane].position,
            noteDestroyLaneTransform[note.lane].position,
            1.5f);
    }
}
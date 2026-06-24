using RhythmGame;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [SerializeField] private ChartPlayer chartPlayer;
    [SerializeField] private Transform[] noteStartLaneTransform;
    [SerializeField] private Transform[] noteEndLaneTransform;
    [SerializeField] private Transform[] noteDestroyLaneTransform;
    [SerializeField] private NotePool notePool;
    [SerializeField] private JudgementManager judgementManager;

    private void Start()
    {
        chartPlayer.OnNoteSpawn += ChartPlayerOnOnNoteSpawn;
    }

    [Tooltip("Seconds a note takes to travel from spawn to the hit line. Must match ChartPlayer.leadTime.")]
    [SerializeField] private float leadTime = 1.5f;

    private void ChartPlayerOnOnNoteSpawn(Note note)
    {
        var noteObj = notePool.Pool.Get();
        Vector3 startPos = noteStartLaneTransform[note.lane].position;
        Vector3 endPos = noteEndLaneTransform[note.lane].position;
        Vector3 destroyPos = noteDestroyLaneTransform[note.lane].position;

        // Hold notes: convert the hold's seconds into a world-space tail length
        // using the note's travel speed (distance / leadTime). Only the hold
        // head carries a duration; the tail and taps have 0.
        float speed = leadTime > 0f ? Vector3.Distance(startPos, endPos) / leadTime : 0f;
        float holdLength = note.duration * speed;

        // Push the hold head's destroy point back by its own tail length so the
        // head (and the line trailing to its tail note) fully clears the hit
        // line before being recycled, instead of vanishing mid-hold.
        if (holdLength > 0f)
        {
            Vector3 dir = (destroyPos - endPos).sqrMagnitude > 1e-8f
                ? (destroyPos - endPos).normalized : (endPos - startPos).normalized;
            destroyPos += dir * holdLength;
        }

        noteObj.SetMovement(startPos, endPos, destroyPos, leadTime);
        noteObj.Configure(holdLength);

        // Register this note so it can be judged when the player hits its lane.
        judgementManager.Register(note, noteObj);
    }
}
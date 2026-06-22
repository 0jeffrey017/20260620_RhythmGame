using System;
using RhythmGame;
using UnityEngine;

public class PopUpTextController : MonoBehaviour
{
    [SerializeField] private RectTransform[] lanes;
    [SerializeField] private GameObject textBox; 
    [SerializeField] private JudgementManager judgementManager;
    [SerializeField] private TextBoxPool textBoxPool;
    private void Start()
    {
        judgementManager.OnNoteBeJudged += OnNoteBeJudged;
    }

    private void OnNoteBeJudged(int lane, JudgementManager.JudgeType type)
    {
        var text = textBoxPool.Pool.Get();
        var message = type switch
        {
            JudgementManager.JudgeType.None => "None",
            JudgementManager.JudgeType.Perfect => "<color=green>Perfect</color>",
            JudgementManager.JudgeType.Good => "<color=yellow>Good</color>",
            JudgementManager.JudgeType.Bad => "<color=blue>Bad</color>",
            JudgementManager.JudgeType.Miss => "<color=red>Miss</color>",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
        text.Initialization(message, lanes[lane].anchoredPosition3D);
    }
}
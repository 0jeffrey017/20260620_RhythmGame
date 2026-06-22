using System;
using RhythmGame;
using TMPro;
using UnityEngine;

/// <summary>
/// Listens to JudgementManager and turns each judgement into score and
/// combo, then reflects them on the UI. Perfect/Good/Bad keep the combo
/// going; a Miss resets it to zero.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [SerializeField] private JudgementManager judgementManager;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;

    [Header("Points per judgement")]
    [SerializeField] private int perfectScore = 1000;
    [SerializeField] private int goodScore = 500;
    [SerializeField] private int badScore = 100;

    public int Score { get; private set; }
    public int Combo { get; private set; }
    public int MaxCombo { get; private set; }

    // Per-judgement tallies, shown on the result screen.
    public int PerfectCount { get; private set; }
    public int GoodCount { get; private set; }
    public int BadCount { get; private set; }
    public int MissCount { get; private set; }

    /// <summary>Fired after score/combo update. (score, combo)</summary>
    public event Action<int, int> OnScoreChanged;

    private void Start()
    {
        if (judgementManager != null)
            judgementManager.OnNoteBeJudged += HandleJudged;
        Refresh();
    }

    private void OnDestroy()
    {
        if (judgementManager != null)
            judgementManager.OnNoteBeJudged -= HandleJudged;
    }

    private void HandleJudged(int lane, JudgementManager.JudgeType type)
    {
        float s = Mathf.Clamp(((Combo + 1) * 1.0f/MaxCombo) * 3f,1f,3f);
        switch (type)
        {
            case JudgementManager.JudgeType.Perfect:
                AddCombo();
                Score += Mathf.RoundToInt(perfectScore * s);
                PerfectCount++;
                break;
            case JudgementManager.JudgeType.Good:
                AddCombo();
                Score += Mathf.RoundToInt(goodScore * s);
                GoodCount++;
                break;
            case JudgementManager.JudgeType.Bad:
                AddCombo();
                Score += Mathf.RoundToInt(badScore * s);
                BadCount++;
                break;
            case JudgementManager.JudgeType.Miss:
                MissCount++;
                Combo = 0;
                break;
            default:
                return; // None: ignore.
        }

        Refresh();
        OnScoreChanged?.Invoke(Score, Combo);
    }

    private void AddCombo()
    {
        Combo++;
        if (Combo > MaxCombo) MaxCombo = Combo;
    }

    private void Refresh()
    {
        if (scoreText) scoreText.text = $"Score : {Score:N0}";
        if (comboText) comboText.text = Combo > 0 ? $"{Combo} COMBO" : "";
    }
}
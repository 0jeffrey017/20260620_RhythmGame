using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RhythmGame
{
    /// <summary>
    /// Shows the result panel when the song finishes, and offers Retry (replay
    /// the same difficulty) and Back (return to difficulty selection). Both work
    /// by reloading the scene, which cleanly resets notes, pools and score.
    /// </summary>
    public class ResultScreen : MonoBehaviour
    {
        [SerializeField] private ChartPlayer chartPlayer;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private GameObject resultPanel;

        [Header("Result texts")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI maxComboText;
        [SerializeField] private TextMeshProUGUI perfectText;
        [SerializeField] private TextMeshProUGUI goodText;
        [SerializeField] private TextMeshProUGUI badText;
        [SerializeField] private TextMeshProUGUI missText;

        [Header("Buttons")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button backButton;

        private void Awake()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (backButton != null) backButton.onClick.AddListener(BackToSelect);
        }

        private void Start()
        {
            if (chartPlayer != null) chartPlayer.OnSongFinished += Show;
        }

        private void OnDestroy()
        {
            if (chartPlayer != null) chartPlayer.OnSongFinished -= Show;
        }

        private void Show()
        {
            if (scoreManager != null)
            {
                SetText(scoreText, $"Score: {scoreManager.Score:N0}");
                SetText(maxComboText, $"Max Combo: {scoreManager.MaxCombo}");
                SetText(perfectText, $"Perfect: {scoreManager.PerfectCount}");
                SetText(goodText, $"Good: {scoreManager.GoodCount}");
                SetText(badText, $"Bad: {scoreManager.BadCount}");
                SetText(missText, $"Miss: {scoreManager.MissCount}");
            }

            if (resultPanel != null) resultPanel.SetActive(true);
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null) label.text = value;
        }

        /// <summary>Replay the same difficulty.</summary>
        private void Retry()
        {
            // GameSession.SelectedChart is kept, so the reloaded scene replays it.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Return to the difficulty-selection menu.</summary>
        private void BackToSelect()
        {
            GameSession.Clear();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

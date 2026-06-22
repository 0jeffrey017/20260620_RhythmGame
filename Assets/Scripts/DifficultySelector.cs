using UnityEngine;
using UnityEngine.UI;

namespace RhythmGame
{
    /// <summary>
    /// Three buttons (Easy/Normal/Hard) that load the matching chart and start
    /// the song. Hides the selection panel once a difficulty is chosen.
    /// </summary>
    public class DifficultySelector : MonoBehaviour
    {
        [SerializeField] private ChartPlayer chartPlayer;

        [Header("Buttons")]
        [SerializeField] private Button easyButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        [Header("Chart file names (in StreamingAssets)")]
        [SerializeField] private string easyChart = "Cat_easy.json";
        [SerializeField] private string normalChart = "Cat_normal.json";
        [SerializeField] private string hardChart = "Cat_hard.json";

        [Tooltip("Panel shown for selection; hidden once a difficulty starts.")]
        [SerializeField] private GameObject selectionPanel;

        private void Awake()
        {
            if (easyButton != null)
                easyButton.onClick.AddListener(() => Select(easyChart));
            if (normalButton != null)
                normalButton.onClick.AddListener(() => Select(normalChart));
            if (hardButton != null)
                hardButton.onClick.AddListener(() => Select(hardChart));
        }

        private void Start()
        {
            // Retry path: a difficulty was already chosen before the reload, so
            // skip the menu and replay it immediately.
            if (GameSession.HasSelection)
            {
                if (selectionPanel != null) selectionPanel.SetActive(false);
                chartPlayer.PlayChart(GameSession.SelectedChart);
            }
            else if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
            }
        }

        private void Select(string chartFile)
        {
            if (chartPlayer == null)
            {
                Debug.LogError("[DifficultySelector] ChartPlayer not assigned.");
                return;
            }

            GameSession.SelectedChart = chartFile;
            chartPlayer.PlayChart(chartFile);
            if (selectionPanel != null) selectionPanel.SetActive(false);
        }
    }
}

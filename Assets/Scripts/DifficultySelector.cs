using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
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
        
        [Header("CountDown")]
        [SerializeField] private GameObject countDownPanel;
        [SerializeField] private TextMeshProUGUI countDownText;
        [SerializeField] private AudioSource countDownSoundSource;
        [SerializeField] private AudioClip countDownSound;

        private void Awake()
        {
            if (easyButton != null)
                easyButton.onClick.AddListener(() => Select(easyChart));
            if (normalButton != null)
                normalButton.onClick.AddListener(() => Select(normalChart));
            if (hardButton != null)
                hardButton.onClick.AddListener(() => Select(hardChart));
        }

        private async void Start()
        {
            // Retry path: a difficulty was already chosen before the reload, so
            // skip the menu and replay it immediately.
            if (GameSession.HasSelection)
            {
                if (selectionPanel != null) selectionPanel.SetActive(false);
                await CountDown(3,this.GetCancellationTokenOnDestroy());
                chartPlayer.PlayChart(GameSession.SelectedChart);
            }
            else if (selectionPanel != null)
            {
                selectionPanel.SetActive(true);
                countDownPanel.SetActive(false);
            }
        }

        private async void Select(string chartFile)
        {
            if (chartPlayer == null)
            {
                Debug.LogError("[DifficultySelector] ChartPlayer not assigned.");
                return;
            }

            GameSession.SelectedChart = chartFile;
            if (selectionPanel != null) selectionPanel.SetActive(false);
            await CountDown(3,this.GetCancellationTokenOnDestroy());
            chartPlayer.PlayChart(chartFile);
        }
        
        private async UniTask CountDown(int countDown, CancellationToken token)
        {
            countDownPanel.SetActive(true);
            int c = countDown;
            countDownSoundSource.PlayOneShot(countDownSound);
            while (c >= 0)
            {
                countDownText.text = c.ToString();
                await LMotion.Create(300f, 325f, 1f)
                    .WithOnComplete(() => c--)
                    .Bind(v => countDownText.fontSize = v)
                    .ToUniTask(token);
            }
            countDownPanel.SetActive(false);
        }
    }
}

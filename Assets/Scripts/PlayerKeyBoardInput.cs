using LitMotion;
using LitMotion.Extensions;
using R3;
using RhythmGame;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerKeyBoardInput : MonoBehaviour
{
    [SerializeField] private Image imageX;
    [SerializeField] private Image imageC;
    [SerializeField] private Image imageN;
    [SerializeField] private Image imageM;
    [SerializeField] private CatController catController;
    [SerializeField] private ChartPlayer chartPlayer;
    [SerializeField] private JudgementManager judgementManager;

    [Header("Hit effect")]
    [SerializeField] private EffectPool effectPool;
    [Tooltip("World position per lane (index = judgement lane 0..3) where the hit effect plays.")]
    [SerializeField] private Transform[] laneEffectPoints;
    
    [SerializeField] private AudioClip judgementSound;
    [SerializeField] private AudioSource[] audioSource = new AudioSource[4];

    [SerializeField] private float _turnDuration = 0.1f;
    private PlayerInput _playerInput;
    private void Start()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.Enable();

        _playerInput.Player.Lane1.performed += HandleLane01;
        _playerInput.Player.Lane2.performed += HandleLane02;
        _playerInput.Player.Lane3.performed += HandleLane03;
        _playerInput.Player.Lane4.performed += HandleLane04;
    }

    private void OnDestroy()
    {
        if (_playerInput == null) return;

        _playerInput.Player.Lane1.performed -= HandleLane01;
        _playerInput.Player.Lane2.performed -= HandleLane02;
        _playerInput.Player.Lane3.performed -= HandleLane03;
        _playerInput.Player.Lane4.performed -= HandleLane04;

        _playerInput.Player.Disable();
        _playerInput.Dispose();
        _playerInput = null;
    }

    private void HandleLane01(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageX.rectTransform);
            catController.HandleTurn(false, false, _turnDuration).Forget();
            judgementManager.NoteJudgement(3, chartPlayer.SongTime);
            PlayHitEffect(3);
            PlayAudio();
        }
    }
    private void HandleLane02(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageC.rectTransform);
            catController.HandleTurn(true, false, _turnDuration).Forget();
            judgementManager.NoteJudgement(2, chartPlayer.SongTime);
            PlayHitEffect(2);
            PlayAudio();
        }
    }
    private void HandleLane03(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageN.rectTransform);
            catController.HandleTurn(false, true, _turnDuration).Forget();
            judgementManager.NoteJudgement(1, chartPlayer.SongTime);
            PlayHitEffect(1);
            PlayAudio();
        }
    }
    private void HandleLane04(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageM.rectTransform);
            catController.HandleTurn(true, true, _turnDuration).Forget();
            judgementManager.NoteJudgement(0, chartPlayer.SongTime);
            PlayHitEffect(0);
            PlayAudio();
        }
    }

    private void PlayHitEffect(int lane)
    {
        if (effectPool == null || laneEffectPoints == null) return;
        if (lane < 0 || lane >= laneEffectPoints.Length) return;
        var point = laneEffectPoints[lane];
        if (point != null) effectPool.Play(point.position);
    }

    private void HandleMovement(RectTransform tran)
    {
        LMotion.Create(1.0f, 1.5f, 0.1f)
            .WithOnComplete(() =>
            {
                LMotion.Create(1.5f, 1.0f, 0.1f)
                    .BindToLocalScaleXYZ(tran)
                    .AddTo(this);
            })
            .BindToLocalScaleXYZ(tran)
            .AddTo(this);
    }

    private void PlayAudio()
    {
        foreach (var source in audioSource)
        {
            if (source != null && !source.isPlaying)
            {
                source.PlayOneShot(judgementSound);
                return;
            }
        }
        Debug.Log("no audioSource to Playing audio");
    }
}

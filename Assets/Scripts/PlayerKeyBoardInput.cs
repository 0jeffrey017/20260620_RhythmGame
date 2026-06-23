using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
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

    [SerializeField] private float _turnDuration = 0.1f;
    private PlayerInput _playerInput;

    private CancellationTokenSource _lane1Cts;
    private CancellationTokenSource _lane2Cts;
    private CancellationTokenSource _lane3Cts;
    private CancellationTokenSource _lane4Cts;
    
    private void Start()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.Enable();

        _playerInput.Player.Lane1.performed += HandleLane01;
        _playerInput.Player.Lane2.performed += HandleLane02;
        _playerInput.Player.Lane3.performed += HandleLane03;
        _playerInput.Player.Lane4.performed += HandleLane04;

        // Key release ends a hold (Button actions raise `canceled` on release).
        _playerInput.Player.Lane1.canceled += HandleLane01Release;
        _playerInput.Player.Lane2.canceled += HandleLane02Release;
        _playerInput.Player.Lane3.canceled += HandleLane03Release;
        _playerInput.Player.Lane4.canceled += HandleLane04Release;
    }

    private void OnDestroy()
    {
        if (_playerInput == null) return;

        _playerInput.Player.Lane1.performed -= HandleLane01;
        _playerInput.Player.Lane2.performed -= HandleLane02;
        _playerInput.Player.Lane3.performed -= HandleLane03;
        _playerInput.Player.Lane4.performed -= HandleLane04;

        _playerInput.Player.Lane1.canceled -= HandleLane01Release;
        _playerInput.Player.Lane2.canceled -= HandleLane02Release;
        _playerInput.Player.Lane3.canceled -= HandleLane03Release;
        _playerInput.Player.Lane4.canceled -= HandleLane04Release;

        _playerInput.Player.Disable();
        _playerInput.Dispose();
        _playerInput = null;
        
        CancelToken(ref _lane1Cts);
        CancelToken(ref _lane2Cts);
        CancelToken(ref _lane3Cts);
        CancelToken(ref _lane4Cts);
    }

    // Lane key released -> finish any active hold in that judgement lane.
    private void HandleLane01Release(InputAction.CallbackContext _)
    { 
        judgementManager.HoldRelease(3, chartPlayer.SongTime);
        CancelToken(ref _lane1Cts);
    }
        
    private void HandleLane02Release(InputAction.CallbackContext _)
    {
        judgementManager.HoldRelease(2, chartPlayer.SongTime);
        CancelToken(ref _lane2Cts);
    }

    private void HandleLane03Release(InputAction.CallbackContext _)
    {
        judgementManager.HoldRelease(1, chartPlayer.SongTime);
        CancelToken(ref _lane3Cts);
    }
        
    private void HandleLane04Release(InputAction.CallbackContext _)
    {
        judgementManager.HoldRelease(0, chartPlayer.SongTime);
        CancelToken(ref _lane4Cts);
    }

    private void HandleLane01(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageX.rectTransform);
            catController.HandleTurn(false, false, _turnDuration).Forget();
            judgementManager.NoteJudgement(3, chartPlayer.SongTime);
            if(_lane1Cts != null) CancelToken(ref _lane1Cts);
            _lane1Cts = new CancellationTokenSource();
            PlayHitEffect(3,_lane1Cts.Token).Forget();
        }
    }
    private void HandleLane02(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageC.rectTransform);
            catController.HandleTurn(true, false, _turnDuration).Forget();
            judgementManager.NoteJudgement(2, chartPlayer.SongTime);
            if(_lane2Cts != null) CancelToken(ref _lane2Cts);
            _lane2Cts = new CancellationTokenSource();
            PlayHitEffect(2,_lane2Cts.Token).Forget();
        }
    }
    private void HandleLane03(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageN.rectTransform);
            catController.HandleTurn(false, true, _turnDuration).Forget();
            judgementManager.NoteJudgement(1, chartPlayer.SongTime);
            if(_lane3Cts != null) CancelToken(ref _lane3Cts);
            _lane3Cts = new CancellationTokenSource();
            PlayHitEffect(1,_lane3Cts.Token).Forget();
        }
    }
    private void HandleLane04(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageM.rectTransform);
            catController.HandleTurn(true, true, _turnDuration).Forget();
            judgementManager.NoteJudgement(0, chartPlayer.SongTime);
            if(_lane4Cts != null) CancelToken(ref _lane4Cts);
            _lane4Cts = new CancellationTokenSource();
            PlayHitEffect(0,_lane4Cts.Token).Forget();
        }
    }

    private async UniTaskVoid PlayHitEffect(int lane , CancellationToken token)
    {
        if (effectPool == null || laneEffectPoints == null) return;
        if (lane < 0 || lane >= laneEffectPoints.Length) return;
        var point = laneEffectPoints[lane];
        ParticleSystem ps = null;
        if (point != null)
        {   
            ps = effectPool.Play(point);
        }
        try
        {
            await UniTask.WaitUntil(() => token.IsCancellationRequested, cancellationToken: token);
        }
        catch (System.OperationCanceledException)
        {
            // Expected cancellation
        }
        finally
        {
            if (ps != null)
            {
                effectPool.StopPlay(ps);
            }
        }
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

    private void CancelToken(ref CancellationTokenSource cts)
    {
        if (cts == null) return;
        try
        {
            cts.Cancel();
        }
        catch (System.ObjectDisposedException)
        {
            // Already disposed
        }
        cts.Dispose();
        cts = null;
    }
}

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class CatController : MonoBehaviour
{   
    [SerializeField] private SkinnedMeshRenderer cat;
    [SerializeField] private InputAction turnRight;
    [SerializeField] private InputAction turnLeft;
    private CancellationTokenSource ctx;
    private Transform catTransform;

    private void Start()
    {
        catTransform =  cat.transform;
        turnRight.Enable();
        turnLeft.Enable();
    }

    private void OnDestroy()
    {
        turnRight.Disable();
        turnLeft.Disable();
    }

    private void Update()
    {
        if (turnRight.WasPressedThisFrame())
        {
            ctx?.Cancel();
            ctx = new CancellationTokenSource();
            HandleTurn(true,18000,ctx.Token).Forget();
        }
        else if (turnLeft.WasPressedThisFrame())
        {
            ctx?.Cancel();
            ctx = new CancellationTokenSource();
            HandleTurn(false,18000,ctx.Token).Forget();
        }
    }

    private async UniTaskVoid HandleTurn(bool turnRight,float speed,CancellationToken token)
    {
        try
        {
            cat.SetBlendShapeWeight(0, 1);
            float dir = turnRight ? 1f : -1f;
            float time = 2.5f;
            while (time > 0.0f)
            {
                token.ThrowIfCancellationRequested();
                time -= Time.deltaTime;
                catTransform.localRotation = Quaternion.Euler(0f, time * dir * speed, 0f);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Cancelled");
        }
        finally
        {
            cat.SetBlendShapeWeight(0, 0);
            catTransform.localRotation =  Quaternion.Euler(0f, 0f, 0f);
        }
        
    }
}

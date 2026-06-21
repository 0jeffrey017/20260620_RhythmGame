using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;

public class CatController : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer catLeft;
    [SerializeField] private SkinnedMeshRenderer catRight;
    
    private bool leftBusy;
    private bool rightBusy;

    public async UniTaskVoid HandleTurn(bool turnRight, bool canRight, float duration)
    {
        // 既に回転中なら無視（一回押す＝一回転）
        if (canRight ? rightBusy : leftBusy) return;
        if (canRight) rightBusy = true; else leftBusy = true;

        SkinnedMeshRenderer cat = canRight ? catRight : catLeft;
        cat.SetBlendShapeWeight(0, 1);

        // 現在角度を基準に相対的に360°回す（右回転は -、左回転は +）
        float startY = cat.transform.localEulerAngles.y;
        float rot = 360f * 2.0f;
        float targetY = turnRight ? startY - rot : startY + rot;

        await LMotion.Create(startY, targetY, duration)
            .BindToLocalEulerAnglesY(cat.transform)
            .ToUniTask(this.GetCancellationTokenOnDestroy());

        cat.SetBlendShapeWeight(0, 0);

        if (canRight) rightBusy = false; else leftBusy = false;
    }
}

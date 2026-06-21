using LitMotion;
using RhythmGame;
using UnityEngine;

/// <summary>
/// ビート同期のディスコライト。ChartPlayer.OnNoteHit を購読し、ノーツの瞬間に
/// スポットライトをパッと光らせて減衰させる。色は lane、強さは note.strength で変化。
/// rig（親）を回し続けることでビームが回転する。URP 想定。
/// </summary>
public class DiscoLight : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private ChartPlayer chartPlayer;
    [SerializeField] private Light[] lights;
    [Tooltip("回転させる親。未設定ならこの transform を回す。")]
    [SerializeField] private Transform rig;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 90f; // 度/秒

    [Header("Flash")]
    [SerializeField] private float baseIntensity = 1f;
    [SerializeField] private float flashIntensity = 8f;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Ease flashEase = Ease.OutQuad;

    [Tooltip("lane ごとの色。lane を要素数で割った余りで参照。")]
    [SerializeField]
    private Color[] laneColors =
    {
        Color.red, Color.cyan, Color.yellow, Color.magenta
    };

    private MotionHandle[] _handles;

    private void Awake()
    {
        if (rig == null) rig = transform;
        _handles = new MotionHandle[lights.Length];
    }

    private void OnEnable()
    {
        if (chartPlayer != null) chartPlayer.OnNoteHit += Flash;
    }

    private void OnDisable()
    {
        if (chartPlayer != null) chartPlayer.OnNoteHit -= Flash;
    }

    private void Update()
    {
        rig.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    private void Flash(Note note)
    {
        Color color = laneColors.Length > 0
            ? laneColors[note.lane % laneColors.Length]
            : Color.white;

        // strength(0..1) でピーク輝度を変える（弱い音は控えめに光る）
        float peak = Mathf.Lerp(flashIntensity * 0.5f, flashIntensity, note.strength);

        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null) continue;

            light.color = color;

            // 直前のフラッシュが残っていたら止めてから新しく打つ
            if (_handles[i].IsActive()) _handles[i].Cancel();

            _handles[i] = LMotion.Create(peak, baseIntensity, flashDuration)
                .WithEase(flashEase)
                .Bind(light, static (x, l) => l.intensity = x);
        }
    }
}

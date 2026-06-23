# Cat.wav → 譜面パイプライン

`Cat.wav` を解析してリズムゲームのノーツデータ（JSON/CSV）を生成し、Unity から読み込む。

## 流れ

```
Cat.wav ─► analyze_chart.py ─► Cat_chart.json / Cat_chart.csv ─► ChartLoader ─► ChartPlayer
          (オンセット/BPM/強度)   (StreamingAssets)              (実行時読込)    (音と同期)
```

## 1. 解析 (Python)

依存: `numpy` のみ（`pip install numpy`）。WAV 読み込みは標準ライブラリ `wave`。

```bash
python Tools/analyze_chart.py                       # 既定: Assets/Cat.wav を解析
python Tools/analyze_chart.py Assets/Cat.wav -o Assets/StreamingAssets/Cat_chart
```

アルゴリズム:
- **オンセット**: STFT → スペクトルフラックス（各周波数ビンの正の増分の総和）→ 適応閾値ピーク検出
- **BPM**: オンセット包絡線の自己相関のピーク（60–200 BPM 範囲）
- **強度 (intensity)**: 正規化したオンセット強度を `levels` 段階（既定 1..4）に量子化
- **レーン (lane)**: スペクトル重心（低域→lane0、高域→lane3）で `lanes` 本（既定 4）に振り分け

### 譜面の難易度調整（ノーツ密度・和音・ホールド）

| 引数 | 効果 |
|------|------|
| `--preset {easy,normal,hard}` | 難易度プリセット。delta/min-gap/和音率/ホールド率を一括設定（個別フラグで上書き可） |
| `--delta 0.06` | ピーク閾値。**上げるとノーツが減る** |
| `--min-gap 0.09` | ノーツ間最小秒数。上げると間引かれる |
| `--chord-rate 0.0` | 強オンセットが**同時押し（和音）**になる確率 0..1 |
| `--hold-rate 0.0` | 適格タップが**長押し（ホールド）**になる確率 0..1 |
| `--min-hold` `--max-hold` | ホールド長の下限/上限（秒） |
| `--seed 0` | 和音/ホールドの乱数シード（再現性） |
| `--lanes 4` `--levels 4` | レーン数 / 強度段階 |

プリセット既定値:

| preset | delta | min-gap | chord-rate | hold-rate | hold長 |
|---|---|---|---|---|---|
| easy   | 0.12 | 0.18 | 0.00 | 0.25 | 0.4–0.8 |
| normal | 0.08 | 0.12 | 0.10 | 0.35 | 0.4–1.0 |
| hard   | 0.05 | 0.09 | 0.22 | 0.18 | 0.4–1.2 |

3難易度の生成（入力は `Assets/_inputAssets/Cat.wav`、実行は `py`）:
```bash
py Tools/analyze_chart.py Assets/_inputAssets/Cat.wav --preset easy   -o Assets/StreamingAssets/Cat_easy
py Tools/analyze_chart.py Assets/_inputAssets/Cat.wav --preset normal -o Assets/StreamingAssets/Cat_normal
py Tools/analyze_chart.py Assets/_inputAssets/Cat.wav --preset hard   -o Assets/StreamingAssets/Cat_hard
```

- **和音** = 同時刻・別レーンの2ノート（`duration` は両方 0）。
- **ホールド** = 2ノート構成。**頭ノート**（`noteType=1`, `duration>0`）と **尾ノート**（`noteType=2`, `duration=0`, 見た目専用）を `time` と `time+duration` に配置。レーンは `[time, time+duration]` を専有し、その間に他ノートは入らない。

## 2. 出力フォーマット

`Cat_chart.json`（Unity 用 / JsonUtility 対応）:
```json
{
  "audioFile": "Cat.wav", "sampleRate": 48000, "duration": 134.6,
  "bpm": 75.0, "offset": 1.55, "lanes": 4, "levels": 4, "noteCount": 835,
  "notes": [ { "time": 1.5467, "strength": 0.54, "intensity": 3, "lane": 3, "centroidHz": 4505.7, "duration": 0.0, "noteType": 0 } ]
}
```
`duration`: 0=タップ/尾、>0=ホールド頭の秒数。`noteType`: 0=タップ、1=ホールド頭、2=ホールド尾（見た目専用・判定対象外）。`Cat_chart.csv`: `time,strength,intensity,lane,centroidHz,duration,noteType`（確認・外部ツール用）

## 3. Unity 側

`Assets/Scripts/`:
- `ChartData.cs` — JSON に対応するデータ構造（`Note` 構造体 + `ChartData`）
- `ChartLoader.cs` — StreamingAssets から非同期読込（Standalone/Android/WebGL 対応）
- `ChartPlayer.cs` — 譜面を `AudioSource.time` に同期して `OnNoteSpawn` / `OnNoteHit` を発火

### シーン組み込み手順
1. 空の GameObject を作成し `ChartPlayer` を追加（`AudioSource` は自動付与）。
2. `AudioSource.clip` に `Cat.wav` を割り当て。
3. `Chart File Name` = `Cat_chart.json`（既定）。
4. ノーツ生成側スクリプトから購読:
   ```csharp
   player.OnNoteSpawn += note => SpawnFallingNote(note.lane, note.intensity);
   player.OnNoteHit   += note => CheckJudgement(note);
   ```

> 注: `Cat_chart.json` は `Assets/StreamingAssets/` に出力済み。譜面を再生成したら、このフォルダへ出力すれば Unity 側の変更は不要。

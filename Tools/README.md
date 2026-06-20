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

### 譜面の難易度調整（ノーツ密度）

| 引数 | 効果 |
|------|------|
| `--delta 0.06` | ピーク閾値。**上げるとノーツが減る** |
| `--min-gap 0.09` | ノーツ間最小秒数。上げると間引かれる |
| `--lanes 4` `--levels 4` | レーン数 / 強度段階 |

例: `python Tools/analyze_chart.py --delta 0.12 --min-gap 0.15 -o Assets/StreamingAssets/Cat_easy`

## 2. 出力フォーマット

`Cat_chart.json`（Unity 用 / JsonUtility 対応）:
```json
{
  "audioFile": "Cat.wav", "sampleRate": 48000, "duration": 134.6,
  "bpm": 75.0, "offset": 1.55, "lanes": 4, "levels": 4, "noteCount": 835,
  "notes": [ { "time": 1.5467, "strength": 0.54, "intensity": 3, "lane": 3, "centroidHz": 4505.7 } ]
}
```
`Cat_chart.csv`: `time,strength,intensity,lane,centroidHz`（確認・外部ツール用）

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

#!/usr/bin/env python3
"""
Cat.wav -> onset / BPM / intensity analysis -> notes data -> JSON + CSV

Pipeline:
  1. Load WAV (stdlib `wave`), convert to mono float.
  2. STFT (numpy.fft) -> magnitude spectrogram.
  3. Spectral-flux onset envelope -> adaptive peak picking -> onset times.
  4. Per-onset strength -> intensity level (1..4) and lane (by spectral centroid).
  5. Global BPM via autocorrelation of the onset envelope.
  6. Write chart JSON (for Unity) and CSV (for inspection).

Pure numpy + stdlib only. Usage:
    python analyze_chart.py [input.wav] [-o out_basename] [--lanes 4] [--levels 4]
"""

import argparse
import csv
import json
import os
import sys
import wave

import numpy as np


# ----------------------------------------------------------------------------
# Audio loading
# ----------------------------------------------------------------------------
def load_wav_mono(path):
    """Return (samples float32 in [-1,1], sample_rate)."""
    with wave.open(path, "rb") as wf:
        n_channels = wf.getnchannels()
        sampwidth = wf.getsampwidth()
        sr = wf.getframerate()
        n_frames = wf.getnframes()
        raw = wf.readframes(n_frames)

    if sampwidth == 2:
        dtype = np.int16
        norm = 32768.0
    elif sampwidth == 1:  # unsigned 8-bit
        data = np.frombuffer(raw, dtype=np.uint8).astype(np.float32)
        data = (data - 128.0) / 128.0
        if n_channels > 1:
            data = data.reshape(-1, n_channels).mean(axis=1)
        return data, sr
    elif sampwidth == 4:
        dtype = np.int32
        norm = 2147483648.0
    else:
        raise ValueError(f"Unsupported sample width: {sampwidth} bytes")

    data = np.frombuffer(raw, dtype=dtype).astype(np.float32) / norm
    if n_channels > 1:
        data = data.reshape(-1, n_channels).mean(axis=1)
    return data, sr


# ----------------------------------------------------------------------------
# Onset detection (spectral flux)
# ----------------------------------------------------------------------------
def spectral_flux_envelope(samples, sr, frame_size=2048, hop=512):
    """Return (onset_envelope, times, centroids_hz) per frame."""
    window = np.hanning(frame_size).astype(np.float32)
    n_frames = 1 + max(0, (len(samples) - frame_size) // hop)
    if n_frames <= 1:
        raise ValueError("Audio too short to analyze.")

    # Build frame matrix and run a single batched rFFT.
    idx = np.arange(frame_size)[None, :] + hop * np.arange(n_frames)[:, None]
    frames = samples[idx] * window
    spec = np.abs(np.fft.rfft(frames, axis=1))  # (n_frames, freq_bins)

    # Spectral flux: sum of positive bin-to-bin differences.
    diff = np.diff(spec, axis=0)
    flux = np.maximum(diff, 0.0).sum(axis=1)
    flux = np.concatenate([[0.0], flux])

    # Spectral centroid (Hz) per frame -> used to assign lanes.
    freqs = np.fft.rfftfreq(frame_size, d=1.0 / sr)
    mag_sum = spec.sum(axis=1) + 1e-9
    centroids = (spec * freqs[None, :]).sum(axis=1) / mag_sum

    times = hop * np.arange(n_frames) / sr
    return flux, times, centroids, hop


def normalize(x):
    x = x - x.min()
    m = x.max()
    return x / m if m > 0 else x


def pick_peaks(env, sr, hop, pre=0.05, post=0.05, mean_win=0.10,
               delta=0.06, min_gap=0.09):
    """Adaptive peak picking on the onset envelope.

    A frame i is an onset if it is the local maximum within +/- pre/post and
    exceeds (local mean + delta), enforcing a minimum gap between onsets.
    """
    env = normalize(env)
    fps = sr / hop
    pre_n = max(1, int(pre * fps))
    post_n = max(1, int(post * fps))
    mean_n = max(1, int(mean_win * fps))
    gap_n = max(1, int(min_gap * fps))

    # Moving average as a local threshold baseline.
    kernel = np.ones(2 * mean_n + 1) / (2 * mean_n + 1)
    local_mean = np.convolve(env, kernel, mode="same")

    peaks = []
    last = -gap_n - 1
    n = len(env)
    for i in range(n):
        v = env[i]
        if v < local_mean[i] + delta:
            continue
        lo = max(0, i - pre_n)
        hi = min(n, i + post_n + 1)
        if v < env[lo:hi].max():
            continue
        if i - last < gap_n:
            # Keep the stronger of two close peaks.
            if peaks and v > env[peaks[-1]]:
                peaks[-1] = i
                last = i
            continue
        peaks.append(i)
        last = i
    return np.array(peaks, dtype=int)


# ----------------------------------------------------------------------------
# Tempo (BPM) via autocorrelation of the onset envelope
# ----------------------------------------------------------------------------
def estimate_bpm(env, sr, hop, bpm_min=60.0, bpm_max=200.0):
    env = normalize(env)
    env = env - env.mean()
    fps = sr / hop
    corr = np.correlate(env, env, mode="full")
    corr = corr[len(corr) // 2:]  # non-negative lags

    lag_min = int(fps * 60.0 / bpm_max)
    lag_max = int(fps * 60.0 / bpm_min)
    lag_max = min(lag_max, len(corr) - 1)
    if lag_max <= lag_min:
        return 0.0
    window = corr[lag_min:lag_max]
    best_lag = lag_min + int(np.argmax(window))
    if best_lag <= 0:
        return 0.0
    bpm = 60.0 * fps / best_lag
    return round(float(bpm), 2)


# ----------------------------------------------------------------------------
# Notes assembly
# ----------------------------------------------------------------------------
def build_notes(env, times, centroids, peaks, lanes, levels):
    env_n = normalize(env)
    notes = []
    if len(centroids) > 0:
        c_lo = float(np.percentile(centroids, 5))
        c_hi = float(np.percentile(centroids, 95))
    else:
        c_lo, c_hi = 0.0, 1.0
    span = max(c_hi - c_lo, 1e-6)

    for i in peaks:
        strength = float(env_n[i])
        # intensity level 1..levels
        level = int(np.clip(np.ceil(strength * levels), 1, levels))
        # lane 0..lanes-1 by spectral centroid (low freq -> lane 0)
        c = float(centroids[i])
        lane = int(np.clip((c - c_lo) / span * lanes, 0, lanes - 1))
        notes.append({
            "time": round(float(times[i]), 4),
            "strength": round(strength, 4),
            "intensity": level,
            "lane": lane,
            "centroidHz": round(c, 1),
        })
    notes.sort(key=lambda n: n["time"])
    return notes


def main():
    ap = argparse.ArgumentParser(description="WAV -> rhythm-game chart (JSON/CSV)")
    ap.add_argument("input", nargs="?", default="Assets/Cat.wav",
                    help="input WAV path (default: Assets/Cat.wav)")
    ap.add_argument("-o", "--out", default="Assets/StreamingAssets/Cat_chart",
                    help="output basename (without extension)")
    ap.add_argument("--lanes", type=int, default=4)
    ap.add_argument("--levels", type=int, default=4)
    ap.add_argument("--frame", type=int, default=2048)
    ap.add_argument("--hop", type=int, default=512)
    ap.add_argument("--delta", type=float, default=0.06,
                    help="peak threshold above local mean (higher = fewer notes)")
    ap.add_argument("--min-gap", type=float, default=0.09,
                    help="minimum seconds between notes")
    args = ap.parse_args()

    if not os.path.isfile(args.input):
        sys.exit(f"Input not found: {args.input}")

    print(f"[1/4] Loading {args.input} ...")
    samples, sr = load_wav_mono(args.input)
    duration = len(samples) / sr
    print(f"      sr={sr} Hz, duration={duration:.2f}s, samples={len(samples)}")

    print("[2/4] Computing spectral-flux onset envelope ...")
    env, times, centroids, hop = spectral_flux_envelope(
        samples, sr, frame_size=args.frame, hop=args.hop)

    print("[3/4] Peak picking + BPM estimation ...")
    peaks = pick_peaks(env, sr, hop, delta=args.delta, min_gap=args.min_gap)
    bpm = estimate_bpm(env, sr, hop)
    notes = build_notes(env, times, centroids, peaks, args.lanes, args.levels)
    print(f"      notes={len(notes)}, bpm={bpm}")

    offset = notes[0]["time"] if notes else 0.0
    chart = {
        "audioFile": os.path.basename(args.input),
        "sampleRate": sr,
        "duration": round(duration, 3),
        "bpm": bpm,
        "offset": round(offset, 4),
        "lanes": args.lanes,
        "levels": args.levels,
        "noteCount": len(notes),
        "notes": notes,
    }

    os.makedirs(os.path.dirname(args.out) or ".", exist_ok=True)
    json_path = args.out + ".json"
    csv_path = args.out + ".csv"

    print(f"[4/4] Writing {json_path} and {csv_path} ...")
    with open(json_path, "w", encoding="utf-8") as f:
        json.dump(chart, f, ensure_ascii=False, indent=2)

    with open(csv_path, "w", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        w.writerow(["time", "strength", "intensity", "lane", "centroidHz"])
        for n in notes:
            w.writerow([n["time"], n["strength"], n["intensity"],
                        n["lane"], n["centroidHz"]])

    print("Done.")
    print(f"  BPM       : {bpm}")
    print(f"  Notes     : {len(notes)}")
    print(f"  Offset    : {offset:.3f}s")
    print(f"  Avg/sec   : {len(notes)/duration:.2f}")


if __name__ == "__main__":
    main()

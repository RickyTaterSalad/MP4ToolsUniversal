# DaVinci Resolve crash: `1.mp4` vs `1_working.mp4`

## Summary

`1.mp4` is a **stream-copied trim** (`-c:v copy`) with a **broken MP4 timeline**.  
`1_working.mp4` is a **full re-encode** with a clean container. Resolve is likely crashing on the mux/timing issues in `1.mp4`, not on resolution or Opus audio alone.

---

## How each file was made

From `log.txt`, `1.mp4` was created with:

```text
ffmpeg -ss 01:17:43 -i "source.mp4" -t 00:00:27 -c:v copy -c:a libopus ...
```

`1_working.mp4` was re-encoded with **libx265** (`encoder: Lavc62.11.100 libx265`, CRF 18).

---

## Key differences

| Property | `1.mp4` (crashes) | `1_working.mp4` (works) |
|---|---|---|
| **Creation method** | Stream copy trim | Re-encoded (x265) |
| **HEVC profile** | **Main 10 @ L5.2 (High tier)** | **Main @ L5 (8-bit)** |
| **Bit depth** | **10-bit** | **8-bit** |
| **Stored height** | **2176** (padded) | 2160 |
| **Video samples** | **828** | **810** |
| **Duration (actual samples)** | **~27.63 s** | ~27.03 s |
| **Timebase** | 1/90000 | 1/30000 |
| **`ctts` box (B-frame timing)** | **Missing** | Present (671 entries) |
| **`colr` box (color metadata)** | **Missing** | Present (BT.709) |
| **Edit list start** | `media_time=53925` (~0.6 s in) | `media_time=2002` |
| **Bitrate** | ~56 Mbps | ~38 Mbps |

---

## Most likely crash cause: broken MP4 from stream-copy trim

The biggest problem is the **container/timing**, not the codec name alone.

### 1. Missing `ctts` with B-frames present

Both files have B-frames (`has_b_frames=2`), but only the working file has a **`ctts`** (composition timestamp) table. Without it, presentation order for B-frames is undefined in MP4.

That is a known failure mode for NLEs like Resolve when importing HEVC in MP4.

### 2. Bad trim timing / wrong sample count

For ~27 s at 29.97 fps you expect **~810 frames**.  
`1.mp4` has **828 samples** (~27.63 s of media), because stream copy had to grab extra frames from an open GOP at the cut point.

The edit list tries to hide that:

- Video: `media_time=53925`, `duration=27028` (90k timescale)
- That is an in-GOP start with a hacked duration trim

Resolve’s parser is much less tolerant of this than ffmpeg/ffprobe.

### 3. 10-bit HEVC vs 8-bit re-encode

`1.mp4` is **10-bit Main 10** with **stored height 2176** (encoder padding).  
`1_working.mp4` is **8-bit Main** at exactly 3840×2160.

10-bit HEVC in MP4 is already finicky in some apps; combined with the broken mux above, it is a plausible crash trigger.

### 4. Missing color metadata in the container

The working file has a **`colr`** atom (BT.709). The crashing file does not. Usually that causes color issues, not crashes — but it is another sign the remux is incomplete.

---

## What to do

### Option A — Re-encode (what fixed `1_working.mp4`)

```bash
ffmpeg -i 1.mp4 -c:v libx265 -crf 18 -pix_fmt yuv420p -c:a copy 1_fixed.mp4
```

### Option B — Better trim (avoid stream-copy `-ss` before `-i`)

```bash
ffmpeg -ss 01:17:43 -i source.mp4 -t 00:00:27 \
  -c:v libx265 -crf 18 -pix_fmt yuv420p -c:a libopus -b:a 192k out.mp4
```

If you must copy video, put **`-ss` after `-i`** and accept that you may still get GOP edge weirdness — re-encoding is safer for Resolve.

### Option C — Quick test

Strip audio and see if Resolve still crashes. If video alone crashes, it is the HEVC/mux issue; if not, Opus may also be a factor (less likely here since the working file also uses Opus).

---

## Bottom line

`1.mp4` is a **malformed stream-copied 10-bit HEVC MP4** (missing `ctts`, bad edit list, extra frames). `1_working.mp4` is a **clean 8-bit re-encode** with proper MP4 structure. That container/timing difference is almost certainly what crashes DaVinci Resolve.

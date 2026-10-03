#!/usr/bin/env bash
# One-shot: download BaseballCV PHC .pt and export ONNX into the MP4Tools model cache.
# Runtime detection uses C#/ONNX only — this script is optional setup, not a sidecar.
set -euo pipefail

DEST_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/MP4Tools/models"
ONNX_NAME="pitcher_hitter_catcher.onnx"
PT_URL="https://data.balldatalab.com/index.php/s/KP5ZqJKEfjQ785X/download/pitcher_hitter_catcher_detector_v3.pt"
WORKDIR="${TMPDIR:-/tmp}/mp4tools_bcv_export_$$"

mkdir -p "$DEST_DIR" "$WORKDIR"
cd "$WORKDIR"

if [[ ! -f "$DEST_DIR/$ONNX_NAME" ]]; then
  echo "Downloading BaseballCV PHC weights…"
  curl -L --fail -o pitcher_hitter_catcher.pt "$PT_URL"
  python3 -m venv .venv
  # shellcheck disable=SC1091
  source .venv/bin/activate
  pip install -q ultralytics onnx onnxslim
  python - <<'PY'
from ultralytics import YOLO
m = YOLO("pitcher_hitter_catcher.pt")
print("classes:", m.names)
m.export(format="onnx", imgsz=640, simplify=True, opset=12)
PY
  cp -f pitcher_hitter_catcher.onnx "$DEST_DIR/$ONNX_NAME"
  echo "Wrote $DEST_DIR/$ONNX_NAME"
else
  echo "Already present: $DEST_DIR/$ONNX_NAME"
fi

rm -rf "$WORKDIR"

#!/bin/bash
# Flame graph generation for sequential vs random access patterns
# Uses standalone perf mode (PerfRunner) — no BDN overhead in the profile
# Requires: perf, FlameGraph tools (stackcollapse-perf.pl, flamegraph.pl)
#   git clone https://github.com/brendangregg/FlameGraph.git
# Usage: FLAMEGRAPH_DIR=/path/to/FlameGraph ./flame-graph.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
OUTPUT_DIR="$PROJECT_DIR/flamegraphs"

FLAMEGRAPH_DIR="${FLAMEGRAPH_DIR:-$HOME/FlameGraph}"

if [ ! -f "$FLAMEGRAPH_DIR/stackcollapse-perf.pl" ]; then
    echo "FlameGraph tools not found at $FLAMEGRAPH_DIR"
    echo "Install: git clone https://github.com/brendangregg/FlameGraph.git ~/FlameGraph"
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

echo "=== Building ==="
dotnet build -c Release "$PROJECT_DIR" --nologo -q

BIN=$(find "$PROJECT_DIR"/bin/Release/net9.0 -name "hardware-counters" -type f | head -1)

echo ""
echo "=== Recording Sequential (standalone, 8M elements) ==="
perf record -g -F 99 -o "$OUTPUT_DIR/sequential.data" -- \
    "$BIN" perf-sequential 8000000

perf script -i "$OUTPUT_DIR/sequential.data" | \
    "$FLAMEGRAPH_DIR/stackcollapse-perf.pl" | \
    "$FLAMEGRAPH_DIR/flamegraph.pl" --title "SumSequential — 8M elements, standalone" --colors hot \
    > "$OUTPUT_DIR/sequential.svg"

echo ""
echo "=== Recording Random (standalone, 8M elements) ==="
perf record -g -F 99 -o "$OUTPUT_DIR/random.data" -- \
    "$BIN" perf-random 8000000

perf script -i "$OUTPUT_DIR/random.data" | \
    "$FLAMEGRAPH_DIR/stackcollapse-perf.pl" | \
    "$FLAMEGRAPH_DIR/flamegraph.pl" --title "SumRandom — 8M elements, standalone" --colors hot \
    > "$OUTPUT_DIR/random.svg"

echo ""
echo "=== Done ==="
echo "Flame graphs: $OUTPUT_DIR/sequential.svg, $OUTPUT_DIR/random.svg"

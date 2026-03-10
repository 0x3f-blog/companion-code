#!/bin/bash
# Run BDN benchmarks and perf stat at each dataset size, per variant
# Shows how cache miss rate scales with working set size
# Usage: ./run-scaling.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

COUNTERS="cycles,instructions,cache-references,cache-misses,LLC-loads,LLC-load-misses"

echo "=== Building ==="
dotnet build -c Release "$PROJECT_DIR" --nologo -q

echo ""
echo "========================================="
echo "  BDN — All sizes (1M, 8M, 64M)"
echo "========================================="
dotnet run -c Release --project "$PROJECT_DIR" -- --filter '*'

echo ""
echo "========================================="
echo "  perf stat — per size, per variant"
echo "========================================="

BIN=$(find "$PROJECT_DIR"/bin/Release/net9.0 -name "hardware-counters" -type f | head -1)

for SIZE in 1000000 8000000 64000000; do
    for VARIANT in sequential random; do
        echo ""
        echo "--- $VARIANT @ $SIZE ---"
        perf stat -e "$COUNTERS" \
            "$BIN" "perf-${VARIANT}" "$SIZE" 2>&1
    done
done

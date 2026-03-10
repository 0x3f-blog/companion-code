#!/bin/bash
# perf stat wrapper — compare hardware counters between sequential and random access
# Uses standalone perf mode (PerfRunner) — no BDN overhead
# Usage: ./perf-stat.sh [N]
# Example: ./perf-stat.sh            # 8M elements (default)
#          ./perf-stat.sh 1000000     # 1M elements

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"

N="${1:-8000000}"

COUNTERS="cycles,instructions,cache-references,cache-misses,branch-instructions,branch-misses,L1-dcache-loads,L1-dcache-load-misses,LLC-loads,LLC-load-misses"

echo "=== Building ==="
dotnet build -c Release "$PROJECT_DIR" --nologo -q

BIN=$(find "$PROJECT_DIR"/bin/Release/net9.0 -name "hardware-counters" -type f | head -1)
if [ -z "$BIN" ]; then
    echo "ERROR: Cannot find compiled binary"
    exit 1
fi

echo ""
echo "========================================="
echo "  Sequential Access (N=$N)"
echo "========================================="
perf stat -e "$COUNTERS" \
    "$BIN" perf-sequential "$N" 2>&1

echo ""
echo "========================================="
echo "  Random Access (N=$N)"
echo "========================================="
perf stat -e "$COUNTERS" \
    "$BIN" perf-random "$N" 2>&1

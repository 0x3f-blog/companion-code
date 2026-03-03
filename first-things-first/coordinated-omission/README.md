# Coordinated Omission Demo

Companion code for [First Things First: Coordinated Omission](https://0x3f.blog/posts/first-things-first-coordinated-omission/).

Demonstrates how closed-loop load tests systematically omit measurements during failures — making dashboards look cleaner the worse the system performs.

## Quick start

```bash
dotnet run -c Release
```

## What it does

1. **Calibrates** a SpinWait loop for ~1ms baseline latency on your hardware
2. **Runs a closed-loop client** — send, wait for response, measure, repeat
3. **Runs an open-loop client** — constant rate, measures from intended start time
4. **Prints side-by-side comparison** — same system, same load, different measurement

Both clients hit the same simulated service: ~1ms baseline, 200ms pause every 500th request.

## Expected output

```
  Metric          Closed-loop      Open-loop      Ratio
  ------          -----------      ---------      -----
  Count                13,500         13,500
  p50                 1.00 ms        1.00 ms       1.0x
  p90                 1.00 ms      137.89 ms     137.4x
  p99                 1.07 ms      194.64 ms     182.4x
  p99.9             200.15 ms      200.15 ms       1.0x
  max               200.28 ms      200.41 ms       1.0x
```

Closed-loop p99 stays at 1 ms because only 27 out of 13,500 requests (0.2%) directly hit a pause. Open-loop p99 explodes to 195 ms because ~4,400 requests are measured from when they *should* have been sent.

## CLI options

| Flag | Default | Description |
|------|---------|-------------|
| `--rate` | 450 | Target requests per second |
| `--duration` | 30 | Test duration in seconds |
| `--pause-every` | 500 | Introduce pause every N requests |
| `--pause-ms` | 200 | Pause duration in milliseconds |

## Requirements

- .NET 9.0+
- HdrHistogram 2.5.0 (restored automatically)

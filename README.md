# White Mask: Phase Field

**A simulation of what it feels like to be looked at: each moment of observation "collapses" a face into one readable version of itself, and it slowly regains its own shape when left alone.**

Part of the **White Mask** series → see also [gazing-attention](https://github.com/lileinsteinbrain/gazing-attention) (physical mask) and [white-membrane-proto](https://github.com/lileinsteinbrain/white-membrane-proto) (XR prototype).

## The idea
Borrowing loosely from quantum measurement: unobserved, the mask holds many possible versions of itself (a fluid inner "phase field"). Being observed forces it into a single version that others can read. This Unity piece models that as two values that rise and fall over time:
- **coherence**: is the self still whole, or fragmented into other people's versions?
- **agency**: how much can the mask still decide who it is?

## How it works
- Observation happens **on purpose** (press G or click) or **at random** (a Poisson process with rate λ_M)
- When observed, coherence and agency drop quickly (λ_M); when the mask "reclaims" itself (press H), they recover slowly (λ_A)
- The outer face switches between **safe**, **exposed** and **abstract** materials; a custom shader renders the inner phase field with scan lines, distortion and fading that follow the current state
- **Calm mode** turns off randomness, so the dynamics can be shown step by step

## What's in this repo
| Path | What it is |
|---|---|
| `Assets/Scripts/WhiteMaskController.cs` | Main dynamics: observation, collapse, recovery, states |
| `Assets/Scripts/PhaseFieldController.cs` | Simpler first version of the phase-field model |
| `Assets/shaders/WhiteMask_Face_Quantum.shader` | Face shader driven by the state |
| `Assets/Scenes/PhaseFieldScene.unity` | Main scene |

## Open it
Unity **6000.0.30f1**. Open `Assets/Scenes/PhaseFieldScene.unity` and press Play; use **G** (gaze) and **H** (reclaim).

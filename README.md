# White Mask: Phase Field v1

## What this is
A Unity simulation that models identity under observation as a phase field. Being looked at "collapses" the mask into a single readable face; agency slowly restores coherence.

## Input
- Click / tap as an explicit act of observation (a "measurement")
- Stochastic observation events drawn from a Poisson process (rate λ_M)

## Processing
- `PhaseFieldController` tracks a coherence index ψ, with collapse strength λ_M, agency recovery λ_A and temporal noise η
- `WhiteMaskController` combines **agency** and **coherence** scalars: fast collapse when observed, slow recovery when safe, plus a breathing-like noise term

## Output
- A custom shader (`Assets/shaders/WhiteMask_Face_Quantum.shader`) renders the inner phase field
- The outer face switches between *safe*, *exposed* and *abstract* materials
- On-screen text reports the current state

## Project
- Unity **6000.0.30f1**
- Main scene: `Assets/Scenes/PhaseFieldScene.unity`

## Related
- [white-membrane-proto](https://github.com/lileinsteinbrain/white-membrane-proto)
- [gazing-attention](https://github.com/lileinsteinbrain/gazing-attention)

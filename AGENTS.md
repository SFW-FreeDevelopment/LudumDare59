# AGENTS.md

## Project

**Signal Scrubber** — a Ludum Dare 59 Unity jam game. Creepy sci-fi signal tuning at a retro CRT workstation. The player stabilizes strange transmissions by adjusting analog-style controls (frequency, noise filter, phase).

Tone: atmospheric, tactile, uncanny. Not horror, not action.

## Repository Layout

- `Unity/` — the Unity project (open this folder in Unity Hub).
- `README.md` — short project blurb.
- `PLAN.md` — implementation plan for the jam build (scope, systems, build order).

## Working Conventions

- **Jam scope first.** Prefer shipping over architecture. Don't build reusable systems where a quick script does. Three similar MonoBehaviours beat a premature base class.
- **One scene.** The game lives in a single fixed-camera scene of the desk/CRT. Resist adding scenes.
- **Visual feel > content volume.** Polish the CRT/tuning feedback loop before adding more signals.
- **Controls on the monitor frame.** No floating HUD. UI is diegetic — knobs, sliders, and the Lock button are attached to the CRT.
- **Screen imagery is creepy sci-fi, not gore or jumpscares.** Alien silhouettes, monoliths, diagrams, celestial symbols.

## Scope Guardrails

Must-haves:
- CRT with frame-mounted controls
- Messy desk composition
- Real-time tuning feedback (continuous, not binary)
- 3–6 authored signals, linear progression
- Lock Signal evaluation with success / partial / fail states

Avoid:
- Multi-scene flow, full 3D exploration, player movement
- Complex puzzle logic or large narrative systems
- More than ~3 tuning controls plus Lock

## Unity Notes

- Assume 2D/2.5D composition with a fixed camera.
- Drive CRT distortion via shader/material parameters bound to control values.
- Hidden transmission art lives under distortion layers (render texture or sprite stack).
- Keep one manager for signal progression; avoid DI frameworks.

## Git

- Main branch: `main`.
- Commits do **not** include `Co-Authored-By` (see [CLAUDE.md](CLAUDE.md) frontmatter).

## Current Plan

- [PLAN.md](PLAN.md) — high-level plan narrative.
- [ROADMAP.md](ROADMAP.md) — milestones, story sequencing, parallelization, ship gate.
- [ARCHITECTURE.md](ARCHITECTURE.md) — systems, shader contract, folder map, naming.
- [stories/](stories/README.md) — per-story work units; start here when picking up a task.

## External Contributors

- **Sprite art** is supplied by the user's wife — drop into `Assets/Art/<category>/` against the named slots defined in stories S04, S07, S19. Do not block on art; use placeholders and let the artist swap in.
- **SFX and music** are supplied by the user later — drop into `Assets/Audio/<category>/`. Code null-checks missing clips (see S14).
- **Everything else** (scene wiring, scripts, shaders, UI Toolkit, post-fx, integration) is Claude's responsibility.

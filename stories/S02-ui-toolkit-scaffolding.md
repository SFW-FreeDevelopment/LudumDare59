# S02 — UI Toolkit Scaffolding

**Milestone:** M0 — Bootstrap
**Priority:** 🟦 MVP
**Depends on:** S01

## Goal

Get UI Toolkit rendering in both diegetic (world-space on CRT) and overlay (screen-space) modes, with a shared theme stylesheet.

## Deliverables

1. Two `PanelSettings` assets in `Assets/UI/`:
   - `Diegetic.panelsettings` — World Space render mode, constant pixel size, scale so 1 UXML px ≈ 1 art px.
   - `Overlay.panelsettings` — Screen Space Overlay.
2. Two UXML documents in `Assets/UI/Documents/`:
   - `CrtFrame.uxml` — stub with a `<ui:Label text="TUNE THE SIGNAL" class="crt-title" />` and a `<ui:VisualElement name="controls-row" />`.
   - `Overlay.uxml` — stub with a `<ui:VisualElement name="fade" />`.
3. Three USS files in `Assets/UI/Styles/`:
   - `theme.uss` — font, colors (phosphor `#7CFF9E`, amber `#FFB35C`, cream `#E8E1C8`, CRT-bg `#0B0F0A`).
   - `crt.uss` — imports theme, styles `.crt-title`, `.crt-panel`, `.crt-label`.
   - `overlay.uss` — imports theme, styles full-screen fade.
4. Add one GameObject `UI/DiegeticUI` with a `UIDocument` component referencing `Diegetic.panelsettings` and `CrtFrame.uxml`.
5. Add one GameObject `UI/OverlayUI` with a `UIDocument` component referencing `Overlay.panelsettings` and `Overlay.uxml`.

## Acceptance Criteria

- [ ] Play mode shows "TUNE THE SIGNAL" rendered in the scene (world-space UIDocument on the CRT region — positioning can be rough for now).
- [ ] Overlay UIDocument exists and is invisible (fade element alpha=0).
- [ ] No console errors.

## Out of Scope

- No real controls yet (S05).
- No final positioning on the CRT (S03/S04 handle placement).

## Implementation Notes

- For UI Toolkit text, use the default runtime font initially. Swap to a CRT-style font in S17 if time allows.
- World-space UIDocument needs the `UIDocument`'s `Sort Order` and `Panel Settings > Scale Mode > Constant Pixel Size` tuned. Don't obsess — S04 places it precisely.
- Use `@import url("theme.uss");` at the top of `crt.uss` and `overlay.uss`.

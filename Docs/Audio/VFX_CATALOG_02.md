# KanjiBattle VFX Catalog 02 (reviewed)

Generated: 2026-09-03

## Main sheet

- File: `kanjibattle_vfx_catalog_02.png`
- Format: RGBA PNG
- Size: **1344 x 1152 px**
- Grid: **7 x 6 cells, exactly 192 x 192 px**
- The previous non-integer sheet remains available as version 01; use version 02 for Unity import.

## Number aura sheet

- File: `kanjibattle_vfx_number_auras_01.png`
- Format: RGBA PNG
- Size: **576 x 576 px**
- Grid: 3 columns x 3 rows, exactly 192 x 192 px
- Rows: lower (green), middle (yellow), upper (red)
- Columns: small, medium, maximum

## Unity import

- Texture Type: `Sprite (2D and UI)`
- Sprite Mode: `Multiple`
- Main sheet Grid Slice: `192 x 192 px`
- Aura sheet Grid Slice: `192 x 192 px`
- Pixels Per Unit: start at `100`
- Filter Mode: `Bilinear`
- Mesh Type: `Tight` for particles; `Full Rect` if glow edges clip
- Compression: platform-compressed after halo review
- Pivot: `Center`

## Preview review

`vfx_catalog_preview_white.png`, `vfx_catalog_preview_black.png`, and `vfx_catalog_preview_board.png` are quick composites for inspecting alpha halos. They are previews only and should not be imported as game assets.

## Scope note

The texture is a visual primitive catalog. Attack range and target selection remain code-defined: gun line, spear two cells, dragon 3x3, and soil trap placement must not be inferred from sprite boundaries.

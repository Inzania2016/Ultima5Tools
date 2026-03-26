# Map rendering notes

Current first-pass renderer targets these DAT families:

- `TOWNE.DAT`
- `DWELLING.DAT`
- `CASTLE.DAT`
- `KEEP.DAT`
- `BRIT.DAT`
- `UNDER.DAT`

## Output format

The renderer emits SVG files.

If a sibling `TILES.16` file is present, the renderer now loads and decompresses it before defining the tile graphics used by the SVG. `TILES.16` is wrapped in an Ultima-style LZW container whose first 4 bytes are the uncompressed length.

Why SVG first:

- zero third-party dependencies
- easy to inspect in a browser
- each tile can carry a tooltip with tile id and LOOK2 description
- simple to overlay NPC schedule markers

## Settlement DAT files

These are treated as sequences of 32x32 planes.

For the settlement families (`TOWNE`, `DWELLING`, `CASTLE`, `KEEP`), the renderer groups planes using the 8-byte start-index tables in `DATA.OVL`.

Each output SVG contains one named settlement section, with one panel per floor.

If a sibling `*.NPC` file is present, the renderer overlays NPC schedule stops:

- red = stop 0
- amber = stop 1
- teal = stop 2
- dashed gray line = route between visible stops on the same floor
- text label = NPC slot plus TLK identity (when a sibling `*.TLK` is available) anchored near stop 0

The renderer also emits a companion `*_npc.txt` file per settlement section with the full schedule table for active slots, including TLK identity when it can be resolved from the sibling `*.TLK` file.

## World DAT files

### `UNDER.DAT`

Rendered as a single 256x256 plane.

### `BRIT.DAT`

Rendered as a single 256x256 plane.

`BRIT.DAT` uses 16x16 chunks. Chunks flagged as all-water in the `DATA.OVL` chunking table are expanded back to tile `0x01` during rendering.

## World object overlays (`*.OOL`)

For `BRIT.DAT` and `UNDER.DAT`, the renderer now checks for sibling `*.OOL` files and overlays active raw records as magenta markers.

This is intentionally conservative:

- the first two bytes are used as x/y coordinates on the 256x256 world map
- the remaining 6 bytes are preserved as raw metadata in tooltips and companion summary output
- the overlay is treated as a first-pass dynamic world-object layer, not a fully decoded gameplay object schema yet

A companion `*_objects.txt` file is emitted for these world-map renders when an OOL overlay is present.

## Current limitations

- tile colors are heuristic and based mainly on `LOOK2.DAT` descriptions rather than original bitmap tiles
- `DUNGEON.DAT` is not rendered yet
- floor labels are conservative and inferred from plane counts rather than a fully decoded settlement-floor schema
- `BRIT.DAT` depends on a sibling `DATA.OVL`

## Planned upgrades

- decode `TILES.16` / `TILES.4` and render with actual game tile graphics
- add `DUNGEON.DAT` support
- add optional coordinate grid and tile id labels
- add current-hour highlighting once the schedule time bytes are decoded more precisely
- add sprite hints / NPC portrait metadata once tile and character graphics are decoded

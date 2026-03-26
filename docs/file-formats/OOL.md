# OOL Format Notes

## Current working model

`*.OOL` now has a **first-pass raw parser** and SVG overlay support for world maps.

Confirmed so far:

- records are fixed at **8 bytes** each
- `BRIT.OOL` and `UNDER.OOL` are **0x100 bytes** = **32 records**
- `SAVED.OOL` is **0x200 bytes** = **64 records**
- the `SAVED.OOL` layout is consistent with **two 0x100-byte segments**
- in the sample data, the back half of `SAVED.OOL` matches the active records seen in `UNDER.OOL`

Working interpretation:

- these files are very likely a **dynamic world-object / world-overlay list** for `BRIT.DAT` and `UNDER.DAT`
- the first two bytes of an active record are treated as **x/y coordinates** on the 256x256 world maps
- the remaining 6 bytes are preserved as raw metadata until their gameplay meaning is confirmed

## What the toolkit exposes today

For each record:

- slot index
- x/y bytes
- raw tail bytes `[2..7]`
- three 16-bit little-endian views over the tail bytes

Example rendering usage:

- `map render BRIT.DAT` now overlays active `BRIT.OOL` records when the sibling file is present
- `map render UNDER.DAT` now overlays active `UNDER.OOL` records when the sibling file is present
- `map render UNDER.DAT` falls back to `INIT.OOL` if `UNDER.OOL` is missing

The renderer also emits a companion `*_objects.txt` summary file for world maps.

## What is still inferred

These fields are **not** considered decoded yet:

- object kind / item id
- floor / level / world selector
- persistence flags
- ownership / quantity / state bytes
- exact relationship between `INIT.OOL`, `BRIT.OOL`, and save-state mutation beyond the obvious segment sizing

## Good next reverse-engineering targets from here

- compare `INIT.OOL` vs `SAVED.OOL` after controlled in-game object interactions
- trace the code paths that load/save the 0x100 and 0x200 blobs
- correlate active records against known world-item placements and underworld special locations

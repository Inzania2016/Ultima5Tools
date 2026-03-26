# OVL Format Notes

## Current toolkit scope

The toolkit currently focuses on `DATA.OVL` because it supports several other formats directly:

- TLK compressed-word dictionary
- map-start tables for TOWNE/DWELLING/CASTLE/KEEP data files
- location name index tables

## DATA.OVL tables currently surfaced

`ovl info` currently extracts:

- TLK compressed-word table
- TOWNE.DAT start-map indexes
- DWELLING.DAT start-map indexes
- CASTLE.DAT start-map indexes
- KEEP.DAT start-map indexes
- city name index table
- other location name index table

## Relationship to other tooling

- `tlk dump` auto-loads sibling `DATA.OVL` when present so dictionary tokens can be resolved.
- `npc dump` auto-loads sibling `DATA.OVL` when present so the relevant start-map indexes can be shown beside map sections.

## Still out of scope

- binary code disassembly of non-DATA overlays
- overlay loader emulation
- relocation or execution analysis for gameplay overlays

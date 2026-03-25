# Architecture Notes

## Phase separation

### Current phase: toolkit / archaeology

This phase focuses on reliable extraction and inspection of original Ultima V data structures.

### Future phase: engine/runtime

A future engine will consume stabilized parsing/models from this toolkit, but runtime behavior is intentionally deferred.

## Project responsibilities

- **U5.Core**
  - Binary IO helpers.
  - Format models/parsers (TLK, NPC, GAM, DAT metadata).
  - Formatters and utility services.
  - Rendering scaffold APIs.
- **U5.Cli**
  - Command routing.
  - Argument validation and exit codes.
  - Human-readable output.
- **U5.Tests**
  - Deterministic parser/diff tests with synthetic fixtures.
  - No hard dependency on proprietary original game files.

## Milestone-1 commands

- `tlk dump <path>`
- `npc dump <path>`
- `gam diff <leftPath> <rightPath>`
- `dat info <path>`
- `map render <path>` (stub only)

## Evidence policy

- Known findings are documented as known.
- Where semantics are incomplete, bytes are preserved and behavior is marked inferred/stubbed.
- Avoid speculative parser behavior that cannot be justified from observed data.

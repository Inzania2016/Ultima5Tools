# TLK Format (Current Toolkit Understanding)

## Known / strongly supported structure

- `uint16 npcCount`
- `npcCount` entries of:
  - `uint16 npcId`
  - `uint16 blockOffset`
- Followed by encoded NPC dialogue blocks.

## Current implementation scope

- Parse `npcCount`.
- Parse table entries.
- Compute inferred block sizes from next offset or EOF.
- Preserve raw bytes.

## Non-goals (for now)

- Full dialogue VM decoding.
- Scripted behavior interpretation.

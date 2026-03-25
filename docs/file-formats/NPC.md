# NPC Format (Current Toolkit Understanding)

## Working assumption

NPC files appear to be a fixed layout of:

- `256` records
- each record `18` bytes

## Current implementation scope

- Parse record stream in `18` byte chunks.
- Report whether total file length exactly matches `256 * 18`.
- Preserve raw bytes per record for forensic workflows.

## Non-goals (for now)

- Full semantic decode of all 18-byte fields.

# TLK Format

## Known structure

- `uint16 npcCount`
- `npcCount` table entries of:
  - `uint16 npcId`
  - `uint16 blockOffset`
- Followed by encoded NPC dialogue blocks

## Dialogue block phases

The toolkit currently treats each NPC block as three logical phases:

1. fixed entries
2. keyword groups
3. tail script / question-response nodes

### Fixed entries

The first five zero-terminated fields are interpreted as:

- Name
- Description
- Greeting
- Job
- Bye

### Keyword groups

Keyword groups are decoded as one or more keywords chained by `<OR>`, followed by an answer payload.

### Tail script

The tail area is now disassembled into:

- grouped keyword routes
- label references
- `0x90` script nodes
- default handlers
- response handlers
- action opcodes such as `TAKE_GOLD`, `JOIN_PARTY`, `CALL_GUARDS`, and the current karma/check candidates

## Current opcode ledger

High confidence:

- `0x82` = end dialogue
- `0x83` = pause
- `0x84` = join party
- `0x85` = take gold
- `0x86` = give item
- `0x87` = OR
- `0x88` = ask name
- `0x8B` = hostile / guards / combat trigger
- `0x8D` = newline
- `0x8F` = key wait
- `0x90` = begin script node

Working interpretations:

- `0x89` = karma / virtue gain
- `0x8A` = karma / virtue loss
- `0x8C` = if NPC knows the Avatar's name
- `0xFE` = token check / state or item check
- `0xFF` = fallthrough / no-op handler

## Output behavior

`tlk dump` now renders:

- fixed entries
- keyword groups
- tail routes
- structured script nodes
- opcode-aware action lists
- raw tail elements
- anomaly warnings where a simple mismatch pattern is detected

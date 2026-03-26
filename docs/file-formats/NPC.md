# NPC Format

## Current toolkit understanding

NPC files are treated as a fixed schedule table for 8 map sections.

## File layout

- Total size: `4608` bytes
- Sections: `8`
- Bytes per section: `576`
- Slots per section: `32`

Each section is parsed as:

```text
32 x 16-byte schedules
32 x type bytes
32 x dialog-number bytes
```

## Schedule layout

Each 16-byte schedule is interpreted as:

```text
AI[3]
X[3]
Y[3]
Z[3]   // signed bytes
Times[4]
```

The current toolkit renders each slot as three candidate stops and four transition times.

## Dialog numbers

The toolkit currently labels the known special values as:

- `0` = no response / silent NPC
- `129` = weapon dealer
- `130` = barkeeper
- `131` = horse seller
- `132` = ship seller
- `133` = magic seller
- `134` = guild master
- `135` = healer
- `136` = innkeeper
- `255` = guard / harassment script
- `1..128` = TLK entry reference

## Output behavior

`npc dump` now groups output by map and renders:

- map name
- active slot count
- slot type/dialog values
- schedule stops
- schedule times
- raw bytes

## Remaining unknowns

- semantic meaning of most type values
- semantic meaning of AI type values
- whether slot `0` has any engine-specific hidden purpose beyond being usually unused

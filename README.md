# Ultima5Tools

Ultima5Tools is a C#/.NET 10 toolkit for reverse-engineering and reconstructing data from **Ultima V**.

## Project phase

This repository is the **toolkit / archaeology phase**.

- In scope: parsing, dumping, diffing, and scaffolded rendering support for original game files.
- Out of scope: a modern runtime game engine.
- The parsing/model layer is designed so a future engine can reuse it.

## Repository layout

- `src/U5.Core` - format models, parsers, disassemblers, diffing, and formatting utilities.
- `src/U5.Cli` - command-line interface entry point and command routing.
- `src/U5.Tests` - deterministic bootstrap tests using synthetic fixtures.
- `docs/file-formats` - format notes for TLK/NPC/GAM/DAT/OVL.
- `docs/subsystem-notes` - architecture and subsystem notes.
- `samples` - optional local inputs for experimentation.
- `output` - optional generated dump/diff output.

## Build

```bash
dotnet build Ultima5Tools.sln
```

## Test

```bash
dotnet test Ultima5Tools.sln
```

## CLI usage

```bash
dotnet run --project src/U5.Cli -- tlk dump <path>
dotnet run --project src/U5.Cli -- npc dump <path>
dotnet run --project src/U5.Cli -- ovl info <path>
dotnet run --project src/U5.Cli -- gam diff <leftPath> <rightPath>
dotnet run --project src/U5.Cli -- dat info <path>
dotnet run --project src/U5.Cli -- map render <path>
```

## Working with original Ultima V files

Place copies of original data files under `samples/` (for example `samples/original/`) for local experimentation.

- Do not modify original game data files.
- Tests do not require original files.

Optional examples (if files are available locally):

```bash
dotnet run --project src/U5.Cli -- tlk dump samples/original/TOWNE.TLK
dotnet run --project src/U5.Cli -- npc dump samples/original/TOWNE.NPC
dotnet run --project src/U5.Cli -- ovl info samples/original/DATA.OVL
```

## Reverse-engineering status at this milestone

- TLK parsing now includes structured script-node disassembly, grouped OR-routes, and opcode-aware action rendering.
- NPC parsing now understands the 8-map / 32-slot schedule layout and renders per-map schedule data.
- DATA.OVL inspection now exposes the compressed-word table and key map-start tables used by TLK/NPC workflows.
- GAM parsing is raw-byte focused with conservative changed-range diffing.
- DAT support is metadata inspection only.
- Map rendering is scaffolding only.

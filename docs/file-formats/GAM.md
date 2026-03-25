# GAM Format (Current Toolkit Understanding)

## Working assumption

GAM files are fixed-size save blobs appropriate for binary diff workflows.

## Current implementation scope

- Read full file as raw bytes.
- Report file length.
- Diff two GAM files with contiguous changed ranges.
- Include old/new hex bytes for each changed range.

## Non-goals (for now)

- Full field-level semantic decode.

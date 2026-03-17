# v1.2 Release Notes

## Highlights

- Generation now uses a fresh runtime seed by default, so repeated runs can resample values within the configured ranges
- CLI generation now supports a --seed parameter so the same run can be reproduced when needed
- GUI generation now supports an optional seed input, including quick clear and reuse of the most recently used generation seed
- Updated README and bilingual user-facing docs to explain resampling behavior, fixed-seed generation, and the GUI seed workflow

## Validation

- Added an integration test to verify identical output when the same explicit seed is used
- Re-ran the existing test suite after the seed and documentation changes

## Current Follow-Up

- GUI and CLI both support fixed-seed reproduction; future polishing can focus on ergonomics rather than missing capability
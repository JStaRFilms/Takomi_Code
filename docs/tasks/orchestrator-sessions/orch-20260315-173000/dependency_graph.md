# Dependency Graph

```mermaid
flowchart TD
    T00["00 Intake And Baseline"] --> T01["01 Mockup Parity Audit"]
    T01 --> T02["02 Visual Shell Recovery"]
    T00 --> T03["03 Feature Test Matrix"]
    T02 --> T04["04 Chat And Session Validation"]
    T03 --> T04
    T02 --> T05["05 Orchestrator And Intervention Validation"]
    T03 --> T05
    T02 --> T06["06 Git And Worktree Validation"]
    T03 --> T06
    T02 --> T07["07 Billing Validation"]
    T03 --> T07
    T02 --> T08["08 Bags Validation"]
    T03 --> T08
    T02 --> T09["09 Runtime Target Validation"]
    T03 --> T09
    T02 --> T10["10 Persistence And Restart Validation"]
    T03 --> T10
    T04 --> T11["11 Consolidated Fix Loop"]
    T05 --> T11
    T06 --> T11
    T07 --> T11
    T08 --> T11
    T09 --> T11
    T10 --> T11
    T11 --> T12["12 Final Validation Gate"]
    T12 --> T13["13 Session Handoff"]
```

## Notes

- `02 Visual Shell Recovery` is an early hard gate because feature testing will otherwise generate noise from an obviously wrong shell.
- `04` through `10` are intended as sequentially reviewed validation slices, but can be delegated independently after the shell and test matrix are stable.
- `11` collects all confirmed defects and parity gaps into one controlled recovery loop.
- `12` is the only step allowed to declare the tested application ready for the next milestone.

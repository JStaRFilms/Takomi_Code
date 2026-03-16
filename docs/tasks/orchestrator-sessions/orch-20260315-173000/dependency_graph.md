# Dependency Graph

```mermaid
flowchart TD
    T00["00 Intake And Baseline"] --> T01["01 Mockup Parity Audit"]
    T01 --> T02["02 Visual Shell Recovery"]
    T00 --> T03["03 Feature Test Matrix"]
    T02 --> T04["04 Project Selector And Workspace Lifecycle"]
    T03 --> T04
    T04 --> T05["05 Chat, Sub-Session, And Transcript"]
    T02 --> T05
    T03 --> T05
    T05 --> T06["06 Codex Runtime And Streaming"]
    T02 --> T06
    T03 --> T06
    T05 --> T07["07 Orchestration Engine And Task Graph"]
    T06 --> T07
    T02 --> T07
    T03 --> T07
    T07 --> T08["08 Intervention Controls"]
    T02 --> T08
    T03 --> T08
    T05 --> T09["09 Git And Worktree"]
    T02 --> T09
    T03 --> T09
    T04 --> T10["10 Mode And Workflow Loader"]
    T07 --> T10
    T02 --> T10
    T03 --> T10
    T04 --> T11["11 Bags Verification Flow Audit"]
    T02 --> T11
    T03 --> T11
    T05 --> T12["12 Persistence And Restart"]
    T06 --> T12
    T07 --> T12
    T09 --> T12
    T10 --> T12
    T11 --> T12
    T04 --> T13["13 Consolidated Repair Loop"]
    T05 --> T13
    T06 --> T13
    T07 --> T13
    T08 --> T13
    T09 --> T13
    T10 --> T13
    T11 --> T13
    T12 --> T13
    T13 --> T14["14 Final Validation Gate"]
    T14 --> T15["15 Session Handoff And Replan Summary"]
```

## Notes

- `02 Visual Shell Recovery` remains an early hard gate because feature testing otherwise generates UI noise.
- `04` through `12` are now feature-specific validation slices, ordered to follow the actual product spine: workspace -> session -> runtime -> orchestration -> interventions -> workspace tooling -> persistence.
- `13` collects confirmed defects into one controlled repair loop after the higher-signal validation tasks complete.
- `14` is the only step allowed to declare the tested application ready for the next milestone.

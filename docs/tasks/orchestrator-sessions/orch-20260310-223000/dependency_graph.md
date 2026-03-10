# Dependency Graph

```mermaid
flowchart TD
    T00["00 Intake"] --> T01["01 Product Genesis"]
    T01 --> T02["02 Architecture Genesis"]
    T02 --> T03["03 Issue and Task Seed"]
    T03 --> T04["04 Design Direction"]
    T04 --> T05["05 Mockup Prototypes"]
    T05 --> T06["06 Design Review Loop"]
    T06 --> T07["07 Solution Scaffold"]
    T07 --> T08["08 Mode and Workflow Loader"]
    T07 --> T09["09 Chat and Session Core"]
    T07 --> T10["10 Codex Adapter Runtime"]
    T08 --> T11["11 Orchestrator Engine"]
    T09 --> T11
    T10 --> T11
    T11 --> T12["12 Intervention Controls"]
    T09 --> T13["13 Git and Workspace Controls"]
    T07 --> T14["14 Billing and Entitlements"]
    T07 --> T15["15 Bags Integration"]
    T10 --> T16["16 Cloud Runtime"]
    T11 --> T17["17 Quality Gate"]
    T12 --> T17
    T13 --> T17
    T14 --> T17
    T15 --> T17
    T16 --> T17
    T17 --> T18["18 Fix Loop"]
    T18 --> T19["19 Final Handoff"]
```

## Notes

- Design review is a hard gate before build work starts.
- Tasks `08`, `09`, and `10` can progress in parallel after scaffolding.
- Tasks `14`, `15`, and `16` may begin once the build foundation is stable enough to host integration code.
- `17` through `19` are sequential quality and closure stages.

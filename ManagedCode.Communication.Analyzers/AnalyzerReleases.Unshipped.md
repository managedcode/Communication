; Analyzer release tracking

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
MCC0003 | Usage | Error | Keep command and result envelopes separate
MCC0004 | Usage | Warning | Avoid nested result envelopes and redundant same-type reconstruction
MCC0005 | Usage | Warning | Use the native CQRS result conversion
MCC0006 | Usage | Warning | Use the CQRS payload factory
MCC0007 | Usage | Error | Terminal chunk factory contradicts its result status

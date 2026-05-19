# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.6.1] - 2026-05-19

### Added
- Asset-based hierarchical event bus system (`{TagName}EventBusAsset`).
- Strongly-typed `ScriptableObject` wrappers (`{TagName}So`) and set fields (`{TagName}SetField`) for Inspector usage.
- `{TagName}EventBusDispatcher` component for Inspector-based event wiring.
- New code generation options for namespace, class names, and folder paths.

### Changed
- Refactored `{TagName}` and `{TagName}Set` to use `NativeArray` for DOTS compatibility.
- Replaced static `HTagEventBus` with instance-based `HTagEventBusAsset`.
- Updated code generator to version 0.6.1.
- Complete documentation overhaul in README.md and Junie skills.

### Removed
- Obsolete static `HTagEventBus` and `HTags.EventBus` namespace.
- Obsolete `HTag.Is(...)` API.

## [0.5.0] - 2026-04-28

### This is the first release of *\<Hierarchical Tags\>*.

*Short description of this release*

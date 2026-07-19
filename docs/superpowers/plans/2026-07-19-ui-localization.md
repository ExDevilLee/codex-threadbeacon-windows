# Windows UI Localization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a shared, immediately switchable Simplified Chinese/English/System localization layer to the WPF app.

**Architecture:** Semantic language preferences live in the existing display-settings JSON. An observable language state resolves System once and publishes changes. WPF resource dictionaries provide static labels, while ViewModels refresh derived display text when the state changes.

**Tech Stack:** .NET 9, WPF ResourceDictionary, xUnit, System.Text.Json.

---

### Task 1: Language preference model

**Files:**
- Create: `src/ThreadBeacon.App/Localization/AppLanguage.cs`
- Create: `src/ThreadBeacon.App/Localization/AppLanguageResolver.cs`
- Modify: `src/ThreadBeacon.App/Settings/DisplaySettings.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Settings/JsonDisplaySettingsStoreTests.cs`
- Create: `tests/ThreadBeacon.App.Tests/Localization/AppLanguageResolverTests.cs`

- [x] Write failing tests for semantic values, invalid values, and Chinese/non-Chinese System resolution.
- [x] Run the focused tests and confirm they fail because the language API is absent.
- [x] Implement the enum, parser, resolver, and optional `Language` JSON property with System fallback.
- [x] Run the focused tests and the existing display-settings tests.

### Task 2: Shared language state and resource dictionaries

**Files:**
- Create: `src/ThreadBeacon.App/Localization/AppLanguageState.cs`
- Create: `src/ThreadBeacon.App/Resources/Strings.zh-Hans.xaml`
- Create: `src/ThreadBeacon.App/Resources/Strings.en.xaml`
- Modify: `src/ThreadBeacon.App/App.xaml`
- Modify: `src/ThreadBeacon.App/App.xaml.cs`
- Create: `tests/ThreadBeacon.App.Tests/Localization/AppLanguageStateTests.cs`

- [x] Write failing tests for immediate change notifications and invalid persisted preference handling.
- [x] Implement the state object and UI-thread-safe resource dictionary replacement.
- [x] Register the state once at app startup and expose it to both windows.
- [x] Run App.Tests and build the WPF project.

### Task 3: Settings language picker and static UI migration

**Files:**
- Modify: `src/ThreadBeacon.App/ViewModels/SettingsWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/SettingsWindow.xaml`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml`
- Modify: `src/ThreadBeacon.App/MainWindow.xaml.cs`
- Modify: `tests/ThreadBeacon.App.Tests/Views/SettingsWindowXamlTests.cs`

- [x] Add language options with semantic values and a two-way setting binding.
- [x] Replace static labels/tooltips with `DynamicResource` keys.
- [x] Subscribe ViewModels/windows to the shared language state so both open views update together.
- [x] Add fixture assertions for the language picker and representative resource bindings.
- [x] Run all tests, build, publish, and manually smoke-test both language choices.

### Task 4: Dynamic text migration and documentation

**Files:**
- Modify: `src/ThreadBeacon.App/ViewModels/DisplaySettingsViewModel.cs`
- Modify: `src/ThreadBeacon.App/ViewModels/MainWindowViewModel.cs`
- Modify: `src/ThreadBeacon.App/Controls/DataSourceHealthControl.xaml`
- Modify: `src/ThreadBeacon.App/Controls/TokenInfoControl.xaml`
- Modify: `src/ThreadBeacon.App/Controls/SubagentInfoControl.xaml`
- Modify: `README.md`
- Modify: `README-EN.md`
- Modify: `ROADMAP.md`

- [x] Add tests proving derived labels refresh after a language change without changing raw task data.
- [ ] Migrate remaining dynamic status, empty-state, health, Token, and Subagent row labels through the localization service.
- [x] Run the full test suite and `dotnet publish`.
- [x] Review the diff for secrets or machine-specific paths, then commit and push.

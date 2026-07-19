# Windows UI Localization Design

## Goal

Add Simplified Chinese, English, and System language selection to the WPF app with immediate updates across the main and settings windows.

## Constraints learned from macOS

- Persist semantic language values (`system`, `zh-Hans`, `en`), never translated labels.
- Resolve unsupported system languages to English deterministically.
- Keep task titles, agent aliases, model names, status codes, and raw Codex data unchanged.
- Use one observable language state for every open window so no view keeps stale text.
- Keep localization in `ThreadBeacon.App`; Core models remain language-neutral.

## Design

`AppLanguage` and `AppLanguageResolver` provide parsing, persistence-safe values, and system fallback. `AppLanguageState` owns the current preference and raises a change event. WPF resource dictionaries contain stable keys for Chinese and English and are swapped on the UI thread. ViewModels subscribe to the same state and raise property changes for derived display options and dynamic labels.

The first migration covers the settings window and the main window's static labels/tooltips. Dynamic task data remains verbatim; dynamic status text is migrated through the same localization service in the following pass without changing Core data contracts.

## Error handling and compatibility

Missing, invalid, or unreadable language settings use System. Missing resource keys fall back to English. Existing display settings remain backward compatible because the language value is an optional JSON property.

## Verification

Unit tests cover parsing, system fallback, invalid persistence, and language-change notifications. XAML fixture tests verify language selection and resource bindings. A Windows build and app smoke test verify the resources compile and both windows update without restart.

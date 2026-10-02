# Changelog

This file details all the changes for this package.

## Current Version

### Changes

- Add change log and meta files for license and change log
- Synchronize with Mojang/brigadier through 9ba4f13:
  - Add `ContextChain` for running a parsed command in stages against a supplied source; `CommandDispatcher.Execute` now runs through it
  - Add `ArgumentType.Parse(reader, source)`, which argument nodes now call and which defaults to `Parse(reader)`
  - Add `SuggestionContext.Context`; its constructor now takes the context builder first
  - Add `CommandDispatcher.SetConsumer`
- Add Editor tests ported from upstream

### Behavior Changes

- A command whose last context has no executable, such as an incomplete command behind a redirect, now throws "Unknown command" and reports the failure to the result consumer before any redirect modifier runs. It previously ran the modifiers and returned 0

### Bug Fixes

- Suggestions after a redirect are built from the redirected context, so suggestion providers can read that context's arguments
- `CommandContext.CopyFor` compares sources by reference, so a distinct source that compares equal is no longer replaced by the original, and a null source no longer throws

## 1.0

### Changes

- UPM Package created

### Bug Fixes
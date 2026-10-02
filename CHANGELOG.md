# Changelog

This file details all the changes for this package.

The major and minor version follow the Mojang/brigadier version this port is synchronized with. The patch number is this package's own.

That is why the version jumps from 1.0.1 straight to 1.3.0. Mojang did the same to Minecraft: Bedrock Edition's version numbers were pushed ahead until they lined up with Java Edition's at 1.16.

## 1.3.0

### Changes

- Raise the declared minimum Unity version to 2021.3
- Synchronize with Mojang/brigadier through 9ba4f13:
  - Add `ContextChain` for running a parsed command in stages against a supplied source; `CommandDispatcher.Execute` now runs through it
  - Add `ArgumentType.Parse(reader, source)`, which argument nodes now call and which defaults to `Parse(reader)`
  - Add `SuggestionContext.Context`; its constructor now takes the context builder first
  - Add `CommandDispatcher.SetConsumer`
- Add Editor tests ported from upstream
- The package name is now `com.minervagamestudio.brigadier`

### Behavior Changes

- A command whose last context has no executable, such as an incomplete command behind a redirect, now throws "Unknown command" and reports the failure to the result consumer before any redirect modifier runs. It previously ran the modifiers and returned 0

### Bug Fixes

- Suggestions after a redirect are built from the redirected context, so suggestion providers can read that context's arguments
- `CommandContext.CopyFor` compares sources by reference, so a distinct source that compares equal is no longer replaced by the original, and a null source no longer throws

## 1.0.1

### Changes

- Add change log and meta files for license and change log

## 1.0

### Changes

- UPM Package created

### Bug Fixes
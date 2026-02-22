# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CompositeKey is a .NET source generation library that produces optimal parsing and formatting code for composite identifiers (partition key + sort key patterns common in NoSQL databases like DynamoDB).

## Build & Test Commands

```bash
dotnet build -c Release                    # Build all projects
dotnet test -c Release                     # Run all tests
dotnet pack src/CompositeKey -c Release    # Create NuGet package

# Run specific test projects
dotnet test src/CompositeKey.SourceGeneration.UnitTests -c Release
dotnet test src/CompositeKey.Analyzers.Common.UnitTests -c Release
dotnet test src/CompositeKey.Analyzers.UnitTests -c Release
dotnet test src/CompositeKey.SourceGeneration.FunctionalTests -c Release

# Run a single test by fully-qualified name
dotnet test src/CompositeKey.SourceGeneration.UnitTests -c Release --filter "FullyQualifiedName~TestMethodName"
```

Requires .NET SDK 10.0 (see `global.json`). CI also tests against net8.0 and net9.0.

## Architecture

The solution is split into four main projects plus corresponding test projects:

**CompositeKey** (`src/CompositeKey/`) — Public API surface. Contains `[CompositeKey]` attribute, `[CompositeKeyConstructor]` attribute, and the `IPrimaryKey<TSelf>` / `ICompositePrimaryKey<TSelf>` interfaces. This is the NuGet package users install; analyzers and source generator ship embedded inside it.

**CompositeKey.SourceGeneration** (`src/CompositeKey.SourceGeneration/`) — Incremental source generator implementing `IIncrementalGenerator`. Three key phases:
- `SourceGenerator.cs` — Entry point; uses `ForAttributeWithMetadataName` for incremental pipeline
- `SourceGenerator.Parser.cs` — Extracts attribute data, validates types, builds `GenerationSpec` model
- `SourceGenerator.Emitter.cs` — Generates `ToString()`, `Parse()`, `TryParse()`, `ToPartitionKeyString()`, `ToSortKeyString()`, partial formatting methods, and `ISpanParsable<TSelf>` implementations

**CompositeKey.Analyzers.Common** (`src/CompositeKey.Analyzers.Common/`) — Shared validation logic used by both the source generator and IDE analyzers. Contains `TemplateStringTokenizer`, type/template/property validation, and all `DiagnosticDescriptor` definitions (COMPOSITE0001–COMPOSITE0008+).

**CompositeKey.Analyzers** (`src/CompositeKey.Analyzers/`) — IDE-time analyzers for real-time feedback: `TypeStructureAnalyzer`, `TemplateStringAnalyzer`, `PropertyAnalyzer`.

## Key Domain Concepts

**Template string**: The format pattern in `[CompositeKey("{Prop1}#{Prop2:N}")]`. Tokenized into key parts: `PropertyKeyPart`, `ConstantKeyPart`, `DelimiterKeyPart`, `PrimaryDelimiterKeyPart`, `RepeatingPropertyKeyPart`.

**PrimaryKeySeparator**: Optional separator in the template that divides partition key from sort key, enabling `ToPartitionKeyString()` / `ToSortKeyString()` generation.

**Repeating sections**: `{Collection...delimiter}` syntax for `IReadOnlyList<T>`, `List<T>`, `ImmutableArray<T>` properties.

**FormatType / ParseType**: Enums that control code generation strategy per property type (string, guid, enum, int, etc.) with fast-path optimizations for strings and enums.

## Build Configuration

- **TreatWarningsAsErrors**: enabled (except CS0618)
- **Nullable**: enabled globally
- **C# LangVersion**: 12
- **RestorePackagesWithLockFile**: true (uses `packages.lock.json` files)
- **Central Package Management**: via `Directory.Packages.props`
- Analyzer projects target both `net8.0` and `netstandard2.0`

## Benchmarks

**CompositeKey.Benchmarks** (`src/CompositeKey.Benchmarks/`) — BenchmarkDotNet benchmarks for generated code performance. Manual, developer-run (not part of CI). Covers `ToString`, `Parse`, `TryParse`, and partition/sort key methods across representative key types exercising all major code-generation paths.

```bash
dotnet run -c Release --project src/CompositeKey.Benchmarks -- --filter "*"
```

## Conventions

- Commit messages follow Conventional Commits (`feat:`, `fix:`, `perf:`, etc.) — GitVersion and `.versionrc` drive versioning and changelog
- EditorConfig: 4-space indent, UTF-8, LF line endings, 120-char max line length
- Test stack: xunit + Shouldly + AutoFixture + Verify (snapshot testing)
- Unit tests use `CompilationHelper` to create in-memory Roslyn compilations
- Functional tests define real key types and test format/parse round-trips
- Snapshot tests (`Snapshots/` in `SourceGeneration.UnitTests`) use
  Verify.SourceGenerators to capture generated `.g.cs` output and
  `GenerationSpec` models as `.verified.*` baselines — any change to
  emitted code or parser output will cause a snapshot diff failure
- `*.received.*` files are gitignored; only `*.verified.*` baselines
  are committed
- The `ModuleInitializer` scrubs the non-deterministic
  `GeneratedCodeAttribute` version string to keep snapshots stable

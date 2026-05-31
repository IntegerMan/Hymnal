# Getting Started With Hymnal

This guide helps you run Hymnal locally and open your first manuscript workspace.

## Prerequisites

- Git
- .NET SDK 10.0 preview (required by the project target framework)
- A desktop environment supported by Avalonia

Check your SDK version:

```bash
dotnet --version
```

If you do not have a .NET 10 preview SDK installed, install it first before continuing.

## 1) Clone The Repository

```bash
git clone <your-fork-or-repo-url>
cd Hymnal
```

## 2) Restore Dependencies

```bash
dotnet restore Hymnal.sln
```

## 3) Build

```bash
dotnet build src/Hymnal/Hymnal.csproj
```

## 4) Run Tests

```bash
dotnet test Hymnal.sln
```

Expected current baseline in this repo:

- 31 tests total
- 31 passed
- 0 failed

## 5) Run The Desktop App

```bash
dotnet run --project src/Hymnal/Hymnal.csproj
```

When the app opens, use File > Open Workspace to select a manuscript folder.

## 6) Create A Minimal Workspace

Hymnal needs a Book.txt manifest. Create this structure:

```text
MyManuscript/
  Book.txt
  chapter-one.md
  chapter-two.md
```

Book.txt content example:

```text
chapter-one.md
chapter-two.md
```

Optional part marker example:

```text
part-one/part.md
part-one/chapter-one.md
```

In part-one/part.md:

```text
{class: part}
# Part One
```

## 7) Use Core Editor Features

- Select a chapter in the left sidebar.
- Edit chapter content in the center editor.
- Save with Ctrl+S.
- Toggle sidebar with Ctrl+B.
- Toggle notes panel with F4.

Notes are saved automatically after idle typing to:

- .hymnal-data/notes/

## Troubleshooting

### Error: Book.txt not found

Hymnal only accepts Book.txt at one of these locations:

- workspace-root/Book.txt
- workspace-root/manuscript/Book.txt

### dotnet test fails with "Specify which project or solution file to use"

Run tests against the solution explicitly:

```bash
dotnet test Hymnal.sln
```

### .NET SDK version issues

If build fails due to target framework net10.0, install the .NET 10 preview SDK and retry.

## Development Notes

- UI project: src/Hymnal
- Core logic: src/Hymnal.Core
- Tests: tests/Hymnal.Core.Tests
- App settings path on Windows: %AppData%/Hymnal/settings.json

## Next Reading

- Project overview: README.md

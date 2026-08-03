# S02: Workspace Open and Book.txt Parsing — UAT

**Milestone:** M001
**Written:** 2026-05-29T15:01:00.228Z

# S02: Workspace Open and Book.txt Parsing — UAT

**Milestone:** M001
**Written:** 2026-05-29

## UAT Type

- UAT mode: artifact-driven
- Why this mode is sufficient: The slice is primarily a wiring and parsing foundation, and its contracts are already exercised by passing unit tests plus structural verification of the shell, DI graph, and workspace persistence path.

## Preconditions

- The S02 implementation files are present and the five task summaries report `verification_result: passed`.
- A manuscript workspace such as `C:/Dev/EliAndGraceMakeAGame` is available for manual launch smoke testing.
- The app shell is built from the S01 foundation and can resolve the registered workspace services.

## Smoke Test

Open the sample manuscript workspace and confirm the sidebar renders the Part/Chapter tree in Book.txt order, then relaunch the app and confirm the same workspace is restored automatically.

## Test Cases

### 1. Open workspace and render tree

1. Launch the app.
2. Open `C:/Dev/EliAndGraceMakeAGame` as the workspace.
3. Observe the sidebar tree.
4. **Expected:** The Part/Chapter hierarchy renders in Book.txt order, with missing files represented consistently.

### 2. Restore the last workspace on launch

1. Open a workspace and close the app.
2. Relaunch the app.
3. **Expected:** The last opened workspace is restored without reselecting the folder.

### 3. React to Book.txt changes

1. Open a workspace.
2. Modify `Book.txt` externally.
3. **Expected:** The workspace reload path is triggered and the user is informed that the workspace content changed.

## Edge Cases

### Missing or malformed Book.txt

1. Open a folder that lacks a valid Book.txt.
2. **Expected:** The workspace load fails gracefully with a user-facing notification rather than crashing.

### Rapid repeated file changes

1. Edit Book.txt several times in quick succession.
2. **Expected:** Reload activity is debounced so the UI does not thrash or duplicate notifications.

## Failure Signals

- The sidebar is empty when a valid workspace is opened.
- The app forgets the last workspace after relaunch.
- File changes do not trigger reload behavior.
- Workspace loading crashes instead of surfacing a notification.

## Not Proven By This UAT

- Full interactive desktop confirmation of the reload banner in a live runtime session.
- Visual confirmation of the folder picker dialog and manual workspace selection flow.
- Terminal-level `dotnet build` confirmation in the Windows environment outside the gsd_exec sandbox.

## Notes for Tester

The artifact evidence already confirms the core implementation shape: parsing, ordered tree binding, persistence, and DI wiring. The remaining live smoke checks should focus on the user-visible shell behavior rather than code-level correctness.

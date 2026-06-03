namespace Hymnal.ViewModels;

/// <summary>
/// High-level centre-panel navigation modes for the main shell.
/// Write is the default authoring surface; Manage hosts the Gantt view.
/// Research, Plan, and Edit are reserved for later expansion.
/// </summary>
public enum ShellMode
{
    Research,
    Plan,
    Write,
    Manage,
    Edit,
}

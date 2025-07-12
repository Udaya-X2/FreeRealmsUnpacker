namespace UnpackerGui.Models;

/// <summary>
/// Represents a preference category.
/// </summary>
/// <param name="Name">The name of the preference.</param>
/// <param name="Description">The description of the preference.</param>
public record Preference(string Name, string Description);

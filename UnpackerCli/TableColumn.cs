namespace UnpackerCli;

/// <summary>
/// Stores column information in a <see cref="Table"/>.
/// </summary>
internal class TableColumn
{
    /// <summary>
    /// The name of the column.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// The width of the column.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Returns the name of the column.
    /// </summary>
    /// <returns>The name of the column.</returns>
    public override string ToString() => Name;
}

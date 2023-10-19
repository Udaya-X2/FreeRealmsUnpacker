namespace UnpackerCli;

/// <summary>
/// Represents a table of strings.
/// </summary>
public class Table
{
    private const string NonNegNumRequired = "Non-negative number required.";
    private const string NotEnoughRowValues = "Not enough values in the specified array to add a row.";
    private const int DefaultCapacity = 4;

    private readonly TableColumn[] _columns;
    private readonly List<string[]> _rows;

    /// <summary>
    /// Initializes a new instance of the <see cref="Table"/> class with the specified column names.
    /// </summary>
    public Table(params string[] columns) : this(DefaultCapacity, columns)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Table"/> class
    /// with the specified initial row capacity and column names.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ArgumentNullException"/>
    public Table(int capacity, params string[] columns)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), capacity, NonNegNumRequired);
        if (columns == null) throw new ArgumentNullException(nameof(columns));

        _columns = new TableColumn[columns.Length];
        _rows = new List<string[]>(capacity);

        for (int i = 0; i < columns.Length; i++)
        {
            string column = columns[i] ?? "";
            _columns[i] = new TableColumn { Name = column, Width = column.Length };
        }
    }

    /// <summary>
    /// Gets the number of rows in the table.
    /// </summary>
    /// <returns>The number of rows in the table.</returns>
    public int Count => _rows.Count;

    /// <summary>
    /// Adds a row with the specified values to the end of the <see cref="Table"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException"/>
    /// <exception cref="ArgumentException"/>
    public void AddRow(params object[] values)
    {
        if (values == null) throw new ArgumentNullException(nameof(values));
        if (values.Length < _columns.Length) throw new ArgumentException(NotEnoughRowValues);

        string[] row = new string[values.Length];

        for (int i = 0; i < values.Length; i++)
        {
            string cell = row[i] = values[i].ToString() ?? "";
            _columns[i].Width = Math.Max(_columns[i].Width, cell.Length);
        }

        _rows.Add(row);
    }

    /// <summary>
    /// Prints the table to the standard output stream.
    /// </summary>
    public void Print()
    {
        string rowFormat = GetRowFormat();
        string[] emptyArgs = CreateEmptyStringArray(_columns.Length);
        string emptyFormat = string.Format(rowFormat, emptyArgs)[1..^1];

        Console.WriteLine(CreateTableDivider(emptyFormat, '┌', '┬', '┐'));
        Console.WriteLine(string.Format(rowFormat, _columns));
        Console.WriteLine(CreateTableDivider(emptyFormat, '├', '┼', '┤'));
        _rows.ForEach(args => Console.WriteLine(string.Format(rowFormat, args)));
        Console.WriteLine(CreateTableDivider(emptyFormat, '└', '┴', '┘'));
    }

    /// <summary>
    /// Returns the format string for a table row.
    /// </summary>
    /// <returns>The format string for a table row.</returns>
    private string GetRowFormat()
    {
        string[] args = new string[_columns.Length];

        for (int i = 0; i < args.Length; i++)
        {
            args[i] = $"{{{i},-{_columns[i].Width}}}";
        }

        return $"│ {string.Join(" │ ", args)} │";
    }

    /// <summary>
    /// Returns an array of empty strings with the specified length.
    /// </summary>
    /// <returns>An array of empty strings with the specified length.</returns>
    private static string[] CreateEmptyStringArray(int length)
    {
        string[] emptyStrings = new string[length];

        for (int i = 0; i < length; i++)
        {
            emptyStrings[i] = string.Empty;
        }

        return emptyStrings;
    }

    /// <summary>
    /// Converts the row format string to a table divider with the specified <paramref name="start"/>,
    /// <paramref name="end"/>, and <paramref name="inter"/> chars at each intersection point.
    /// </summary>
    /// <returns>
    /// A table divider string with the specified <paramref name="start"/>, <paramref name="end"/>,
    /// and <paramref name="inter"/> chars at each intersection point.
    /// </returns>
    private static string CreateTableDivider(string format, char start, char inter, char end)
        => $"{start}{format.Replace('│', inter).Replace(' ', '─')}{end}";
}

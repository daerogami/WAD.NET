namespace WAD.NET.ZScript.Formatting;

/// <summary>
/// Options that control how ZScript source code is formatted.
/// </summary>
public class FormattingOptions
{
    /// <summary>
    /// The default formatting options.
    /// </summary>
    public static FormattingOptions Default { get; } = new FormattingOptions();

    /// <summary>
    /// The number of spaces per indentation level. Defaults to 4.
    /// </summary>
    public int IndentSize { get; init; } = 4;

    /// <summary>
    /// Whether to use tabs for indentation. Defaults to true.
    /// </summary>
    public bool UseTabs { get; init; } = true;

    /// <summary>
    /// Whether to insert a space after commas. Defaults to true.
    /// </summary>
    public bool SpaceAfterComma { get; init; } = true;

    /// <summary>
    /// Whether to insert spaces around binary operators. Defaults to true.
    /// </summary>
    public bool SpaceAroundBinaryOperators { get; init; } = true;

    /// <summary>
    /// Whether to place the opening brace on a new line. Defaults to true.
    /// </summary>
    public bool NewLineBeforeOpenBrace { get; init; } = true;

    /// <summary>
    /// Whether to indent case labels inside switch statements. Defaults to true.
    /// </summary>
    public bool IndentCaseLabels { get; init; } = true;
}

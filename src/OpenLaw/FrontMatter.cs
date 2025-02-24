namespace Clarius.OpenLaw;

/// <summary>
/// Front matter format to use when generating markdown files.
/// </summary>
public enum FrontMatter
{
    /// <summary>
    /// No front-matter will be included at all.
    /// </summary>
    None = 0,
    /// <summary>
    /// Use double-dash wrapping `--`.
    /// </summary>
    /// <remarks>
    /// This wrapping is useful to have GitHub display 
    /// the YAML when rendering markdown files.
    /// </remarks>
    Markdown,
    /// <summary>
    /// Use HTML comment wrapping `<!-- -->`.
    /// </summary>
    /// <remarks>
    /// This wrapping is useful to hide the YAML when 
    /// GitHub renders markdown files.
    /// </remarks>
    Html
}

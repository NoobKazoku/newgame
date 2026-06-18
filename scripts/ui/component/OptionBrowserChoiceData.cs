namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     浏览器内候选值的数据描述。
/// </summary>
public sealed class OptionBrowserChoiceData
{
    /// <summary>
    ///     候选值。
    /// </summary>
    public int Value { get; init; }

    /// <summary>
    ///     候选显示文本。
    /// </summary>
    public string Label { get; init; } = string.Empty;
}
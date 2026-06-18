namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     通用选项浏览器的数据项，负责描述一条可筛选、可分页的配置记录。
/// </summary>
public sealed class OptionBrowserEntryData
{
    /// <summary>
    ///     条目唯一键。
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    ///     分类唯一键。
    /// </summary>
    public string CategoryId { get; init; } = string.Empty;

    /// <summary>
    ///     分类显示文本。
    /// </summary>
    public string CategoryLabel { get; init; } = string.Empty;

    /// <summary>
    ///     标题文本。
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    ///     用于搜索命中的归一化文本。
    /// </summary>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    ///     说明文本。
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    ///     悬浮提示文本。
    /// </summary>
    public string TooltipText { get; init; } = string.Empty;

    /// <summary>
    ///     当前编辑值文本。
    /// </summary>
    public string CurrentValueText { get; init; } = string.Empty;

    /// <summary>
    ///     当前局内实际生效值文本。
    /// </summary>
    public string AppliedValueText { get; init; } = string.Empty;

    /// <summary>
    ///     默认值文本。
    /// </summary>
    public string DefaultValueText { get; init; } = string.Empty;

    /// <summary>
    ///     范围文本。
    /// </summary>
    public string RangeText { get; init; } = string.Empty;

    /// <summary>
    ///     推荐值文本。
    /// </summary>
    public string RecommendedValueText { get; init; } = string.Empty;

    /// <summary>
    ///     运行时生效策略文本。
    /// </summary>
    public string RuntimeStateText { get; init; } = string.Empty;

    /// <summary>
    ///     当前条目是否允许编辑。
    /// </summary>
    public bool CanEdit { get; init; } = true;

    /// <summary>
    ///     编辑器类型。
    /// </summary>
    public OptionBrowserEditorKind EditorKind { get; init; } = OptionBrowserEditorKind.None;

    /// <summary>
    ///     数值编辑器的当前值。
    /// </summary>
    public double NumericValue { get; init; }

    /// <summary>
    ///     数值编辑器最小值。
    /// </summary>
    public double MinValue { get; init; }

    /// <summary>
    ///     数值编辑器最大值。
    /// </summary>
    public double MaxValue { get; init; }

    /// <summary>
    ///     数值编辑器步进。
    /// </summary>
    public double Step { get; init; } = 1d;

    /// <summary>
    ///     是否使用整数取整。
    /// </summary>
    public bool Rounded { get; init; } = true;

    /// <summary>
    ///     选项编辑器的候选项。
    /// </summary>
    public IReadOnlyList<OptionBrowserChoiceData> Choices { get; init; } = Array.Empty<OptionBrowserChoiceData>();

    /// <summary>
    ///     当前选中的候选值。
    /// </summary>
    public int SelectedChoiceValue { get; init; }

    /// <summary>
    ///     数值变更回调。
    /// </summary>
    public Action<double>? NumericValueChanged { get; init; }

    /// <summary>
    ///     枚举变更回调。
    /// </summary>
    public Action<int>? ChoiceValueChanged { get; init; }
}
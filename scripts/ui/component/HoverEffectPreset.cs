namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     悬停效果预设档位，用于快速区分主按钮、次按钮和工具按钮的反馈强度。
/// </summary>
public enum HoverEffectPreset
{
    /// <summary>
    ///     主按钮，高对比与较强悬停反馈。
    /// </summary>
    Primary = 0,

    /// <summary>
    ///     次按钮，中等悬停反馈。
    /// </summary>
    Secondary = 1,

    /// <summary>
    ///     工具按钮，较轻的悬停反馈。
    /// </summary>
    Tool = 2,

    /// <summary>
    ///     自定义参数，直接使用组件上的导出值。
    /// </summary>
    Custom = 3
}
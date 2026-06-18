using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     顶部停靠动画面板。
/// </summary>
public partial class TopDockPanel : BaseAnimatedDockPanel
{
    /// <summary>
    ///     顶部栏切换按钮浮在内容外侧，不再单独占一整块高度。
    /// </summary>
    protected override bool ReserveLayoutSpaceForToggleButton => false;

    public TopDockPanel()
    {
        ExpandedButtonText = "▲";
        CollapsedButtonText = "▼";
    }

    protected override bool IsHorizontalDock => false;

    protected override Vector2 GetExpandedPanelPosition(Vector2 panelSize)
    {
        return new Vector2(VisualPadding, VisualPadding);
    }

    protected override Vector2 GetCollapsedTranslation(float panelOffset)
    {
        return new Vector2(0f, -panelOffset);
    }

    protected override Vector2 GetTogglePosition(
        Vector2 panelPosition,
        Vector2 panelSize,
        Vector2 toggleSize,
        float gap,
        float toggleCrossPosition
    )
    {
        return new Vector2(toggleCrossPosition, panelPosition.Y + panelSize.Y + gap);
    }
}

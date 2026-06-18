using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     左侧停靠动画面板。
/// </summary>
public partial class LeftDockPanel : BaseAnimatedDockPanel
{
    public LeftDockPanel()
    {
        ExpandedButtonText = "◀";
        CollapsedButtonText = "▶";
    }

    protected override bool IsHorizontalDock => true;

    protected override Vector2 GetExpandedPanelPosition(Vector2 panelSize)
    {
        return new Vector2(VisualPadding, VisualPadding);
    }

    protected override Vector2 GetCollapsedTranslation(float panelOffset)
    {
        return new Vector2(-panelOffset, 0f);
    }

    protected override Vector2 GetTogglePosition(
        Vector2 panelPosition,
        Vector2 panelSize,
        Vector2 toggleSize,
        float gap,
        float toggleCrossPosition
    )
    {
        return new Vector2(panelPosition.X + panelSize.X + gap, toggleCrossPosition);
    }
}
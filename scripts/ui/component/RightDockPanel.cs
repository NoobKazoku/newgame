using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     右侧停靠动画面板。
/// </summary>
public partial class RightDockPanel : BaseAnimatedDockPanel
{
    public RightDockPanel()
    {
        ExpandedButtonText = "▶";
        CollapsedButtonText = "◀";
    }

    protected override bool IsHorizontalDock => true;

    protected override Vector2 GetExpandedPanelPosition(Vector2 panelSize)
    {
        return new Vector2(Size.X - VisualPadding - panelSize.X, VisualPadding);
    }

    protected override Vector2 GetCollapsedTranslation(float panelOffset)
    {
        return new Vector2(panelOffset, 0f);
    }

    protected override Vector2 GetTogglePosition(
        Vector2 panelPosition,
        Vector2 panelSize,
        Vector2 toggleSize,
        float gap,
        float toggleCrossPosition
    )
    {
        return new Vector2(panelPosition.X - toggleSize.X - gap, toggleCrossPosition);
    }
}
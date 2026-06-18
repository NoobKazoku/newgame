using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     底部停靠动画面板。
/// </summary>
public partial class BottomDockPanel : BaseAnimatedDockPanel
{
    public BottomDockPanel()
    {
        ExpandedButtonText = "▼";
        CollapsedButtonText = "▲";
    }

    protected override bool IsHorizontalDock => false;

    protected override Vector2 GetExpandedPanelPosition(Vector2 panelSize)
    {
        return new Vector2(VisualPadding, Size.Y - VisualPadding - panelSize.Y);
    }

    protected override Vector2 GetCollapsedTranslation(float panelOffset)
    {
        return new Vector2(0f, panelOffset);
    }

    protected override Vector2 GetTogglePosition(
        Vector2 panelPosition,
        Vector2 panelSize,
        Vector2 toggleSize,
        float gap,
        float toggleCrossPosition
    )
    {
        return new Vector2(toggleCrossPosition, panelPosition.Y - toggleSize.Y - gap);
    }
}
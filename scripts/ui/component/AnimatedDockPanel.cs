using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     兼容旧场景的通用停靠动画面板。
/// </summary>
public partial class AnimatedDockPanel : BaseAnimatedDockPanel
{
    private DockEdge _edge = DockEdge.Left;

    public AnimatedDockPanel()
    {
        ApplyDefaultToggleText(_edge, _edge, true);
    }

    /// <summary>
    ///     停靠方向。
    /// </summary>
    [Export]
    public DockEdge Edge
    {
        get => _edge;
        set
        {
            if (_edge == value)
                return;

            var previousEdge = _edge;
            _edge = value;
            ApplyDefaultToggleText(previousEdge, _edge);
            if (!IsInsideTree())
                return;

            RefreshLayout();
            SetExpanded(IsExpanded, false);
        }
    }

    protected override bool IsHorizontalDock => Edge is DockEdge.Left or DockEdge.Right;

    protected override Vector2 GetExpandedPanelPosition(Vector2 panelSize)
    {
        return Edge switch
        {
            DockEdge.Right => new Vector2(Size.X - VisualPadding - panelSize.X, VisualPadding),
            DockEdge.Bottom => new Vector2(VisualPadding, Size.Y - VisualPadding - panelSize.Y),
            _ => new Vector2(VisualPadding, VisualPadding)
        };
    }

    protected override Vector2 GetCollapsedTranslation(float panelOffset)
    {
        return Edge switch
        {
            DockEdge.Left => new Vector2(-panelOffset, 0f),
            DockEdge.Right => new Vector2(panelOffset, 0f),
            DockEdge.Top => new Vector2(0f, -panelOffset),
            DockEdge.Bottom => new Vector2(0f, panelOffset),
            _ => Vector2.Zero
        };
    }

    protected override Vector2 GetTogglePosition(
        Vector2 panelPosition,
        Vector2 panelSize,
        Vector2 toggleSize,
        float gap,
        float toggleCrossPosition
    )
    {
        return Edge switch
        {
            DockEdge.Left => new Vector2(panelPosition.X + panelSize.X + gap, toggleCrossPosition),
            DockEdge.Right => new Vector2(panelPosition.X - toggleSize.X - gap, toggleCrossPosition),
            DockEdge.Top => new Vector2(toggleCrossPosition, panelPosition.Y + panelSize.Y + gap),
            DockEdge.Bottom => new Vector2(toggleCrossPosition, panelPosition.Y - toggleSize.Y - gap),
            _ => panelPosition
        };
    }

    private void ApplyDefaultToggleText(DockEdge previousEdge, DockEdge nextEdge, bool force = false)
    {
        var previousExpandedText = GetExpandedArrow(previousEdge);
        var previousCollapsedText = GetCollapsedArrow(previousEdge);
        if (force ||
            string.IsNullOrEmpty(ExpandedButtonText) ||
            string.Equals(ExpandedButtonText, previousExpandedText, StringComparison.Ordinal))
            ExpandedButtonText = GetExpandedArrow(nextEdge);

        if (force ||
            string.IsNullOrEmpty(CollapsedButtonText) ||
            string.Equals(CollapsedButtonText, previousCollapsedText, StringComparison.Ordinal))
            CollapsedButtonText = GetCollapsedArrow(nextEdge);
    }

    private static string GetExpandedArrow(DockEdge edge)
    {
        return edge switch
        {
            DockEdge.Right => "▶",
            DockEdge.Top => "▲",
            DockEdge.Bottom => "▼",
            _ => "◀"
        };
    }

    private static string GetCollapsedArrow(DockEdge edge)
    {
        return edge switch
        {
            DockEdge.Right => "◀",
            DockEdge.Top => "▼",
            DockEdge.Bottom => "▲",
            _ => "▶"
        };
    }
}

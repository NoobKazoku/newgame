using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     停靠动画面板基类，负责挂载内容面板、管理切换按钮和播放展开收起动画。
/// </summary>
[ContextAware]
[Log]
public abstract partial class BaseAnimatedDockPanel : Control, IController
{
    /// <summary>
    ///     展开状态变化时触发。
    /// </summary>
    [Signal]
    public delegate void ExpandedChangedEventHandler(bool isExpanded);

    /// <summary>
    ///     动画结束时触发。
    /// </summary>
    [Signal]
    public delegate void TransitionFinishedEventHandler(bool isExpanded);

    private Control? _attachedPanel;
    private bool _isLayoutReady;
    private Vector2 _measuredPanelSize;
    private Vector2 _measuredToggleSize;
    private float _progress;
    private bool _targetVisible = true;
    private Tween? _transitionTween;

    [GetNode] protected Control ClipRoot = null!;
    [GetNode] protected Control ContentRoot = null!;
    [GetNode] protected Control PanelRoot = null!;
    [GetNode] protected Button ToggleButton = null!;

    /// <summary>
    ///     动画预设。
    /// </summary>
    [Export]
    public DockMotionPreset MotionPreset { get; set; } = DockMotionPreset.Slide;

    /// <summary>
    ///     初始是否展开。
    /// </summary>
    [Export]
    public bool StartExpanded { get; set; } = true;

    /// <summary>
    ///     是否显示切换按钮。
    /// </summary>
    [Export]
    public bool ShowToggleButton { get; set; } = true;

    /// <summary>
    ///     收起状态下保留的面板可见尺寸。
    /// </summary>
    [Export(PropertyHint.Range, "0,256,1")]
    public float CollapsedVisibleSize { get; set; }

    /// <summary>
    ///     是否根据挂载内容自动刷新布局。
    /// </summary>
    [Export]
    public bool AutoMeasureFromContent { get; set; } = true;

    /// <summary>
    ///     是否阻止面板区域外的鼠标事件穿透。
    /// </summary>
    [Export]
    public bool BlockMouseOutsidePanel { get; set; }

    /// <summary>
    ///     动画时长。
    /// </summary>
    [Export(PropertyHint.Range, "0.05,1.00,0.01")]
    public float AnimationDuration { get; set; } = 0.18f;

    /// <summary>
    ///     动画过渡类型。
    /// </summary>
    [Export]
    public Tween.TransitionType TransitionType { get; set; } = Tween.TransitionType.Quart;

    /// <summary>
    ///     动画缓动方式。
    /// </summary>
    [Export]
    public Tween.EaseType EaseType { get; set; } = Tween.EaseType.Out;

    /// <summary>
    ///     展开状态下按钮文案。
    /// </summary>
    [Export]
    public string ExpandedButtonText { get; set; } = string.Empty;

    /// <summary>
    ///     收起状态下按钮文案。
    /// </summary>
    [Export]
    public string CollapsedButtonText { get; set; } = string.Empty;

    /// <summary>
    ///     展开状态下按钮提示。
    /// </summary>
    [Export]
    public string ExpandedTooltipText { get; set; } = string.Empty;

    /// <summary>
    ///     收起状态下按钮提示。
    /// </summary>
    [Export]
    public string CollapsedTooltipText { get; set; } = string.Empty;

    /// <summary>
    ///     切换按钮的最小尺寸。
    /// </summary>
    [Export]
    public Vector2 ToggleButtonMinimumSize { get; set; } = Vector2.Zero;

    /// <summary>
    ///     切换按钮在交叉轴上的对齐方式。
    /// </summary>
    [Export]
    public DockCrossAlignment ToggleCrossAlignment { get; set; } = DockCrossAlignment.Center;

    /// <summary>
    ///     切换按钮在交叉轴上的内边距。
    /// </summary>
    [Export(PropertyHint.Range, "0,128,1")]
    public float ToggleCrossInset { get; set; }

    /// <summary>
    ///     面板在组件内部保留的可视安全边距，避免边框和阴影被裁切。
    /// </summary>
    [Export(PropertyHint.Range, "0,64,1")]
    public float VisualPadding { get; set; } = 12f;

    /// <summary>
    ///     切换按钮与面板主体之间的间距。
    /// </summary>
    [Export(PropertyHint.Range, "0,32,1")]
    public float TogglePanelGap { get; set; } = 4f;

    /// <summary>
    ///     当前是否处于展开状态。
    /// </summary>
    public bool IsExpanded { get; private set; }

    /// <summary>
    ///     当前方向是否为横向停靠。
    /// </summary>
    protected abstract bool IsHorizontalDock { get; }

    /// <summary>
    ///     是否需要为切换按钮在宿主尺寸中额外预留布局空间。
    /// </summary>
    protected virtual bool ReserveLayoutSpaceForToggleButton => true;

    /// <summary>
    ///     获取面板在完全展开时的位置。
    /// </summary>
    /// <param name="panelSize">面板尺寸。</param>
    /// <returns>面板位置。</returns>
    protected abstract Vector2 GetExpandedPanelPosition(Vector2 panelSize);

    /// <summary>
    ///     获取收起状态下相对展开位置的平移量。
    /// </summary>
    /// <param name="panelOffset">主轴上的收起偏移量。</param>
    /// <returns>收起平移量。</returns>
    protected abstract Vector2 GetCollapsedTranslation(float panelOffset);

    /// <summary>
    ///     获取切换按钮位置。
    /// </summary>
    /// <param name="panelPosition">面板位置。</param>
    /// <param name="panelSize">面板尺寸。</param>
    /// <param name="toggleSize">按钮尺寸。</param>
    /// <param name="gap">按钮与面板间距。</param>
    /// <param name="toggleCrossPosition">按钮在交叉轴上的位置。</param>
    /// <returns>按钮位置。</returns>
    protected abstract Vector2 GetTogglePosition(
        Vector2 panelPosition,
        Vector2 panelSize,
        Vector2 toggleSize,
        float gap,
        float toggleCrossPosition
    );

    /// <summary>
    ///     获取内容挂载根节点。
    /// </summary>
    /// <returns>内容根节点。</returns>
    public Control GetContentRoot()
    {
        return ContentRoot;
    }

    /// <summary>
    ///     将外部内容面板挂载到组件内部。
    /// </summary>
    /// <param name="panel">需要挂载的面板。</param>
    public void AttachPanel(Control panel)
    {
        if (_attachedPanel == panel && panel.GetParent() == ContentRoot)
        {
            RefreshLayout();
            return;
        }

        if (_attachedPanel is not null)
            _attachedPanel.Resized -= OnAttachedPanelResized;

        _attachedPanel = panel;
        _attachedPanel.Resized += OnAttachedPanelResized;
        _attachedPanel.Reparent(ContentRoot);
        NormalizeAttachedPanelLayout(_attachedPanel);
        RefreshLayout();
    }

    /// <summary>
    ///     设置展开状态。
    /// </summary>
    /// <param name="expanded">目标展开状态。</param>
    /// <param name="animate">是否播放动画。</param>
    public void SetExpanded(bool expanded, bool animate = true)
    {
        var changed = IsExpanded != expanded;
        IsExpanded = expanded;

        UpdateToggleVisual();
        if (!_isLayoutReady)
        {
            _progress = expanded ? 1f : 0f;
            return;
        }

        if (!changed)
            return;

        if (!Visible || !animate)
        {
            StopTransition();
            _progress = expanded ? 1f : 0f;
            ApplyProgress(_progress);
        }
        else
        {
            StartProgressTransition(expanded ? 1f : 0f);
        }

        EmitSignalExpandedChanged(expanded);
    }

    /// <summary>
    ///     切换展开状态。
    /// </summary>
    /// <param name="animate">是否播放动画。</param>
    public void Toggle(bool animate = true)
    {
        SetExpanded(!IsExpanded, animate);
    }

    /// <summary>
    ///     设置组件整体可见性。
    /// </summary>
    /// <param name="visible">是否显示。</param>
    /// <param name="animate">是否播放动画。</param>
    public void SetPanelVisible(bool visible, bool animate = true)
    {
        if (_targetVisible == visible)
            return;

        _targetVisible = visible;
        StopTransition();
        if (!animate)
        {
            Visible = visible;
            SetModulateAlpha(1f);
            if (visible)
                RefreshLayout();

            return;
        }

        if (visible)
        {
            var wasHidden = !Visible;
            Visible = true;
            RefreshLayout();
            if (wasHidden) SetModulateAlpha(0f);
            _transitionTween = CreateTween();
            _transitionTween.SetTrans(TransitionType);
            _transitionTween.SetEase(EaseType);
            _transitionTween.TweenProperty(this, "modulate:a", 1f, AnimationDuration);
            _transitionTween.Finished += () =>
            {
                SetModulateAlpha(1f);
                _transitionTween = null;
            };
            return;
        }

        _transitionTween = CreateTween();
        _transitionTween.SetTrans(TransitionType);
        _transitionTween.SetEase(EaseType);
        _transitionTween.TweenProperty(this, "modulate:a", 0f, AnimationDuration);
        _transitionTween.Finished += () =>
        {
            Visible = false;
            SetModulateAlpha(1f);
            _transitionTween = null;
        };
    }

    /// <summary>
    ///     刷新布局测量结果。
    /// </summary>
    public void RefreshLayout()
    {
        if (!IsInsideTree())
            return;

        EnsureAttachedPanelReference();
        EnsureToggleButtonParent();
        ApplyMouseFilter();
        ApplyToggleVisualMode();

        _measuredToggleSize = MeasureToggleSize();
        ToggleButton.CustomMinimumSize = _measuredToggleSize;
        _measuredPanelSize = MeasurePanelSize();

        PanelRoot.Size = _measuredPanelSize;
        ContentRoot.Size = _measuredPanelSize;

        if (AutoMeasureFromContent)
        {
            var requiredSize = CalculateRequiredSize();
            EnsureHostSize(requiredSize);
            CustomMinimumSize = requiredSize;
            Size = new Vector2(Mathf.Max(Size.X, requiredSize.X), Mathf.Max(Size.Y, requiredSize.Y));
        }

        _isLayoutReady = true;
        _progress = IsExpanded ? 1f : 0f;
        ApplyProgress(_progress);
    }

    /// <summary>
    ///     初始化内部节点与布局。
    /// </summary>
    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        __BindNodeSignals_Generated();
        EnsureToggleButtonParent();
        _targetVisible = Visible;
        IsExpanded = StartExpanded;
        _progress = IsExpanded ? 1f : 0f;
        ApplyMouseFilter();
        ApplyToggleVisualMode();
        UpdateToggleVisual();
        CallDeferred(nameof(RefreshLayout));
    }

    /// <summary>
    ///     节点退出场景树时清理动画和事件。
    /// </summary>
    public override void _ExitTree()
    {
        StopTransition();
        if (_attachedPanel is not null)
            _attachedPanel.Resized -= OnAttachedPanelResized;

        __UnbindNodeSignals_Generated();
    }

    [BindNodeSignal(nameof(ToggleButton), nameof(Button.Pressed))]
    private void OnToggleButtonPressed()
    {
        if (!ShowToggleButton)
            return;

        Toggle();
    }

    private void OnAttachedPanelResized()
    {
        RefreshLayout();
    }

    private void EnsureAttachedPanelReference()
    {
        if (_attachedPanel is not null)
            return;

        for (var i = 0; i < ContentRoot.GetChildCount(); i++)
        {
            if (ContentRoot.GetChild(i) is not Control child)
                continue;

            _attachedPanel = child;
            _attachedPanel.Resized += OnAttachedPanelResized;
            NormalizeAttachedPanelLayout(_attachedPanel);
            return;
        }
    }

    private static void NormalizeAttachedPanelLayout(Control panel)
    {
        var measuredSize = panel.Size;
        if (measuredSize == Vector2.Zero)
            measuredSize = panel.GetCombinedMinimumSize();

        panel.SetAnchorsPreset(LayoutPreset.TopLeft);
        panel.Position = Vector2.Zero;
        panel.Size = measuredSize;
    }

    private void EnsureToggleButtonParent()
    {
        if (ToggleButton.GetParent() == this)
            return;

        ToggleButton.Reparent(this);
        MoveChild(ToggleButton, GetChildCount() - 1);
    }

    private void StartProgressTransition(float targetProgress)
    {
        StopTransition();
        _transitionTween = CreateTween();
        _transitionTween.SetTrans(TransitionType);
        _transitionTween.SetEase(EaseType);
        _transitionTween.TweenMethod(Callable.From<float>(ApplyProgress), _progress, targetProgress, AnimationDuration);
        _transitionTween.Finished += () =>
        {
            _progress = targetProgress;
            _transitionTween = null;
            EmitSignalTransitionFinished(IsExpanded);
        };
    }

    private void StopTransition()
    {
        _transitionTween?.Kill();
        _transitionTween = null;
    }

    private void SetModulateAlpha(float alpha)
    {
        Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, alpha);
    }

    private void ApplyProgress(float progress)
    {
        _progress = Mathf.Clamp(progress, 0f, 1f);

        var panelExtent = GetPrimaryAxis(_measuredPanelSize);
        var toggleCrossSize = ShowToggleButton ? GetCrossAxis(_measuredToggleSize) : 0f;
        var gap = ShowToggleButton ? TogglePanelGap : 0f;
        var panelOffset = Mathf.Max(0f, panelExtent + VisualPadding - CollapsedVisibleSize) * (1f - _progress);
        var crossAvailableSize = Mathf.Max(0f, GetCrossAxis(Size) - VisualPadding * 2f);
        var toggleCrossPosition = VisualPadding + GetToggleCrossPosition(crossAvailableSize, toggleCrossSize);
        var panelPosition = GetExpandedPanelPosition(_measuredPanelSize) + GetCollapsedTranslation(panelOffset);
        var togglePosition = GetTogglePosition(panelPosition, _measuredPanelSize, _measuredToggleSize, gap,
            toggleCrossPosition);

        PanelRoot.Position = panelPosition;
        ToggleButton.Position = togglePosition;

        var panelAlpha = MotionPreset switch
        {
            DockMotionPreset.Fade => _progress,
            DockMotionPreset.SlideAndFade => _progress,
            _ => 1f
        };
        PanelRoot.Modulate = new Color(PanelRoot.Modulate, panelAlpha);
        PanelRoot.Visible = _progress > 0f || MotionPreset == DockMotionPreset.Slide || CollapsedVisibleSize > 0f;
    }

    private Vector2 MeasurePanelSize()
    {
        if (_attachedPanel is not null)
        {
            var measuredSize = _attachedPanel.Size;
            if (measuredSize == Vector2.Zero)
                measuredSize = _attachedPanel.GetCombinedMinimumSize();

            return measuredSize;
        }

        var fallback = ContentRoot.GetCombinedMinimumSize();
        if (fallback == Vector2.Zero)
            fallback = Size;

        return fallback;
    }

    private Vector2 MeasureToggleSize()
    {
        if (!ShowToggleButton)
            return Vector2.Zero;

        var measuredSize = ToggleButtonMinimumSize;
        if (measuredSize == Vector2.Zero)
            measuredSize = ToggleButton.GetCombinedMinimumSize();

        if (measuredSize == Vector2.Zero)
            measuredSize = new Vector2(36f, 36f);

        return measuredSize;
    }

    private Vector2 CalculateRequiredSize()
    {
        var panelExtent = GetPrimaryAxis(_measuredPanelSize);
        var panelCrossSize = GetCrossAxis(_measuredPanelSize);
        var shouldReserveToggleSpace = ShowToggleButton && ReserveLayoutSpaceForToggleButton;
        var toggleExtent = shouldReserveToggleSpace ? GetPrimaryAxis(_measuredToggleSize) : 0f;
        var toggleCrossSize = shouldReserveToggleSpace ? GetCrossAxis(_measuredToggleSize) : 0f;
        var gap = shouldReserveToggleSpace ? TogglePanelGap : 0f;
        var requiredExtent = panelExtent + toggleExtent + gap + VisualPadding * 2f;
        var requiredCrossSize = Mathf.Max(panelCrossSize, toggleCrossSize) + VisualPadding * 2f;
        return IsHorizontalDock
            ? new Vector2(requiredExtent, requiredCrossSize)
            : new Vector2(requiredCrossSize, requiredExtent);
    }

    private void EnsureHostSize(Vector2 requiredSize)
    {
        var widthDelta = Mathf.Max(0f, requiredSize.X - Size.X);
        var heightDelta = Mathf.Max(0f, requiredSize.Y - Size.Y);

        if (widthDelta > 0f && Mathf.IsEqualApprox(AnchorLeft, AnchorRight))
        {
            if (Mathf.IsEqualApprox(AnchorLeft, 1f))
            {
                OffsetLeft -= widthDelta;
            }
            else if (Mathf.IsEqualApprox(AnchorLeft, 0f))
            {
                OffsetRight += widthDelta;
            }
            else
            {
                OffsetLeft -= widthDelta * 0.5f;
                OffsetRight += widthDelta * 0.5f;
            }
        }

        if (heightDelta > 0f && Mathf.IsEqualApprox(AnchorTop, AnchorBottom))
        {
            if (Mathf.IsEqualApprox(AnchorTop, 1f))
            {
                OffsetTop -= heightDelta;
            }
            else if (Mathf.IsEqualApprox(AnchorTop, 0f))
            {
                OffsetBottom += heightDelta;
            }
            else
            {
                OffsetTop -= heightDelta * 0.5f;
                OffsetBottom += heightDelta * 0.5f;
            }
        }
    }

    private void ApplyMouseFilter()
    {
        MouseFilter = BlockMouseOutsidePanel ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private void ApplyToggleVisualMode()
    {
        ToggleButton.Visible = ShowToggleButton;
        ToggleButton.FocusMode = ShowToggleButton ? FocusModeEnum.All : FocusModeEnum.None;
        ToggleButton.MouseFilter = ShowToggleButton ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore;
    }

    private void UpdateToggleVisual()
    {
        if (!ShowToggleButton)
            return;

        ToggleButton.Text = IsExpanded ? ExpandedButtonText : CollapsedButtonText;
        ToggleButton.TooltipText = IsExpanded ? ExpandedTooltipText : CollapsedTooltipText;
    }

    private float GetPrimaryAxis(Vector2 size)
    {
        return IsHorizontalDock ? size.X : size.Y;
    }

    private float GetCrossAxis(Vector2 size)
    {
        return IsHorizontalDock ? size.Y : size.X;
    }

    private float GetToggleCrossPosition(float availableSize, float toggleSize)
    {
        return ToggleCrossAlignment switch
        {
            DockCrossAlignment.Start => ToggleCrossInset,
            DockCrossAlignment.End => Mathf.Max(ToggleCrossInset, availableSize - toggleSize - ToggleCrossInset),
            _ => Mathf.Max(0f, (availableSize - toggleSize) * 0.5f)
        };
    }
}

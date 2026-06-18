using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     通用确认弹窗，统一处理项目内确认/取消类交互。
/// </summary>
[ContextAware]
[Log]
[GetAll]
public partial class ConfirmDialog : Control, IController
{
    /// <summary>
    ///     用户点击取消或按下返回键时触发。
    /// </summary>
    [Signal]
    public delegate void CanceledEventHandler();

    /// <summary>
    ///     用户点击确认时触发。
    /// </summary>
    [Signal]
    public delegate void ConfirmedEventHandler();

    [GetNode] private PanelContainer _accentBar = null!;

    [GetNode("Backdrop")] private ColorRect _backdrop = null!;

    [GetNode] private Button _cancelButton = null!;

    [GetNode] private Button _confirmButton = null!;

    [GetNode("CenterContainer/DialogMargin/DialogPanel")]
    private PanelContainer _dialogPanel = null!;

    [GetNode] private Label _messageLabel = null!;

    private Tween? _openTween;

    [GetNode] private Label _titleLabel = null!;

    /// <summary>
    ///     确认按钮是否使用危险动作样式。
    /// </summary>
    [Export]
    public bool UseDangerConfirmStyle { get; set; }

    /// <summary>
    ///     当前弹窗是否处于打开状态。
    /// </summary>
    public bool IsOpen => Visible;

    /// <summary>
    ///     节点就绪时注入依赖并初始化按钮样式。
    /// </summary>
    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        __BindNodeSignals_Generated();
        Visible = false;
        ApplyConfirmButtonVariation();
    }

    /// <summary>
    ///     节点退出树时解绑内部信号。
    /// </summary>
    public override void _ExitTree()
    {
        __UnbindNodeSignals_Generated();
    }

    /// <summary>
    ///     当弹窗可见时，优先消费取消键并关闭弹窗。
    /// </summary>
    /// <param name="event">输入事件。</param>
    public override void _Input(InputEvent @event)
    {
        if (!Visible || !@event.IsActionPressed("ui_cancel")) return;

        CancelAndClose();
        AcceptEvent();
    }

    /// <summary>
    ///     配置弹窗文案和确认按钮样式。
    /// </summary>
    /// <param name="title">标题文本。</param>
    /// <param name="message">正文文本。</param>
    /// <param name="confirmText">确认按钮文本。</param>
    /// <param name="cancelText">取消按钮文本。</param>
    /// <param name="useDangerConfirmStyle">确认按钮是否使用危险样式。</param>
    public void Configure(
        string title,
        string message,
        string confirmText,
        string cancelText,
        bool useDangerConfirmStyle = false)
    {
        _titleLabel.Text = title;
        _messageLabel.Text = message;
        _confirmButton.Text = confirmText;
        _cancelButton.Text = cancelText;
        UseDangerConfirmStyle = useDangerConfirmStyle;
        ApplyConfirmButtonVariation();
    }

    /// <summary>
    ///     打开弹窗并聚焦确认按钮。
    /// </summary>
    public void Open()
    {
        StopOpenTween();
        Visible = true;
        _backdrop.Modulate = new Color(1f, 1f, 1f, 0f);
        _dialogPanel.PivotOffset = _dialogPanel.Size / 2f;
        _dialogPanel.Scale = new Vector2(0.97f, 0.97f);
        _dialogPanel.Modulate = new Color(1f, 1f, 1f, 0f);
        _openTween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _openTween.TweenProperty(_backdrop, "modulate:a", 1f, 0.12f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _openTween.Parallel().TweenProperty(_dialogPanel, "scale", Vector2.One, 0.16f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        _openTween.Parallel().TweenProperty(_dialogPanel, "modulate:a", 1f, 0.14f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _confirmButton.GrabFocus();
    }

    /// <summary>
    ///     关闭弹窗。
    /// </summary>
    public void Close()
    {
        StopOpenTween();
        Visible = false;
    }

    /// <summary>
    ///     若弹窗处于打开状态，则按“取消”语义关闭并发出取消事件。
    /// </summary>
    /// <returns>如果当前成功处理取消则返回 <see langword="true" />。</returns>
    public bool TryCancel()
    {
        if (!Visible) return false;

        CancelAndClose();
        return true;
    }

    [BindNodeSignal(nameof(_confirmButton), nameof(Button.Pressed))]
    private void OnConfirmButtonPressed()
    {
        Close();
        EmitSignalConfirmed();
    }

    [BindNodeSignal(nameof(_cancelButton), nameof(Button.Pressed))]
    private void OnCancelButtonPressed()
    {
        CancelAndClose();
    }

    private void CancelAndClose()
    {
        Close();
        EmitSignalCanceled();
    }

    private void ApplyConfirmButtonVariation()
    {
        _confirmButton.ThemeTypeVariation = UseDangerConfirmStyle
            ? "ConfirmDialogDangerButton"
            : "ConfirmDialogPrimaryButton";
        _cancelButton.ThemeTypeVariation = "ConfirmDialogGhostButton";
        _accentBar.ThemeTypeVariation = UseDangerConfirmStyle
            ? "ConfirmDialogDangerAccentBar"
            : "ConfirmDialogPrimaryAccentBar";
    }

    private void StopOpenTween()
    {
        _openTween?.Kill();
        _openTween = null;
    }
}
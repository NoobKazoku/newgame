using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     组合式按钮悬停效果组件，可递归为目标节点下的按钮接入轻量缩放反馈。
/// </summary>
public partial class HoverEffect : Node
{
    private const string ManagedMetaKey = "_coregrid_hover_effect_managed";

    private readonly Dictionary<BaseButton, ButtonBinding> _bindings = new();
    private readonly Dictionary<BaseButton, Tween> _scaleTweens = new();
    private HoverProfile _resolvedProfile = HoverProfile.FromCustom(1.03f, 0.08f);

    /// <summary>
    ///     目标控件；为空时默认使用父节点上的 <see cref="Control" />。
    /// </summary>
    [Export]
    public Control? Target { get; set; }

    /// <summary>
    ///     预设档位；使用 <see cref="HoverEffectPreset.Custom" /> 时读取下方自定义参数。
    /// </summary>
    [Export]
    public HoverEffectPreset Preset { get; set; } = HoverEffectPreset.Custom;

    /// <summary>
    ///     悬停时的目标缩放倍率。
    /// </summary>
    [Export]
    public float HoverScale { get; set; } = 1.03f;

    /// <summary>
    ///     是否启用缩放效果。
    /// </summary>
    [Export]
    public bool EnableScale { get; set; } = true;

    /// <summary>
    ///     为兼容旧场景导出而保留；按钮样式现在完全由 Godot Theme 负责。
    /// </summary>
    [Export]
    public bool EnableBorderGlow { get; set; } = true;

    /// <summary>
    ///     是否递归处理目标节点下的所有按钮。
    /// </summary>
    [Export]
    public bool ApplyToDescendants { get; set; } = true;

    /// <summary>
    ///     悬停动画时长。
    /// </summary>
    [Export]
    public float AnimationDuration { get; set; } = 0.08f;

    /// <summary>
    ///     为兼容旧场景导出而保留；按钮样式现在完全由 Godot Theme 负责。
    /// </summary>
    [Export]
    public Color GlowBorderColor { get; set; } = new(0.98f, 0.87f, 0.56f);

    /// <summary>
    ///     为兼容旧场景导出而保留；按钮样式现在完全由 Godot Theme 负责。
    /// </summary>
    [Export]
    public int AdditionalShadowSize { get; set; } = 6;

    /// <summary>
    ///     组件就绪时绑定目标按钮，并监听后续动态加入的节点。
    /// </summary>
    public override void _Ready()
    {
        Target ??= GetParent() as Control;
        if (Target is null)
        {
            GD.PushWarning($"{nameof(HoverEffect)} 需要一个 Control 作为目标节点。");
            return;
        }

        _resolvedProfile = ResolveProfile();
        BindTarget(Target);
        if (GetTree() is { } tree) tree.NodeAdded += OnTreeNodeAdded;
    }

    /// <summary>
    ///     节点退出树时解绑所有按钮事件并恢复原始缩放状态。
    /// </summary>
    public override void _ExitTree()
    {
        if (GetTree() is { } tree) tree.NodeAdded -= OnTreeNodeAdded;

        foreach (var button in _bindings.Keys.ToArray()) UnbindButton(button);
    }

    /// <summary>
    ///     处理运行时动态加入树的节点，确保后续创建的按钮也能获得悬停效果。
    /// </summary>
    /// <param name="node">新增节点。</param>
    private void OnTreeNodeAdded(Node node)
    {
        if (Target is null || !Target.IsInsideTree()) return;

        if (node == Target || Target.IsAncestorOf(node)) BindTarget(node);
    }

    /// <summary>
    ///     根据配置绑定目标节点本身或其子树中的按钮。
    /// </summary>
    /// <param name="node">待处理的节点。</param>
    private void BindTarget(Node node)
    {
        if (ApplyToDescendants)
        {
            BindButtonsRecursive(node);
            return;
        }

        if (node == Target && node is BaseButton button) BindButton(button);
    }

    /// <summary>
    ///     递归绑定节点树中的所有按钮。
    /// </summary>
    /// <param name="node">根节点。</param>
    private void BindButtonsRecursive(Node node)
    {
        if (node is BaseButton button) BindButton(button);

        foreach (var child in node.GetChildren()) BindButtonsRecursive(child);
    }

    /// <summary>
    ///     为单个按钮接入悬停逻辑。
    /// </summary>
    /// <param name="button">目标按钮。</param>
    private void BindButton(BaseButton button)
    {
        if (button is OptionButton ||
            button.HasMeta(ManagedMetaKey) ||
            !button.IsInsideTree() ||
            HasDedicatedHoverEffect(button))
            return;

        var binding = new ButtonBinding(
            button.Scale,
            () => OnButtonHoverChanged(button, true),
            () => OnButtonHoverChanged(button, false));

        button.SetMeta(ManagedMetaKey, true);
        button.MouseEntered += binding.MouseEnteredHandler;
        button.MouseExited += binding.MouseExitedHandler;
        _bindings[button] = binding;
    }

    /// <summary>
    ///     解绑按钮事件并恢复原始缩放状态。
    /// </summary>
    /// <param name="button">目标按钮。</param>
    private void UnbindButton(BaseButton button)
    {
        if (!_bindings.Remove(button, out var binding)) return;

        if (_scaleTweens.Remove(button, out var tween)) tween.Kill();

        if (!IsInstanceValid(button) || button.IsQueuedForDeletion()) return;

        button.MouseEntered -= binding.MouseEnteredHandler;
        button.MouseExited -= binding.MouseExitedHandler;
        button.RemoveMeta(ManagedMetaKey);
        button.Scale = binding.OriginalScale;
    }

    /// <summary>
    ///     处理按钮悬停状态切换。
    /// </summary>
    /// <param name="button">目标按钮。</param>
    /// <param name="hovered">是否处于悬停态。</param>
    private void OnButtonHoverChanged(BaseButton button, bool hovered)
    {
        if (!_bindings.TryGetValue(button, out var binding) ||
            !IsInstanceValid(button) ||
            button.IsQueuedForDeletion())
            return;

        if (EnableScale)
            AnimateScale(button, hovered ? binding.OriginalScale * _resolvedProfile.HoverScale : binding.OriginalScale);
    }

    /// <summary>
    ///     为按钮播放缩放动画。
    /// </summary>
    /// <param name="button">目标按钮。</param>
    /// <param name="targetScale">目标缩放值。</param>
    private void AnimateScale(BaseButton button, Vector2 targetScale)
    {
        if (!IsInstanceValid(button) || button.IsQueuedForDeletion()) return;

        if (_scaleTweens.Remove(button, out var existingTween)) existingTween.Kill();

        button.PivotOffset = button.Size / 2f;
        var tween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        tween.TweenProperty(button, "scale", targetScale, _resolvedProfile.AnimationDuration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        _scaleTweens[button] = tween;
    }

    /// <summary>
    ///     判断按钮是否已经拥有专属的悬停组件。
    /// </summary>
    /// <param name="button">目标按钮。</param>
    /// <returns>若存在专属组件则返回 <see langword="true" />。</returns>
    private bool HasDedicatedHoverEffect(BaseButton button)
    {
        foreach (var child in button.GetChildren())
            if (child is HoverEffect effect && effect != this)
                return true;

        return false;
    }

    /// <summary>
    ///     根据预设解析当前组件实际使用的参数。
    /// </summary>
    /// <returns>解析后的悬停配置。</returns>
    private HoverProfile ResolveProfile()
    {
        return Preset switch
        {
            HoverEffectPreset.Primary => HoverProfile.FromCustom(1.03f, 0.08f),
            HoverEffectPreset.Secondary => HoverProfile.FromCustom(1.02f, 0.08f),
            HoverEffectPreset.Tool => HoverProfile.FromCustom(1.012f, 0.07f),
            _ => HoverProfile.FromCustom(HoverScale, AnimationDuration)
        };
    }

    /// <summary>
    ///     按钮绑定缓存。
    /// </summary>
    /// <param name="OriginalScale">按钮初始缩放。</param>
    /// <param name="MouseEnteredHandler">鼠标进入处理器。</param>
    /// <param name="MouseExitedHandler">鼠标离开处理器。</param>
    private sealed record ButtonBinding(
        Vector2 OriginalScale,
        Action MouseEnteredHandler,
        Action MouseExitedHandler);

    /// <summary>
    ///     解析后的悬停效果配置。
    /// </summary>
    /// <param name="HoverScale">目标缩放倍率。</param>
    /// <param name="AnimationDuration">动画时长。</param>
    private sealed record HoverProfile(float HoverScale, float AnimationDuration)
    {
        /// <summary>
        ///     用自定义参数构建悬停配置。
        /// </summary>
        /// <param name="hoverScale">目标缩放倍率。</param>
        /// <param name="animationDuration">动画时长。</param>
        /// <returns>悬停配置实例。</returns>
        public static HoverProfile FromCustom(float hoverScale, float animationDuration)
        {
            return new HoverProfile(hoverScale, animationDuration);
        }
    }
}

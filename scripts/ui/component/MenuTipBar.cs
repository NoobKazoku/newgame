using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     可复用的轻量提示条组件，用于菜单、选择页等低干扰信息位。
/// </summary>
public partial class MenuTipBar : PanelContainer
{
    private Label? _tipLabel;
    private string _text = string.Empty;

    /// <summary>
    ///     当前提示文本。
    /// </summary>
    [Export]
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            ApplyText();
        }
    }

    /// <summary>
    ///     组件就绪时绑定内部文本节点。
    /// </summary>
    public override void _Ready()
    {
        _tipLabel = GetNodeOrNull<Label>("Margin/TipLabel");
        ApplyText();
    }

    private void ApplyText()
    {
        if (_tipLabel is not null) _tipLabel.Text = _text;
    }
}

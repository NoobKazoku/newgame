using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     主菜单标题绘制组件，提供字距、轻描边、微发光和横向渐变。
/// </summary>
[Tool]
public partial class MainMenuTitle : Control
{
    private string _text = GetDefaultTitleText();
    private int _fontSize = 72;
    private float _characterSpacing = 5f;
    private float _glowAlpha = 0.18f;

    /// <summary>
    ///     标题文本。
    /// </summary>
    [Export]
    public string Text
    {
        get => _text;
        set
        {
            _text = value;
            QueueRedraw();
        }
    }

    /// <summary>
    ///     标题字号。
    /// </summary>
    [Export]
    public int FontSize
    {
        get => _fontSize;
        set
        {
            if (_fontSize == value)
                return;

            _fontSize = value;
            QueueRedraw();
        }
    }

    /// <summary>
    ///     字符间距。
    /// </summary>
    [Export]
    public float CharacterSpacing
    {
        get => _characterSpacing;
        set
        {
            if (Mathf.IsEqualApprox(_characterSpacing, value))
                return;

            _characterSpacing = value;
            QueueRedraw();
        }
    }

    /// <summary>
    ///     微发光强度。
    /// </summary>
    [Export]
    public float GlowAlpha
    {
        get => _glowAlpha;
        set
        {
            if (Mathf.IsEqualApprox(_glowAlpha, value))
                return;

            _glowAlpha = value;
            QueueRedraw();
        }
    }

    private static string GetDefaultTitleText()
    {
        var projectName = ProjectSettings.GetSetting("application/config/name", "GFramework Godot Template").AsString();
        return string.IsNullOrWhiteSpace(projectName)
            ? "GFramework Godot Template"
            : projectName;
    }

    /// <summary>
    ///     控件尺寸变化时重绘标题。
    /// </summary>
    public override void _Notification(int what)
    {
        if (what == NotificationResized) QueueRedraw();
    }

    /// <summary>
    ///     绘制带轻描边和渐变的标题。
    /// </summary>
    public override void _Draw()
    {
        if (string.IsNullOrWhiteSpace(Text)) return;

        var font = ThemeDB.FallbackFont;
        var characters = Text.ToCharArray();
        var totalWidth = MeasureWidth(font, characters);
        var startX = (Size.X - totalWidth) * 0.5f;
        var baselineY = Size.Y * 0.5f + FontSize * 0.34f;
        var cursor = startX;

        for (var i = 0; i < characters.Length; i++)
        {
            var glyph = characters[i].ToString();
            var glyphSize = font.GetStringSize(glyph, HorizontalAlignment.Left, -1, FontSize);
            var position = new Vector2(cursor, baselineY);
            var color = GetGradientColor(i, characters.Length);

            DrawGlyph(font, glyph, position + new Vector2(0f, 0f), color);
            cursor += glyphSize.X + CharacterSpacing;
        }
    }

    private float MeasureWidth(Font font, IReadOnlyCollection<char> characters)
    {
        var width = 0f;
        var index = 0;
        foreach (var character in characters)
        {
            width += font.GetStringSize(character.ToString(), HorizontalAlignment.Left, -1, FontSize).X;
            if (++index < characters.Count) width += CharacterSpacing;
        }

        return width;
    }

    private void DrawGlyph(Font font, string glyph, Vector2 position, Color color)
    {
        var outline = new Color(0.03f, 0.04f, 0.035f, 0.82f);
        var glow = new Color(0.94f, 0.76f, 0.36f, GlowAlpha);
        var shadow = new Color(0f, 0f, 0f, 0.42f);

        DrawString(font, position + new Vector2(0f, 6f), glyph, HorizontalAlignment.Left, -1, FontSize, shadow);

        for (var radius = 5; radius >= 2; radius -= 3)
        {
            DrawString(font, position + new Vector2(radius, 0f), glyph, HorizontalAlignment.Left, -1, FontSize, glow);
            DrawString(font, position + new Vector2(-radius, 0f), glyph, HorizontalAlignment.Left, -1, FontSize, glow);
            DrawString(font, position + new Vector2(0f, radius), glyph, HorizontalAlignment.Left, -1, FontSize, glow);
            DrawString(font, position + new Vector2(0f, -radius), glyph, HorizontalAlignment.Left, -1, FontSize, glow);
        }

        DrawString(font, position + new Vector2(2f, 0f), glyph, HorizontalAlignment.Left, -1, FontSize, outline);
        DrawString(font, position + new Vector2(-2f, 0f), glyph, HorizontalAlignment.Left, -1, FontSize, outline);
        DrawString(font, position + new Vector2(0f, 2f), glyph, HorizontalAlignment.Left, -1, FontSize, outline);
        DrawString(font, position + new Vector2(0f, -2f), glyph, HorizontalAlignment.Left, -1, FontSize, outline);
        DrawString(font, position, glyph, HorizontalAlignment.Left, -1, FontSize, color);
    }

    private static Color GetGradientColor(int index, int count)
    {
        if (count <= 1) return new Color(0.98f, 0.95f, 0.82f);

        var t = index / (float)(count - 1);
        var left = new Color(0.96f, 0.95f, 0.84f);
        var middle = new Color(0.92f, 0.72f, 0.34f);
        var right = new Color(0.72f, 0.82f, 0.76f);
        return t < 0.55f
            ? left.Lerp(middle, t / 0.55f)
            : middle.Lerp(right, (t - 0.55f) / 0.45f);
    }
}

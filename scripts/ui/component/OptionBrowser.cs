using System.Globalization;
using Godot;

namespace GFrameworkGodotTemplate.scripts.ui.component;

/// <summary>
///     可复用的选项浏览容器，支持查询、分类过滤、分页与滚动浏览。
/// </summary>
[Tool]
public partial class OptionBrowser : PanelContainer
{
    private const string AllCategoriesId = "__all__";
    private const float WheelScrollStepPixels = 96f;
    private const double WheelScrollTweenDurationSeconds = 0.14d;

    private readonly List<OptionBrowserEntryData> _allEntries = [];
    private string _allCategoriesText = "All";

    [GetNode] private OptionButton _categoryFilter = null!;

    private string _editLabelText = "Edit";
    private string _emptyEntriesText = "No entries available.";
    private string _emptyQueryText = "No entries match the current query.";

    [GetNode] private Label _emptyStateLabel = null!;

    [GetNode] private VBoxContainer _entryContainer = null!;

    [GetNode] private Button _nextPageButton = null!;

    private string _nextPageText = "Next";

    [GetNode] private Label _pageLabel = null!;

    [GetNode] private Button _previousPageButton = null!;

    private string _previousPageText = "Prev";

    [GetNode("Margin/Root/Scroll")] private ScrollContainer _scroll = null!;

    private Tween? _scrollTween;

    [GetNode] private LineEdit _searchInput = null!;

    private string _searchPlaceholderText = "Search";
    private string? _selectedEntryKey;
    private float _targetScrollVertical;

    /// <summary>
    ///     每页展示的条目数量。
    /// </summary>
    [Export]
    public int PageSize { get; set; } = 10;

    private int CurrentCategoryIndex =>
        Math.Clamp(_categoryFilter.Selected, 0, Math.Max(0, _categoryFilter.ItemCount - 1));

    private int CurrentPageIndex { get; set; }

    /// <summary>
    ///     用当前条目集合刷新浏览器。
    /// </summary>
    /// <param name="entries">新的条目集合。</param>
    public void SetEntries(IReadOnlyCollection<OptionBrowserEntryData> entries)
    {
        _allEntries.Clear();
        _allEntries.AddRange(entries.OrderBy(static entry => entry.CategoryLabel, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Title, StringComparer.Ordinal));
        RebuildCategoryFilter();
        ClampPageIndex();
        RefreshView(false);
    }

    /// <summary>
    ///     更新浏览器自身的固定界面文案。
    /// </summary>
    /// <param name="searchPlaceholderText">搜索框占位文案。</param>
    /// <param name="allCategoriesText">全部分类文案。</param>
    /// <param name="previousPageText">上一页按钮文案。</param>
    /// <param name="nextPageText">下一页按钮文案。</param>
    /// <param name="emptyQueryText">搜索无结果时的空状态文案。</param>
    /// <param name="emptyEntriesText">无条目时的空状态文案。</param>
    /// <param name="editLabelText">编辑器标签文案。</param>
    public void ConfigureTexts(
        string searchPlaceholderText,
        string allCategoriesText,
        string previousPageText,
        string nextPageText,
        string emptyQueryText,
        string emptyEntriesText,
        string editLabelText)
    {
        _searchPlaceholderText = searchPlaceholderText;
        _allCategoriesText = allCategoriesText;
        _previousPageText = previousPageText;
        _nextPageText = nextPageText;
        _emptyQueryText = emptyQueryText;
        _emptyEntriesText = emptyEntriesText;
        _editLabelText = editLabelText;

        if (!IsInsideTree()) return;

        _searchInput.PlaceholderText = _searchPlaceholderText;
        _previousPageButton.Text = _previousPageText;
        _nextPageButton.Text = _nextPageText;
        RebuildCategoryFilter();
        RefreshView(true);
    }

    /// <summary>
    ///     节点进入场景树后注入控件并绑定已知信号。
    /// </summary>
    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        __BindNodeSignals_Generated();
        ConfigureScrollInputBehavior();
        _searchInput.PlaceholderText = _searchPlaceholderText;
        _previousPageButton.Text = _previousPageText;
        _nextPageButton.Text = _nextPageText;
        RebuildCategoryFilter();
        RefreshView(true);
    }

    /// <summary>
    ///     节点退出场景树时解除已知信号绑定。
    /// </summary>
    public override void _ExitTree()
    {
        __UnbindNodeSignals_Generated();
    }

    /// <summary>
    ///     点击浏览器内非输入控件区域时释放搜索框或编辑器焦点。
    /// </summary>
    /// <param name="event">输入事件。</param>
    public override void _Input(InputEvent @event)
    {
        if (!Visible ||
            @event is not InputEventMouseButton { Pressed: true } mouseButton ||
            mouseButton.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
            return;

        var focusOwner = GetViewport().GuiGetFocusOwner();
        if (focusOwner is null || !IsAncestorOf(focusOwner))
            return;

        if (IsPointInsideFocusRetainingControl(mouseButton.GlobalPosition))
            return;

        focusOwner.ReleaseFocus();
    }

    [BindNodeSignal(nameof(_searchInput), nameof(LineEdit.TextChanged))]
    private void OnSearchTextChanged(string _)
    {
        CurrentPageIndex = 0;
        RefreshView(true);
    }

    [BindNodeSignal(nameof(_categoryFilter), nameof(OptionButton.ItemSelected))]
    private void OnCategoryFilterItemSelected(long _)
    {
        CurrentPageIndex = 0;
        RefreshView(true);
    }

    [BindNodeSignal(nameof(_previousPageButton), nameof(Button.Pressed))]
    private void OnPreviousPagePressed()
    {
        CurrentPageIndex = Math.Max(0, CurrentPageIndex - 1);
        RefreshView(true);
    }

    [BindNodeSignal(nameof(_nextPageButton), nameof(Button.Pressed))]
    private void OnNextPagePressed()
    {
        CurrentPageIndex++;
        ClampPageIndex();
        RefreshView(true);
    }

    [BindNodeSignal(nameof(_scroll), nameof(GuiInput))]
    private void OnScrollGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } mouseButton) return;

        var direction = mouseButton.ButtonIndex switch
        {
            MouseButton.WheelUp => -1,
            MouseButton.WheelDown => 1,
            _ => 0
        };

        if (direction == 0) return;

        SmoothScrollBy(direction * WheelScrollStepPixels);
        AcceptEvent();
    }

    private void RebuildCategoryFilter()
    {
        if (!IsInsideTree()) return;

        var selectedId = _categoryFilter.ItemCount > 0
            ? _categoryFilter.GetItemMetadata(CurrentCategoryIndex).AsString()
            : AllCategoriesId;

        _categoryFilter.Clear();
        _categoryFilter.AddItem(_allCategoriesText);
        _categoryFilter.SetItemMetadata(0, AllCategoriesId);

        foreach (var category in _allEntries
                     .GroupBy(static entry => entry.CategoryId, static entry => entry.CategoryLabel,
                         StringComparer.Ordinal)
                     .OrderBy(static group => group.Key, StringComparer.Ordinal))
        {
            _categoryFilter.AddItem(category.First());
            _categoryFilter.SetItemMetadata(_categoryFilter.ItemCount - 1, category.Key);
        }

        for (var i = 0; i < _categoryFilter.ItemCount; i++)
        {
            if (!string.Equals(_categoryFilter.GetItemMetadata(i).AsString(), selectedId,
                    StringComparison.Ordinal)) continue;

            _categoryFilter.Select(i);
            return;
        }

        _categoryFilter.Select(0);
    }

    private void RefreshView(bool resetScrollPosition)
    {
        if (!IsInsideTree()) return;

        var preservedScrollVertical = _scroll.ScrollVertical;

        foreach (var child in _entryContainer.GetChildren()) child.QueueFree();

        var filteredEntries = GetFilteredEntries();
        var pageCount = Math.Max(1, (int)Math.Ceiling(filteredEntries.Count / (double)Math.Max(1, PageSize)));
        CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, pageCount - 1);

        var pageEntries = filteredEntries
            .Skip(CurrentPageIndex * Math.Max(1, PageSize))
            .Take(Math.Max(1, PageSize))
            .ToArray();

        foreach (var entry in pageEntries)
            _entryContainer.AddChild(CreateEntryCard(entry,
                string.Equals(entry.Key, _selectedEntryKey, StringComparison.Ordinal)));

        _emptyStateLabel.Visible = filteredEntries.Count == 0;
        _emptyStateLabel.Text = _searchInput.Text.Length > 0
            ? _emptyQueryText
            : _emptyEntriesText;
        _pageLabel.Text = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1} · {2}",
            filteredEntries.Count == 0 ? 0 : CurrentPageIndex + 1,
            filteredEntries.Count == 0 ? 0 : pageCount,
            filteredEntries.Count);
        _previousPageButton.Disabled = CurrentPageIndex <= 0 || filteredEntries.Count == 0;
        _nextPageButton.Disabled = CurrentPageIndex >= pageCount - 1 || filteredEntries.Count == 0;

        if (resetScrollPosition)
        {
            ResetScrollPositionToTop();
            return;
        }

        RestoreScrollPosition(preservedScrollVertical);
    }

    private List<OptionBrowserEntryData> GetFilteredEntries()
    {
        var search = _searchInput.Text.Trim().ToLowerInvariant();
        var categoryId = _categoryFilter.ItemCount == 0
            ? AllCategoriesId
            : _categoryFilter.GetItemMetadata(CurrentCategoryIndex).AsString();

        return _allEntries
            .Where(entry => string.Equals(categoryId, AllCategoriesId, StringComparison.Ordinal) ||
                            string.Equals(entry.CategoryId, categoryId, StringComparison.Ordinal))
            .Where(entry => string.IsNullOrEmpty(search) || entry.SearchText.Contains(search, StringComparison.Ordinal))
            .ToList();
    }

    private void ClampPageIndex()
    {
        var filteredCount = GetFilteredEntries().Count;
        var pageCount = Math.Max(1, (int)Math.Ceiling(filteredCount / (double)Math.Max(1, PageSize)));
        CurrentPageIndex = Math.Clamp(CurrentPageIndex, 0, pageCount - 1);
    }

    private Control CreateEntryCard(OptionBrowserEntryData entry, bool isSelected)
    {
        var panel = CreateEntryCardPanel(entry, isSelected);
        var root = CreateEntryCardRoot(panel);
        root.AddChild(CreateEntryHeader(entry));
        root.AddChild(CreateEntryBodyLabel(entry.Description, entry.TooltipText, new Color(0.82f, 0.84f, 0.87f, 0.92f),
            0));
        root.AddChild(CreateEntryBodyLabel(BuildMetaLine(entry), entry.TooltipText,
            new Color(0.77f, 0.75f, 0.71f, 0.94f), 14));
        root.AddChild(CreateEditorRow(entry));
        return panel;
    }

    private PanelContainer CreateEntryCardPanel(OptionBrowserEntryData entry, bool isSelected)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(0f, 132f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = entry.TooltipText
        };
        panel.AddThemeStyleboxOverride("panel", CreateCardStyle(isSelected));
        return panel;
    }

    private VBoxContainer CreateEntryCardRoot(PanelContainer panel)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        panel.AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 6);
        margin.AddChild(root);
        return root;
    }

    private HBoxContainer CreateEntryHeader(OptionBrowserEntryData entry)
    {
        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 10);

        var title = new Label
        {
            Text = entry.Title,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = entry.TooltipText
        };
        title.AddThemeFontSizeOverride("font_size", 18);
        header.AddChild(title);
        header.AddChild(CreateBadgeLabel(entry.CategoryLabel, new Color(0.90f, 0.80f, 0.53f)));

        if (!string.IsNullOrWhiteSpace(entry.RuntimeStateText))
            header.AddChild(CreateBadgeLabel(entry.RuntimeStateText, new Color(0.72f, 0.87f, 0.97f)));

        return header;
    }

    private static Label CreateEntryBodyLabel(string text, string tooltipText, Color color, int fontSize)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TooltipText = tooltipText
        };
        if (fontSize > 0)
            label.AddThemeFontSizeOverride("font_size", fontSize);

        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private HBoxContainer CreateEditorRow(OptionBrowserEntryData entry)
    {
        var editorRow = new HBoxContainer();
        editorRow.AddThemeConstantOverride("separation", 10);

        var editorLabel = new Label
        {
            Text = _editLabelText,
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        editorLabel.AddThemeColorOverride("font_color", new Color(0.93f, 0.90f, 0.84f, 0.92f));
        editorRow.AddChild(editorLabel);
        editorRow.AddChild(CreateEditorControl(entry));
        return editorRow;
    }

    private Control CreateEditorControl(OptionBrowserEntryData entry)
    {
        return entry.EditorKind switch
        {
            OptionBrowserEditorKind.Integer => CreateIntegerEditor(entry),
            OptionBrowserEditorKind.Choice => CreateChoiceEditor(entry),
            _ => CreateReadOnlyEditor()
        };
    }

    private SpinBox CreateIntegerEditor(OptionBrowserEntryData entry)
    {
        var spinBox = new SpinBox
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Editable = entry.CanEdit,
            Rounded = entry.Rounded
        };

        // Godot SpinBox 默认最大值是 100，必须先写范围再写值，否则会在初始化时被旧上限夹住。
        spinBox.MinValue = entry.MinValue;
        spinBox.MaxValue = entry.MaxValue;
        spinBox.Step = entry.Step;
        spinBox.Value = Math.Clamp(entry.NumericValue, entry.MinValue, entry.MaxValue);
        ConfigureEditorScrollBehavior(spinBox);
        spinBox.ValueChanged += value =>
        {
            _selectedEntryKey = entry.Key;
            entry.NumericValueChanged?.Invoke(value);
        };
        return spinBox;
    }

    private OptionButton CreateChoiceEditor(OptionBrowserEntryData entry)
    {
        var optionButton = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Disabled = !entry.CanEdit
        };

        var selectedIndex = 0;
        for (var i = 0; i < entry.Choices.Count; i++)
        {
            var choice = entry.Choices[i];
            optionButton.AddItem(choice.Label, choice.Value);
            if (choice.Value == entry.SelectedChoiceValue)
                selectedIndex = i;
        }

        optionButton.Select(selectedIndex);
        ConfigureEditorScrollBehavior(optionButton);
        optionButton.ItemSelected += index =>
        {
            if (index < 0 || index >= entry.Choices.Count)
                return;

            _selectedEntryKey = entry.Key;
            entry.ChoiceValueChanged?.Invoke(entry.Choices[(int)index].Value);
        };
        return optionButton;
    }

    private static Label CreateReadOnlyEditor()
    {
        return new Label
        {
            Text = "-",
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
    }

    private static Label CreateBadgeLabel(string text, Color color)
    {
        var label = new Label
        {
            Text = text
        };
        label.AddThemeFontSizeOverride("font_size", 13);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static string BuildMetaLine(OptionBrowserEntryData entry)
    {
        var segments = new[]
        {
            entry.CurrentValueText,
            entry.AppliedValueText,
            entry.DefaultValueText,
            entry.RangeText,
            entry.RecommendedValueText
        };

        return string.Join("  ·  ", segments.Where(static segment => !string.IsNullOrWhiteSpace(segment)));
    }

    private static StyleBoxFlat CreateCardStyle(bool isSelected)
    {
        return new StyleBoxFlat
        {
            BgColor = isSelected
                ? new Color(0.16f, 0.20f, 0.19f, 0.95f)
                : new Color(0.10f, 0.12f, 0.12f, 0.88f),
            BorderColor = isSelected
                ? new Color(0.95f, 0.81f, 0.48f, 0.98f)
                : new Color(0.40f, 0.35f, 0.24f, 0.88f),
            BorderWidthBottom = 2,
            BorderWidthLeft = 2,
            BorderWidthRight = 2,
            BorderWidthTop = 2,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            ShadowColor = new Color(0f, 0f, 0f, 0.20f),
            ShadowOffset = new Vector2(0f, 0f),
            ShadowSize = 6
        };
    }

    private void ConfigureScrollInputBehavior()
    {
        ConfigureWheelForwarding(_searchInput);
        ConfigureWheelForwarding(_categoryFilter);
        ConfigureWheelForwarding(_previousPageButton);
        ConfigureWheelForwarding(_nextPageButton);
        _targetScrollVertical = _scroll.ScrollVertical;
    }

    private void SmoothScrollBy(float deltaPixels)
    {
        var scrollBar = _scroll.GetVScrollBar();
        if (scrollBar is null) return;

        var scrollMax = Math.Max(0f, (float)(scrollBar.MaxValue - scrollBar.Page));
        var scrollBase = _scrollTween is null ? _scroll.ScrollVertical : _targetScrollVertical;
        _targetScrollVertical = Mathf.Clamp(scrollBase + deltaPixels, 0f, scrollMax);
        if (Mathf.IsEqualApprox(_targetScrollVertical, _scroll.ScrollVertical)) return;

        _scrollTween?.Kill();
        _scrollTween = CreateTween();
        _scrollTween.SetTrans(Tween.TransitionType.Cubic);
        _scrollTween.SetEase(Tween.EaseType.Out);
        _scrollTween.TweenProperty(_scroll, "scroll_vertical", _targetScrollVertical, WheelScrollTweenDurationSeconds);
    }

    private void ResetScrollTweenState()
    {
        _scrollTween?.Kill();
        _scrollTween = null;
        _targetScrollVertical = _scroll.ScrollVertical;
    }

    private void ResetScrollPositionToTop()
    {
        ResetScrollTweenState();
        _scroll.ScrollVertical = 0;
        _targetScrollVertical = 0;
    }

    private void RestoreScrollPosition(int desiredScrollVertical)
    {
        ResetScrollTweenState();

        var scrollBar = _scroll.GetVScrollBar();
        if (scrollBar is null)
        {
            _scroll.ScrollVertical = desiredScrollVertical;
            _targetScrollVertical = desiredScrollVertical;
            return;
        }

        var scrollMax = Math.Max(0, (int)MathF.Ceiling((float)(scrollBar.MaxValue - scrollBar.Page)));
        var clampedScrollVertical = Math.Clamp(desiredScrollVertical, 0, scrollMax);
        _scroll.ScrollVertical = clampedScrollVertical;
        _targetScrollVertical = clampedScrollVertical;
    }

    private void ConfigureEditorScrollBehavior(Control control)
    {
        control.MouseForcePassScrollEvents = false;
        ConfigureWheelForwarding(control);
    }

    private void ConfigureWheelForwarding(Control control)
    {
        control.MouseForcePassScrollEvents = false;
        control.GuiInput += eventArgs => ForwardWheelInputToScroll(eventArgs);
    }

    private bool IsPointInsideFocusRetainingControl(Vector2 globalPosition)
    {
        return IsPointInsideFocusRetainingControl(this, globalPosition);
    }

    private static bool IsPointInsideFocusRetainingControl(Node node, Vector2 globalPosition)
    {
        if (node is Control { Visible: true } control &&
            control is LineEdit or SpinBox or OptionButton &&
            control.GetGlobalRect().HasPoint(globalPosition))
            return true;

        foreach (var child in node.GetChildren())
            if (IsPointInsideFocusRetainingControl(child, globalPosition))
                return true;

        return false;
    }

    private void ForwardWheelInputToScroll(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } mouseButton) return;

        var direction = mouseButton.ButtonIndex switch
        {
            MouseButton.WheelUp => -1,
            MouseButton.WheelDown => 1,
            _ => 0
        };

        if (direction == 0) return;

        SmoothScrollBy(direction * WheelScrollStepPixels);
        AcceptEvent();
    }
}

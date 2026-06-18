using GFrameworkGodotTemplate.scripts.config;
using GFrameworkGodotTemplate.scripts.core.ui;
using GFrameworkGodotTemplate.scripts.cqrs.setting.query;
using GFrameworkGodotTemplate.scripts.cqrs.setting.query.view;
using GFrameworkGodotTemplate.scripts.enums.ui;
using GFrameworkGodotTemplate.scripts.ui.component;
using Godot;
using VolumeContainer = GFrameworkGodotTemplate.scripts.ui.component.VolumeContainer;

namespace GFrameworkGodotTemplate.scripts.options_menu;

/// <summary>
///     选项设置界面控制器。
///     页面内修改先保留为待应用状态，点击“应用”后才写入设置并保存。
/// </summary>
[ContextAware]
[Log]
public partial class OptionsMenu : Control, IController, IUiPageBehaviorProvider, ISimpleUiPage,
    IUiInteractionProfileProvider, IUiActionHandler
{
    private const string ChineseLanguageValue = "简体中文";
    private const string EnglishLanguageValue = "English";

    private readonly AudioSettings _appliedAudio = new();
    private readonly GraphicsSettings _appliedGraphics = new();
    private readonly LocalizationSettings _appliedLocalization = new();
    private readonly SemaphoreSlim _audioApplySemaphore = new(1, 1);
    private readonly AudioSettings _pendingAudio = new();
    private readonly GraphicsSettings _pendingGraphics = new();
    private readonly LocalizationSettings _pendingLocalization = new();

    private readonly Vector2I[] _resolutions =
    [
        new(1920, 1080),
        new(1366, 768),
        new(1280, 720),
        new(1024, 768)
    ];

    [GetNode] private Button _applyButton = null!;

    [GetNode("Panel/MarginContainer/HBoxContainer/MarginContainer/HBoxContainer/Audio/Title")]
    private Label _audioTitleLabel = null!;

    [GetNode("%Back")] private Button _backButton = null!;

    [GetNode] private VolumeContainer _bgmVolumeContainer = null!;

    [GetUtility] private ITemplateContentCatalog _contentCatalog = null!;

    [GetNode(
        "Panel/MarginContainer/HBoxContainer/MarginContainer/HBoxContainer/Graphics/MarginContainer/FullscreenContainer/FullscreenLabel")]
    private Label _fullscreenLabel = null!;

    [GetNode] private OptionButton _fullscreenOptionButton = null!;

    [GetNode("Panel/MarginContainer/HBoxContainer/MarginContainer/HBoxContainer/Graphics/Title")]
    private Label _graphicsTitleLabel = null!;

    private bool _initializing;
    private int _latestAudioPreviewRequestId;

    [GetNode(
        "Panel/MarginContainer/HBoxContainer/MarginContainer/HBoxContainer/Localization/MarginContainer/LanguageContainer/LanguageLabel")]
    private Label _languageLabel = null!;

    [GetNode] private OptionButton _languageOptionButton = null!;

    private ILocalizationManager? _localizationManager;

    [GetNode("Panel/MarginContainer/HBoxContainer/MarginContainer/HBoxContainer/Localization/Title")]
    private Label _localizationTitleLabel = null!;

    [GetNode] private VolumeContainer _masterVolumeContainer = null!;

    private IUiPageBehavior? _page;

    [GetNode(
        "Panel/MarginContainer/HBoxContainer/MarginContainer/HBoxContainer/Graphics/MarginContainer2/ResolutionContainer/ResolutionLabel")]
    private Label _resolutionLabel = null!;

    [GetNode] private OptionButton _resolutionOptionButton = null!;

    private ISettingsModel _settingsModel = null!;
    private ISettingsSystem _settingsSystem = null!;

    [GetNode] private VolumeContainer _sfxVolumeContainer = null!;

    [GetNode("Panel/MarginContainer/HBoxContainer/MarginContainer/HBoxContainer/Title")]
    private Label _titleLabel = null!;

    private IUiRouter _uiRouter = null!;

    [GetNode] private ConfirmDialog _unsavedChangesDialog = null!;

    /// <summary>
    ///     Ui Key的字符串形式。
    /// </summary>
    public static string UiKeyStr => nameof(UiKey.OptionsMenu);

    /// <summary>
    ///     处理路由器转发的取消动作。
    /// </summary>
    bool IUiActionHandler.TryHandleUiAction(UiInputAction action)
    {
        if (action != UiInputAction.Cancel) return false;

        if (_unsavedChangesDialog.TryCancel()) return true;

        RequestClose();
        return true;
    }

    /// <summary>
    ///     声明选项页会阻断玩法输入，并在暂停态下继续运行。
    /// </summary>
    UiInteractionProfile IUiInteractionProfileProvider.GetUiInteractionProfile(UiLayer layer)
    {
        return new UiInteractionProfile
        {
            CapturedActions = UiInputActionMask.Cancel,
            BlocksWorldPointerInput = true,
            BlocksWorldActionInput = true,
            ContinueProcessingWhenPaused = true
        };
    }

    /// <summary>
    ///     获取页面行为实例。
    /// </summary>
    public IUiPageBehavior GetPage()
    {
        _page ??= UiPageBehaviorFactory.Create<Control>(this, UiKeyStr, UiLayer.Modal);
        return _page;
    }

    /// <summary>
    ///     节点准备就绪时初始化设置页。
    /// </summary>
    public override void _Ready()
    {
        __InjectGetNodes_Generated();
        __InjectContextBindings_Generated();

        _uiRouter = this.GetSystem<IUiRouter>()!;
        _settingsModel = this.GetModel<ISettingsModel>()!;
        _settingsSystem = this.GetSystem<ISettingsSystem>()!;
        _localizationManager = this.GetSystem<ILocalizationManager>()!;

        _localizationManager.SubscribeToLanguageChange(OnCurrentLanguageChanged);
        this.RegisterEvent<SettingsAppliedEvent<ISettingsSection>>(OnSettingsApplied);
        _unsavedChangesDialog.Signal("Confirmed").To(Callable.From(OnUnsavedChangesDialogConfirmed)).End();
        _unsavedChangesDialog.Signal("Canceled").To(Callable.From(OnUnsavedChangesDialogCanceled)).End();

        InitCoroutine().RunCoroutine();
    }

    /// <summary>
    ///     节点退出树时恢复未应用预览并解绑事件。
    /// </summary>
    public override void _ExitTree()
    {
        this.UnRegisterEvent<SettingsAppliedEvent<ISettingsSection>>(OnSettingsApplied);
        _localizationManager?.UnsubscribeFromLanguageChange(OnCurrentLanguageChanged);
    }

    private IEnumerator<IYieldInstruction> InitCoroutine()
    {
        SetupEventHandlers();
        CallDeferred(nameof(CallDeferredInit));
        yield return new Delay(0);
    }

    private void SetupEventHandlers()
    {
        var signalName = VolumeContainer.SignalName.VolumeChanged;
        _masterVolumeContainer.Signal(signalName).To(Callable.From<float>(OnMasterVolumeChanged)).End();
        _bgmVolumeContainer.Signal(signalName).To(Callable.From<float>(OnBgmVolumeChanged)).End();
        _sfxVolumeContainer.Signal(signalName).To(Callable.From<float>(OnSfxVolumeChanged)).End();
        _resolutionOptionButton.ItemSelected += OnResolutionOptionButtonItemSelected;
        _fullscreenOptionButton.ItemSelected += OnFullscreenOptionButtonItemSelected;
        _languageOptionButton.ItemSelected += OnLanguageOptionButtonItemSelected;
        _applyButton.Pressed += OnApplyButtonPressed;
        _backButton.Pressed += RequestClose;
    }

    private void CallDeferredInit()
    {
        CallDeferredInitCoroutine().RunCoroutine(Segment.ProcessIgnorePause);
    }

    private IEnumerator<IYieldInstruction> CallDeferredInitCoroutine()
    {
        Hide();
        var eventBus = this.GetService<IEventBus>()!;
        if (!_settingsModel.IsInitialized) yield return new WaitForEvent<SettingsInitializedEvent>(eventBus);

        yield return InitializeUiAsync().AsCoroutineInstruction();
        Show();
    }

    private async Task InitializeUiAsync()
    {
        _initializing = true;
        try
        {
            var menuText = _contentCatalog.GetMenuText();
            ApplyStaticText(menuText);

            var view = await this.SendQueryAsync(new GetCurrentSettingsQuery()).ConfigureAwait(true);
            CaptureAppliedSettings(view);
            ResetPendingSettingsToApplied();
            ApplyPendingSettingsToUi(menuText);
            RefreshDirtyState();
        }
        finally
        {
            _initializing = false;
        }
    }

    private void CaptureAppliedSettings(SettingsView view)
    {
        _appliedAudio.LoadFrom(view.Audio);
        _appliedGraphics.LoadFrom(view.Graphics);
        _appliedLocalization.LoadFrom(view.Localization);
    }

    private void ResetPendingSettingsToApplied()
    {
        _pendingAudio.LoadFrom(_appliedAudio);
        _pendingGraphics.LoadFrom(_appliedGraphics);
        _pendingLocalization.LoadFrom(_appliedLocalization);
    }

    private void ApplyPendingSettingsToUi(MenuTextConfig menuText)
    {
        _masterVolumeContainer.Initialize(menuText.OptionsMasterVolume, _pendingAudio.MasterVolume);
        _bgmVolumeContainer.Initialize(menuText.OptionsBgmVolume, _pendingAudio.BgmVolume);
        _sfxVolumeContainer.Initialize(menuText.OptionsSfxVolume, _pendingAudio.SfxVolume);

        _fullscreenOptionButton.Clear();
        _fullscreenOptionButton.AddItem(menuText.OptionsFullscreen);
        _fullscreenOptionButton.AddItem(menuText.OptionsWindowed);
        _fullscreenOptionButton.Selected = _pendingGraphics.Fullscreen ? 0 : 1;

        _resolutionOptionButton.Clear();
        for (var i = 0; i < _resolutions.Length; i++)
        {
            var resolution = _resolutions[i];
            _resolutionOptionButton.AddItem($"{resolution.X}x{resolution.Y}");

            if (resolution.X == _pendingGraphics.ResolutionWidth &&
                resolution.Y == _pendingGraphics.ResolutionHeight)
                _resolutionOptionButton.Selected = i;
        }

        _resolutionOptionButton.Disabled = _pendingGraphics.Fullscreen;

        _languageOptionButton.Clear();
        _languageOptionButton.AddItem(menuText.OptionsLanguageZh);
        _languageOptionButton.AddItem(menuText.OptionsLanguageEn);
        _languageOptionButton.Selected =
            string.Equals(_pendingLocalization.Language, ChineseLanguageValue, StringComparison.Ordinal) ? 0 : 1;
    }

    private void ApplyStaticText(MenuTextConfig menuText)
    {
        _titleLabel.Text = menuText.OptionsTitle;
        _audioTitleLabel.Text = menuText.OptionsAudioTitle;
        _graphicsTitleLabel.Text = menuText.OptionsGraphicsTitle;
        _localizationTitleLabel.Text = menuText.OptionsLocalizationTitle;
        _fullscreenLabel.Text = menuText.OptionsDisplayModeLabel;
        _resolutionLabel.Text = menuText.OptionsResolutionLabel;
        _languageLabel.Text = menuText.OptionsLanguageLabel;
        _applyButton.Text = menuText.OptionsApply;
        _backButton.Text = menuText.OptionsBack;
        _unsavedChangesDialog.Configure(
            menuText.OptionsUnsavedChangesTitle,
            menuText.OptionsUnsavedChangesText,
            menuText.OptionsUnsavedApplyAndExit,
            menuText.OptionsUnsavedDiscardAndExit);
    }

    private void OnResolutionOptionButtonItemSelected(long index)
    {
        if (_initializing) return;

        var resolution = _resolutions[index];
        _pendingGraphics.ResolutionWidth = resolution.X;
        _pendingGraphics.ResolutionHeight = resolution.Y;
        RefreshDirtyState();
    }

    private void OnFullscreenOptionButtonItemSelected(long index)
    {
        if (_initializing) return;

        _pendingGraphics.Fullscreen = index == 0;
        _resolutionOptionButton.Disabled = _pendingGraphics.Fullscreen;
        RefreshDirtyState();
    }

    private void OnLanguageOptionButtonItemSelected(long index)
    {
        if (_initializing) return;

        _pendingLocalization.Language = index == 0 ? ChineseLanguageValue : EnglishLanguageValue;
        RefreshDirtyState();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (_initializing) return;

        _pendingAudio.MasterVolume = value;
        StartPreviewPendingAudio();
        RefreshDirtyState();
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (_initializing) return;

        _pendingAudio.BgmVolume = value;
        StartPreviewPendingAudio();
        RefreshDirtyState();
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (_initializing) return;

        _pendingAudio.SfxVolume = value;
        StartPreviewPendingAudio();
        RefreshDirtyState();
    }

    private async void OnApplyButtonPressed()
    {
        try
        {
            await ApplyPendingChangesAsync(false).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Error(_contentCatalog.GetMenuText().OptionsSaveFailed, ex);
        }
    }

    private async void OnUnsavedChangesDialogConfirmed()
    {
        try
        {
            await ApplyPendingChangesAsync(true).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Error(_contentCatalog.GetMenuText().OptionsSaveFailed, ex);
        }
    }

    private void OnUnsavedChangesDialogCanceled()
    {
        ClosePage();
    }

    private async Task ApplyPendingChangesAsync(bool closeAfterApply)
    {
        if (!HasPendingChanges())
        {
            ClosePageIfRequested(closeAfterApply);
            return;
        }

        var audioChanged = !AreAudioSettingsEqual(_appliedAudio, _pendingAudio);
        var graphicsChanged = !AreGraphicsSettingsEqual(_appliedGraphics, _pendingGraphics);
        var localizationChanged = !AreLocalizationSettingsEqual(_appliedLocalization, _pendingLocalization);
        var committedAudio = CloneSettings(_appliedAudio);
        var committedGraphics = CloneSettings(_appliedGraphics);
        var committedLocalization = CloneSettings(_appliedLocalization);

        try
        {
            await ApplyPendingSettingsAsync(audioChanged, graphicsChanged, localizationChanged).ConfigureAwait(true);
            CommitPendingSettings();
        }
        catch
        {
            await TryRestoreCommittedSettingsAsync(
                committedAudio,
                committedGraphics,
                committedLocalization,
                audioChanged,
                graphicsChanged,
                localizationChanged).ConfigureAwait(true);
            RefreshDirtyState();
            throw;
        }

        ClosePageIfRequested(closeAfterApply);
    }

    private async Task ApplyPendingSettingsAsync(bool audioChanged, bool graphicsChanged, bool localizationChanged)
    {
        if (audioChanged)
            await ApplyAudioSettingsSnapshotAsync(CloneSettings(_pendingAudio)).ConfigureAwait(true);
        else
            await DrainAudioPreviewRequestsAsync().ConfigureAwait(true);

        LoadSettingsModel(_pendingAudio, _pendingGraphics, _pendingLocalization);

        if (graphicsChanged)
            await _settingsSystem.Apply<GodotGraphicsSettings>().ConfigureAwait(true);

        if (localizationChanged)
        {
            await _settingsSystem.Apply<GodotLocalizationSettings>().ConfigureAwait(true);
            RefreshLocalizedUi();
        }

        await _settingsSystem.SaveAll().ConfigureAwait(true);
    }

    private void CommitPendingSettings()
    {
        _appliedAudio.LoadFrom(_pendingAudio);
        _appliedGraphics.LoadFrom(_pendingGraphics);
        _appliedLocalization.LoadFrom(_pendingLocalization);
        RefreshDirtyState();
        _log.Info(_contentCatalog.GetMenuText().OptionsSaved);
    }

    private async Task TryRestoreCommittedSettingsAsync(
        AudioSettings committedAudio,
        GraphicsSettings committedGraphics,
        LocalizationSettings committedLocalization,
        bool audioChanged,
        bool graphicsChanged,
        bool localizationChanged)
    {
        try
        {
            await RestoreCommittedSettingsAsync(
                committedAudio,
                committedGraphics,
                committedLocalization,
                audioChanged,
                graphicsChanged,
                localizationChanged).ConfigureAwait(true);
        }
        catch (Exception restoreEx)
        {
            _log.Error("恢复已应用设置失败。", restoreEx);
        }
    }

    private void ClosePageIfRequested(bool closeAfterApply)
    {
        if (closeAfterApply) ClosePage();
    }

    private void RequestClose()
    {
        if (HasPendingChanges())
        {
            _unsavedChangesDialog.Open();
            return;
        }

        ClosePage();
    }

    private async void ClosePage()
    {
        try
        {
            await RevertPendingAudioPreviewIfNeededAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Error("恢复音频预览设置失败。", ex);
        }

        ResetPendingSettingsToApplied();
        RefreshLocalizedUi();
        var handle = GetPage().Handle;
        if (handle.HasValue)
        {
            _uiRouter.Hide(handle.Value, UiLayer.Modal, true);
            return;
        }

        _log.Warn("Options page handle is null, cannot hide page.");
    }

    private void OnCurrentLanguageChanged(string _)
    {
        CallDeferred(nameof(RefreshLocalizedUi));
    }

    private void OnSettingsApplied(SettingsAppliedEvent<ISettingsSection> @event)
    {
        if (!@event.Success ||
            @event.Settings is not IResetApplyAbleSettings settings ||
            settings.DataType != typeof(LocalizationSettings))
            return;

        CallDeferred(nameof(RefreshLocalizedUi));
    }

    private void RefreshLocalizedUi()
    {
        if (!IsInsideTree()) return;

        _initializing = true;
        try
        {
            var menuText = _contentCatalog.GetMenuText();
            ApplyStaticText(menuText);
            ApplyPendingSettingsToUi(menuText);
            RefreshDirtyState();
        }
        finally
        {
            _initializing = false;
        }
    }

    private void RefreshDirtyState()
    {
        _applyButton.Disabled = !HasPendingChanges();
    }

    private bool HasPendingChanges()
    {
        return !AreAudioSettingsEqual(_appliedAudio, _pendingAudio) ||
               !AreGraphicsSettingsEqual(_appliedGraphics, _pendingGraphics) ||
               !AreLocalizationSettingsEqual(_appliedLocalization, _pendingLocalization);
    }

    private static bool AreAudioSettingsEqual(AudioSettings left, AudioSettings right)
    {
        return Mathf.IsEqualApprox(left.MasterVolume, right.MasterVolume) &&
               Mathf.IsEqualApprox(left.BgmVolume, right.BgmVolume) &&
               Mathf.IsEqualApprox(left.SfxVolume, right.SfxVolume);
    }

    private static bool AreGraphicsSettingsEqual(GraphicsSettings left, GraphicsSettings right)
    {
        return left.Fullscreen == right.Fullscreen &&
               left.ResolutionWidth == right.ResolutionWidth &&
               left.ResolutionHeight == right.ResolutionHeight;
    }

    private static bool AreLocalizationSettingsEqual(LocalizationSettings left, LocalizationSettings right)
    {
        return string.Equals(left.Language, right.Language, StringComparison.Ordinal);
    }

    private async Task PreviewPendingAudioAsync()
    {
        var requestId = Interlocked.Increment(ref _latestAudioPreviewRequestId);
        var audioSnapshot = CloneSettings(_pendingAudio);
        await ApplyLatestPreviewAudioAsync(audioSnapshot, requestId).ConfigureAwait(true);
    }

    private async Task RestoreCommittedSettingsAsync(
        AudioSettings audio,
        GraphicsSettings graphics,
        LocalizationSettings localization,
        bool audioChanged,
        bool graphicsChanged,
        bool localizationChanged)
    {
        LoadSettingsModel(audio, graphics, localization);

        if (audioChanged)
            await ApplyAudioSettingsSnapshotAsync(CloneSettings(audio)).ConfigureAwait(true);
        else
            await DrainAudioPreviewRequestsAsync().ConfigureAwait(true);

        if (graphicsChanged) await _settingsSystem.Apply<GodotGraphicsSettings>().ConfigureAwait(true);

        if (localizationChanged) await _settingsSystem.Apply<GodotLocalizationSettings>().ConfigureAwait(true);

        RefreshLocalizedUi();
    }

    private void LoadSettingsModel(
        AudioSettings audio,
        GraphicsSettings graphics,
        LocalizationSettings localization)
    {
        _settingsModel.GetData<AudioSettings>().LoadFrom(audio);
        _settingsModel.GetData<GraphicsSettings>().LoadFrom(graphics);
        _settingsModel.GetData<LocalizationSettings>().LoadFrom(localization);
    }

    private static AudioSettings CloneSettings(AudioSettings source)
    {
        var clone = new AudioSettings();
        clone.LoadFrom(source);
        return clone;
    }

    private static GraphicsSettings CloneSettings(GraphicsSettings source)
    {
        var clone = new GraphicsSettings();
        clone.LoadFrom(source);
        return clone;
    }

    private static LocalizationSettings CloneSettings(LocalizationSettings source)
    {
        var clone = new LocalizationSettings();
        clone.LoadFrom(source);
        return clone;
    }

    private async Task RevertPendingAudioPreviewIfNeededAsync()
    {
        if (AreAudioSettingsEqual(_appliedAudio, _pendingAudio))
        {
            await DrainAudioPreviewRequestsAsync().ConfigureAwait(true);
            return;
        }

        _pendingAudio.LoadFrom(_appliedAudio);
        await ApplyAudioSettingsSnapshotAsync(CloneSettings(_appliedAudio)).ConfigureAwait(true);
    }

    private async void StartPreviewPendingAudio()
    {
        try
        {
            await PreviewPendingAudioAsync().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Error("预览音量设置失败。", ex);
        }
    }

    private async Task ApplyLatestPreviewAudioAsync(AudioSettings audioSnapshot, int requestId)
    {
        if (requestId != Volatile.Read(ref _latestAudioPreviewRequestId)) return;

        await _audioApplySemaphore.WaitAsync().ConfigureAwait(true);
        try
        {
            if (requestId != Volatile.Read(ref _latestAudioPreviewRequestId)) return;

            await ApplyAudioSettingsSnapshotCoreAsync(audioSnapshot).ConfigureAwait(true);
        }
        finally
        {
            _audioApplySemaphore.Release();
        }
    }

    private async Task ApplyAudioSettingsSnapshotAsync(AudioSettings audioSnapshot)
    {
        Interlocked.Increment(ref _latestAudioPreviewRequestId);

        await _audioApplySemaphore.WaitAsync().ConfigureAwait(true);
        try
        {
            await ApplyAudioSettingsSnapshotCoreAsync(audioSnapshot).ConfigureAwait(true);
        }
        finally
        {
            _audioApplySemaphore.Release();
        }
    }

    private async Task DrainAudioPreviewRequestsAsync()
    {
        Interlocked.Increment(ref _latestAudioPreviewRequestId);

        await _audioApplySemaphore.WaitAsync().ConfigureAwait(true);
        try
        {
        }
        finally
        {
            _audioApplySemaphore.Release();
        }
    }

    private async Task ApplyAudioSettingsSnapshotCoreAsync(AudioSettings audioSnapshot)
    {
        _settingsModel.GetData<AudioSettings>().LoadFrom(audioSnapshot);
        await _settingsSystem.Apply<GodotAudioSettings>().ConfigureAwait(true);
    }
}

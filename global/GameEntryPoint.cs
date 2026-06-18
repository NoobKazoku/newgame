using GFrameworkGodotTemplate.scripts.core;
using GFrameworkGodotTemplate.scripts.core.environment;
using GFrameworkGodotTemplate.scripts.core.resource;
using GFrameworkGodotTemplate.scripts.core.state.impls;
using GFrameworkGodotTemplate.scripts.enums.scene;
using GFrameworkGodotTemplate.scripts.utility;
using Godot;
using Godot.Collections;

namespace GFrameworkGodotTemplate.global;

/// <summary>
///     游戏入口点节点类，负责初始化游戏架构和管理全局游戏状态
/// </summary>
[Log]
[ContextAware]
public partial class GameEntryPoint : Node
{
    private bool _quitRequested;
    private IGodotSceneRegistry _sceneRegistry = null!;
    private ISettingsModel _settingsModel = null!;
    private ISettingsSystem _settingsSystem = null!;
    private IGodotTextureRegistry _textureRegistry = null!;
    private IGodotUiRegistry _uiRegistry = null!;
    public static IArchitecture Architecture { get; private set; } = null!;
    public static SceneTree Tree { get; private set; } = null!;
    [Export] public bool IsDev { get; set; } = true;

    /// <summary>
    ///     UI页面配置数组，包含所有可用的UI页面配置项
    /// </summary>
    /// <value>
    ///     存储UiPageConfig对象的数组集合
    /// </value>
    [Export]
    public Array<UiPageConfig> UiPageConfigs { get; set; } = null!;

    [Export] public Array<SceneConfig> GameSceneConfigs { get; set; } = null!;

    [Export] public Array<TextureConfig> TextureConfigs { get; set; } = null!;

    /// <summary>
    ///     Godot引擎调用的节点就绪方法，在此方法中初始化游戏架构和相关组件
    /// </summary>
    public override async void _Ready()
    {
        // 获取游戏根节点
        Tree = GetTree();
        Tree.AutoAcceptQuit = false;
        // 创建并初始化游戏架构实例
        // 配置架构的日志记录属性，设置Godot日志工厂提供程序并指定最低日志级别为调试级别
        // 然后初始化架构实例以准备游戏运行环境
        Architecture = new GameArchitecture(new ArchitectureConfiguration
        {
            LoggerProperties = new LoggerProperties
            {
                LoggerFactoryProvider = new GodotLoggerFactoryProvider
                {
                    MinLevel = LogLevel.Debug
                }
            }
        }, IsDev ? new GameDevEnvironment() : new GameMainEnvironment());
        Architecture.Initialize();
        _settingsModel = this.GetModel<ISettingsModel>()!;
        _settingsSystem = this.GetSystem<ISettingsSystem>()!;
        _sceneRegistry = this.GetUtility<IGodotSceneRegistry>()!;
        _uiRegistry = this.GetUtility<IGodotUiRegistry>()!;
        _textureRegistry = this.GetUtility<IGodotTextureRegistry>()!;
        await InitializeSettingsAsync().ConfigureAwait(true);
        // 注册所有游戏场景配置到场景注册表中
        foreach (var gameSceneConfig in GameSceneConfigs) _sceneRegistry.Registry(gameSceneConfig);

        // 注册所有UI页面配置到UI注册表中
        foreach (var uiPageConfig in UiPageConfigs) _uiRegistry.Registry(uiPageConfig);

        // 注册所有纹理配置
        foreach (var textureConfig in TextureConfigs) _textureRegistry.Registry(textureConfig);

        _log.Debug("GameEntryPoint ready.");
        CallDeferred(nameof(CallDeferredInit));
    }

    private static void CallDeferredInit()
    {
        // 协程预热
        Timing.Prewarm();
    }

    /// <summary>
    ///     异步初始化并应用设置，避免事件订阅顺序导致启动时漏应用持久化设置。
    /// </summary>
    private async Task InitializeSettingsAsync()
    {
        try
        {
            await _settingsModel.InitializeAsync().ConfigureAwait(true);

            if (!_settingsModel.IsInitialized) return;

            await _settingsSystem.ApplyAll().ConfigureAwait(true);
            _log.Info("设置已加载");
        }
        catch (Exception ex)
        {
            _log.Error("启动时初始化设置失败。", ex);
        }
    }

    private void StartBootState()
    {
        this.GetSystem<IStateMachineSystem>()!
            .ChangeToAsync<BootStartState>()
            .ToCoroutineEnumerator()
            .RunCoroutine();
    }

    public override void _Notification(int what)
    {
        if (what == (int)NotificationWMCloseRequest || what == (int)NotificationWMGoBackRequest)
        {
            RequestQuitAsync();
            return;
        }

        base._Notification(what);
    }


    /// <summary>
    ///     判断当前场景是否为主菜单场景，决定是否需要进入主菜单状态
    /// </summary>
    /// <returns>如果当前场景是主菜单场景则返回true，否则返回false</returns>
    private bool ShouldEnterMainMenu()
    {
        var tree = GetTree();
        var currentScene = tree.CurrentScene;

        if (currentScene == null)
            return false;

        var scenePath = currentScene.SceneFilePath;
        return string.Equals(scenePath, _sceneRegistry.Get(nameof(SceneKey.Main)).GetPath(),
            StringComparison.Ordinal);
    }

    /// <summary>
    ///     处理窗口关闭/返回请求，先异步保存设置再退出。
    /// </summary>
    private async void RequestQuitAsync()
    {
        if (_quitRequested)
            return;

        _quitRequested = true;
        try
        {
            await _settingsSystem.SaveAll().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _log.Error("退出时保存设置失败。", ex);
        }

        Tree.Quit();
    }
}

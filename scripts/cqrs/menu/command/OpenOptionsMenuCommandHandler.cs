using GFrameworkGodotTemplate.scripts.options_menu;

namespace GFrameworkGodotTemplate.scripts.cqrs.menu.command;

/// <summary>
///     打开选项菜单命令处理器
/// </summary>
public partial class OpenOptionsMenuCommandHandler : AbstractCommandHandler<OpenOptionsMenuCommand>
{
    [GetSystem] private IUiRouter _uiRouter = null!;

    public override ValueTask<Unit> Handle(OpenOptionsMenuCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        _uiRouter.Show(OptionsMenu.UiKeyStr, UiLayer.Modal);
        return ValueTask.FromResult(Unit.Value);
    }
}
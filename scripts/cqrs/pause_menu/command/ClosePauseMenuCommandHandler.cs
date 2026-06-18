namespace GFrameworkGodotTemplate.scripts.cqrs.pause_menu.command;

/// <summary>
///     关闭暂停菜单命令处理器
/// </summary>
public partial class ClosePauseMenuCommandHandler : AbstractCommandHandler<ClosePauseMenuCommand>
{
    [GetSystem] private IUiRouter _uiRouter = null!;

    public override ValueTask<Unit> Handle(ClosePauseMenuCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        var input = command.Input;
        _uiRouter.Hide(input.Handle, UiLayer.Modal);
        return ValueTask.FromResult(Unit.Value);
    }
}
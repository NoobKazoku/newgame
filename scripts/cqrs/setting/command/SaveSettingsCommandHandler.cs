namespace GFrameworkGodotTemplate.scripts.cqrs.setting.command;

/// <summary>
///     保存游戏设置命令处理器
/// </summary>
public partial class SaveSettingsCommandHandler : AbstractCommandHandler<SaveSettingsCommand>
{
    [GetSystem] private ISettingsSystem _settingsSystem = null!;

    public override async ValueTask<Unit> Handle(SaveSettingsCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        await _settingsSystem.SaveAll().ConfigureAwait(true);
        return Unit.Value;
    }
}
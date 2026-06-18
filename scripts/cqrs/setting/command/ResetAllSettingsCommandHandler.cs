namespace GFrameworkGodotTemplate.scripts.cqrs.setting.command;

/// <summary>
///     重置所有设置命令处理器
/// </summary>
public partial class ResetAllSettingsCommandHandler : AbstractCommandHandler<ResetAllSettingsCommand>
{
    [GetSystem] private ISettingsSystem _settingsSystem = null!;

    public override async ValueTask<Unit> Handle(ResetAllSettingsCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        await _settingsSystem.ResetAll().ConfigureAwait(true);
        return Unit.Value;
    }
}
namespace GFrameworkGodotTemplate.scripts.cqrs.setting.command;

/// <summary>
///     更改语言命令处理器
/// </summary>
public partial class ChangeLanguageCommandHandler : AbstractCommandHandler<ChangeLanguageCommand>
{
    [GetModel] private ISettingsModel _model = null!;

    [GetSystem] private ISettingsSystem _settingsSystem = null!;

    public override async ValueTask<Unit> Handle(ChangeLanguageCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        var input = command.Input;
        var settings = _model.GetData<LocalizationSettings>();
        settings.Language = input.Language;
        await _settingsSystem.Apply<GodotLocalizationSettings>()
            .ConfigureAwait(true);
        return Unit.Value;
    }
}
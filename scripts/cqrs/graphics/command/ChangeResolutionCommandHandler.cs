namespace GFrameworkGodotTemplate.scripts.cqrs.graphics.command;

/// <summary>
///     更改分辨率命令处理器
/// </summary>
public partial class ChangeResolutionCommandHandler : AbstractCommandHandler<ChangeResolutionCommand>
{
    [GetModel] private ISettingsModel _model = null!;

    [GetSystem] private ISettingsSystem _settingsSystem = null!;

    public override async ValueTask<Unit> Handle(ChangeResolutionCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        var input = command.Input;
        var settings = _model.GetData<GraphicsSettings>();
        settings.ResolutionWidth = input.Width;
        settings.ResolutionHeight = input.Height;
        await _settingsSystem.Apply<GodotGraphicsSettings>()
            .ConfigureAwait(true);
        return Unit.Value;
    }
}
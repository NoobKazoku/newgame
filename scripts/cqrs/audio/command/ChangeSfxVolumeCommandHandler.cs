namespace GFrameworkGodotTemplate.scripts.cqrs.audio.command;

/// <summary>
///     更改音效音量命令处理器
/// </summary>
public partial class ChangeSfxVolumeCommandHandler : AbstractCommandHandler<ChangeSfxVolumeCommand>
{
    [GetModel] private ISettingsModel _model = null!;

    [GetSystem] private ISettingsSystem _settingsSystem = null!;

    public override async ValueTask<Unit> Handle(ChangeSfxVolumeCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        var input = command.Input;
        _model.GetData<AudioSettings>().SfxVolume = input.Volume;
        await _settingsSystem.Apply<GodotAudioSettings>().ConfigureAwait(true);
        return Unit.Value;
    }
}
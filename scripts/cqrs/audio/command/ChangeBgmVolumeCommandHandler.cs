namespace GFrameworkGodotTemplate.scripts.cqrs.audio.command;

/// <summary>
///     更改背景音乐音量命令处理器
/// </summary>
public partial class ChangeBgmVolumeCommandHandler : AbstractCommandHandler<ChangeBgmVolumeCommand>
{
    [GetModel] private ISettingsModel _model = null!;

    [GetSystem] private ISettingsSystem _settingsSystem = null!;

    public override async ValueTask<Unit> Handle(ChangeBgmVolumeCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        var input = command.Input;
        _model.GetData<AudioSettings>().BgmVolume = input.Volume;
        await _settingsSystem.Apply<GodotAudioSettings>().ConfigureAwait(true);
        return Unit.Value;
    }
}
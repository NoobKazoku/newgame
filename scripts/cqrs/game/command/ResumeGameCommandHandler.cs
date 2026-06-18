using GFrameworkGodotTemplate.scripts.core.state.impls;

namespace GFrameworkGodotTemplate.scripts.cqrs.game.command;

/// <summary>
///     恢复游戏命令处理器
/// </summary>
public partial class ResumeGameCommandHandler : AbstractCommandHandler<ResumeGameCommand>
{
    /// <summary>
    ///     状态机系统实例，用于切换回进行中状态。
    /// </summary>
    [GetSystem] private IStateMachineSystem _stateMachineSystem = null!;

    /// <summary>
    ///     处理恢复游戏命令。
    ///     该命令只负责恢复状态，树级暂停由 UI 路由隐藏暂停页时自动释放。
    /// </summary>
    /// <param name="command">恢复游戏命令对象。</param>
    /// <param name="cancellationToken">取消令牌，用于在开始切换前终止操作。</param>
    /// <returns>表示恢复流程完成的异步结果。</returns>
    public override async ValueTask<Unit> Handle(ResumeGameCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        cancellationToken.ThrowIfCancellationRequested();

        await _stateMachineSystem.ChangeToAsync<PlayingState>().ConfigureAwait(true);
        return Unit.Value;
    }
}
// Copyright (c) 2026 GeWuYou
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using GFrameworkGodotTemplate.scripts.core.state.impls;

namespace GFrameworkGodotTemplate.scripts.cqrs.game.command;

/// <summary>
///     暂停游戏命令处理器类，负责处理暂停游戏的命令逻辑
///     继承自AbstractCommandHandler，专门处理PauseGameCommand类型的命令
/// </summary>
public partial class PauseGameCommandHandler : AbstractCommandHandler<PauseGameCommand>
{
    /// <summary>
    ///     状态机系统实例，用于切换状态
    /// </summary>
    [GetSystem] private IStateMachineSystem _stateMachineSystem = null!;

    /// <summary>
    ///     处理暂停游戏命令的核心方法。
    ///     该命令只负责切换状态机，真正的树级暂停交给 UI 路由与暂停栈处理。
    /// </summary>
    /// <param name="command">暂停游戏命令对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作完成的ValueTask，返回Unit值表示无返回结果</returns>
    public override async ValueTask<Unit> Handle(PauseGameCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        cancellationToken.ThrowIfCancellationRequested();

        await _stateMachineSystem.ChangeToAsync<PausedState>().ConfigureAwait(true);
        return Unit.Value;
    }
}
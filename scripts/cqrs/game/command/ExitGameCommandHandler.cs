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

using GFrameworkGodotTemplate.scripts.core.utils;
using Godot;

namespace GFrameworkGodotTemplate.scripts.cqrs.game.command;

/// <summary>
///     退出游戏命令处理器类，负责处理退出游戏的命令逻辑
///     继承自AbstractCommandHandler，专门处理ExitGameCommand类型的命令
/// </summary>
public partial class ExitGameCommandHandler : AbstractCommandHandler<ExitGameCommand>
{
    [GetSystem] private ISettingsSystem? _settingsSystem;

    /// <summary>
    ///     处理退出游戏命令的核心方法
    ///     通过调用GameUtil获取场景树并执行退出操作
    /// </summary>
    /// <param name="command">退出游戏命令对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>表示异步操作完成的ValueTask，返回Unit值表示无返回结果</returns>
    public override async ValueTask<Unit> Handle(ExitGameCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        if (_settingsSystem is not null)
            try
            {
                await _settingsSystem.SaveAll().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Failed to save settings on exit: {ex.Message}");
            }

        GameUtil.GetTree().Quit();
        return Unit.Value;
    }
}
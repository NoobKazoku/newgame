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


namespace GFrameworkGodotTemplate.scripts.cqrs.graphics.command;

/// <summary>
///     切换全屏模式命令处理器类
///     负责处理ToggleFullscreenCommand命令，更新图形设置并应用更改
/// </summary>
public partial class ToggleFullscreenCommandHandler : AbstractCommandHandler<ToggleFullscreenCommand>
{
    [GetModel] private ISettingsModel _model = null!;

    [GetSystem] private ISettingsSystem _settingsSystem = null!;

    /// <summary>
    ///     处理切换全屏模式命令的核心方法
    /// </summary>
    /// <param name="command">切换全屏命令对象，包含目标全屏状态</param>
    /// <param name="cancellationToken">取消令牌，用于取消异步操作</param>
    /// <returns>表示操作完成的Unit值</returns>
    public override async ValueTask<Unit> Handle(ToggleFullscreenCommand command, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        cancellationToken.ThrowIfCancellationRequested();

        var settings = _model.GetData<GraphicsSettings>();
        var previousFullscreen = settings.Fullscreen;
        settings.Fullscreen = command.Input.Fullscreen;

        try
        {
            await _settingsSystem.Apply<GodotGraphicsSettings>().ConfigureAwait(true);
            return Unit.Value;
        }
        catch
        {
            settings.Fullscreen = previousFullscreen;
            throw;
        }
    }
}
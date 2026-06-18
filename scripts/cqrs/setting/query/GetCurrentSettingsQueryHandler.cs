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

using GFrameworkGodotTemplate.scripts.cqrs.setting.query.view;

namespace GFrameworkGodotTemplate.scripts.cqrs.setting.query;

/// <summary>
///     当前设置查询处理器
///     处理获取当前设置信息的查询请求，从设置模型中提取各类设置数据并构建成视图对象
/// </summary>
public partial class GetCurrentSettingsQueryHandler : AbstractQueryHandler<GetCurrentSettingsQuery, SettingsView>
{
    [GetModel] private ISettingsModel _settingsModel = null!;

    /// <summary>
    ///     处理获取当前设置的查询请求
    /// </summary>
    /// <param name="query">获取当前设置的查询对象</param>
    /// <param name="cancellationToken">取消令牌，用于取消操作</param>
    /// <returns>包含音频、图形和本地化设置的设置视图对象</returns>
    public override ValueTask<SettingsView> Handle(GetCurrentSettingsQuery query, CancellationToken cancellationToken)
    {
        __InjectContextBindings_Generated();
        // 在此可以校验设置数据
        // 构建并返回设置视图对象
        return ValueTask.FromResult(new SettingsView
        {
            Audio = _settingsModel.GetData<AudioSettings>(),
            Graphics = _settingsModel.GetData<GraphicsSettings>(),
            Localization = _settingsModel.GetData<LocalizationSettings>()
        });
    }
}

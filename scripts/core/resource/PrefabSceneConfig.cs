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

using GFrameworkGodotTemplate.scripts.enums.resources;
using Godot;

namespace GFrameworkGodotTemplate.scripts.core.resource;

/// <summary>
///     预制体场景配置资源，用于注册运行时实例化的 PackedScene。
/// </summary>
[GlobalClass]
public partial class PrefabSceneConfig : Resource, IKeyValue<string, PackedScene>
{
    /// <summary>
    ///     预制体资源键。
    /// </summary>
    [Export]
    public PrefabKey PrefabKey { get; private set; }

    /// <summary>
    ///     对应的预制体场景。
    /// </summary>
    [Export]
    public PackedScene Scene { get; private set; } = null!;

    /// <summary>
    ///     键值对中的键。
    /// </summary>
    public string Key => PrefabKey.ToString();

    /// <summary>
    ///     键值对中的值。
    /// </summary>
    public PackedScene Value => Scene;
}

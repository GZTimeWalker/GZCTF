﻿using System.Diagnostics.CodeAnalysis;
using ConfigModel = GZCTF.Models.Data.Config;

namespace GZCTF.Services.Config;

public interface IConfigService
{
    /// <summary>
    /// 保存配置对象
    /// </summary>
    /// <typeparam name="T">选项类型</typeparam>
    /// <param name="config">选项对象</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SaveConfig<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T config,
        CancellationToken token = default) where T : class;

    /// <summary>
    /// 保存配置对象
    /// </summary>
    /// <param name="type">对象类型</param>
    /// <param name="value">对象值</param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SaveConfig([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
        object? value, CancellationToken token = default);

    /// <summary>
    /// 保存配置键值对
    /// </summary>
    /// <param name="configs">键值对</param>
    /// <param name="token"></param>
    public Task SaveConfigSet(HashSet<ConfigModel> configs, CancellationToken token = default);

    /// <summary>
    /// 重载配置
    /// </summary>
    public void ReloadConfig();
}

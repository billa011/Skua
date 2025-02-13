﻿using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Scripts;
using Skua.Core.Utils;
using Skua.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Skua.Core.AppStartup;
internal class Options
{
    internal static GameOptionsViewModel CreateGameOptions(IServiceProvider s)
    {
        IScriptOption scriptOpt = s.GetService<IScriptOption>()!;

        List<DisplayOptionItemViewModelBase> options = new()
        {
            new CommandOptionItemViewModel<bool>("Upgrade", "1", CreateBoolCommand(b => Ioc.Default.GetRequiredService<IFlashUtil>().SetGameObject("world.myAvatar.pMC.pname.ti.textColor", b ? 0x8CD5FF : 0xFFFFFF))),
            new CommandOptionItemViewModel<bool>("Staff", "1", CreateBoolCommand(b => Ioc.Default.GetRequiredService<IFlashUtil>().SetGameObject("world.myAvatar.pMC.pname.ti.textColor", b ? 0xFECB38 : 0xFFFFFF))),
            new CommandOptionItemViewModel<IRelayCommand>("Reload Map", "4", new RelayCommand(Ioc.Default.GetRequiredService<IScriptMap>().Reload))
        };

        foreach (PropertyInfo pi in scriptOpt.GetType().GetProperties())
        {
            if (pi.Name is nameof(IScriptOption.OptionDictionary) or nameof(ObservableRecipient.IsActive) or nameof(IScriptOption.GuildColor) or nameof(IScriptOption.NameColor))
                continue;
            if (pi.PropertyType == typeof(bool))
                options.Add(CreateOptionItem<bool>(s, pi.Name, "1", new RelayCommand<bool>(b => pi.SetValue(scriptOpt, b))));
            else if (pi.PropertyType == typeof(string))
                options.Add(CreateOptionItem<string>(s, pi.Name, "2", new RelayCommand<string>(value => pi.SetValue(scriptOpt, value))));
            else if (pi.PropertyType == typeof(int))
            {
                options.Add(CreateOptionItem<int>(s, pi.Name, "3", new RelayCommand<string>(value =>
                {
                    if (!int.TryParse(value, out int result))
                        return;
                    pi.SetValue(scriptOpt, result);
                })));
            }
        }

        return new GameOptionsViewModel(options, s.GetService<IScriptServers>()!, scriptOpt);

        static RelayCommand<bool> CreateBoolCommand(Action<bool> action)
        {
            return new RelayCommand<bool>(action);
        }
        static BindingOptionItemViewModel<T, ScriptOption> CreateOptionItem<T>(IServiceProvider s, string binding, string tag, IRelayCommand command)
        {
            return new BindingOptionItemViewModel<T, ScriptOption>(s.GetRequiredService<IDecamelizer>().Decamelize(binding, null), tag, binding, (ScriptOption)s.GetRequiredService<IScriptOption>(), command);
        }
    }

    internal static ApplicationOptionsViewModel CreateAppOptions(IServiceProvider s)
    {
        List<DisplayOptionItemViewModelBase> appOptions = new()
        {
            CreateSettingOptionItem<bool>("Auto Update Scripts", "Whether to auto update scripts when launching the Manager, needs \"Check for Scripts updates\" to be true", "AutoUpdateScripts"),
            CreateSettingOptionItem<bool>("Check for Script Updates", "Whether to check for scripts updates when launching the Manager", "CheckScriptUpdates"),
            new CommandOptionItemViewModel<int>("* Client Animation Framerate", "ClientAnim", new RelayCommand<string>(value =>
            {
                if (!int.TryParse(value, out int result) || result < 1)
                    return;
                Ioc.Default.GetRequiredService<ISettingsService>().Set("AnimationFrameRate", result);
            }), s.GetRequiredService<ISettingsService>().Get<int>("AnimationFrameRate"))
        };

        return new ApplicationOptionsViewModel(appOptions);

        static RelayCommand<T> CreateSettingCommand<T>(string key)
        {
            return new RelayCommand<T>(b => Ioc.Default.GetRequiredService<ISettingsService>().Set(key, b));
        }
        static CommandOptionItemViewModel<T> CreateSettingOptionItem<T>(string content, string description, string key)
        {
            return new(content, description, key, CreateSettingCommand<T>(key), Ioc.Default.GetRequiredService<ISettingsService>().Get<T>(key));
        }
    }
}

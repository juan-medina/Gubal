using System;
using System.Collections.Generic;
using Dalamud.Game.Command;

namespace Gubal;

public class SearchService : IDisposable
{
    private readonly Configuration configuration;
    private readonly HashSet<string> registeredCommands = new(StringComparer.OrdinalIgnoreCase);

    public SearchService(Configuration? configuration = null)
    {
        this.configuration = configuration ?? new Configuration();
        ReloadCommands();
    }

    public void search(string command, string args)
    {
        if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(args)) return;

        if (configuration.SearchCommands.TryGetValue(command, out var searchCommand))
        {
            if (!searchCommand.Enabled) return;


            var url = searchCommand.Url.Replace("{text}", args, StringComparison.OrdinalIgnoreCase);
            if (!UrlValidator.IsValidUrl(url)) return;

            Dalamud.Utility.Util.OpenLink(url);
        }
    }

    public void ReloadCommands()
    {
        foreach (var name in registeredCommands) Plugin.CommandManager.RemoveHandler(name);

        registeredCommands.Clear();

        foreach (var (name, cmd) in configuration.SearchCommands)
        {
            if (Plugin.CommandManager.Commands.ContainsKey(name)) continue;

            Plugin.CommandManager.AddHandler(name, new CommandInfo(OnCommand)
            {
                HelpMessage = cmd.HelpMessage
            });
            registeredCommands.Add(name);
        }
    }

    public bool AddCommand(string name, SearchCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(name) || !UrlValidator.IsValidUrl(cmd.Url)) return false;

        configuration.SearchCommands[name] = cmd;
        configuration.Save();
        ReloadCommands();
        return true;
    }

    public bool RemoveCommand(string name)
    {
        if (!configuration.SearchCommands.Remove(name)) return false;

        configuration.Save();
        ReloadCommands();
        return true;
    }

    public void UpdateCommand(string name, SearchCommand cmd)
    {
        configuration.SearchCommands[name] = cmd;
        configuration.Save();
        ReloadCommands();
    }

    public void ResetToDefaults()
    {
        configuration.Reset();
        ReloadCommands();
    }

    public void Dispose()
    {
        foreach (var name in registeredCommands) Plugin.CommandManager.RemoveHandler(name);
        registeredCommands.Clear();
        GC.SuppressFinalize(this);
    }

    public void OnCommand(string command, string args) => search(command, args);
}




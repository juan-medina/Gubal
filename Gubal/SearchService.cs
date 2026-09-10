using System;
using System.Collections.Generic;
using Dalamud.Game.Command;

namespace Gubal;

public class SearchService : IDisposable
{
    private readonly Configuration _configuration;
    private readonly HashSet<string> _registeredCommands = new(StringComparer.OrdinalIgnoreCase);

    public SearchService(Configuration? configuration = null)
    {
        _configuration = configuration ?? new Configuration();
        ReloadCommands();
    }

    public bool IsValidUrl(string url) => UrlValidator.IsValidUrl(url);

    public void search(string command, string args)
    {
        if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(args))
        {
            return;
        }

        if (_configuration.SearchCommands.TryGetValue(command, out var searchCommand))
        {
            if (!searchCommand.Enabled)
            {
                return;
            }

            string url = searchCommand.Url.Replace("{text}", args, StringComparison.OrdinalIgnoreCase);
            if (!IsValidUrl(url))
            {
                return;
            }

            Dalamud.Utility.Util.OpenLink(url);
        }
    }

    public void ReloadCommands()
    {
        foreach (var name in _registeredCommands)
        {
            Plugin.CommandManager.RemoveHandler(name);
        }
        _registeredCommands.Clear();

        foreach (var (name, cmd) in _configuration.SearchCommands)
        {
            if (Plugin.CommandManager.Commands.ContainsKey(name))
            {
                continue;
            }
            Plugin.CommandManager.AddHandler(name, new CommandInfo(OnCommand)
            {
                HelpMessage = cmd.HelpMessage
            });
            _registeredCommands.Add(name);
        }
    }

    public bool AddCommand(string name, SearchCommand cmd)
    {
        if (string.IsNullOrWhiteSpace(name) || !UrlValidator.IsValidUrl(cmd.Url))
        {
            return false;
        }

        _configuration.SearchCommands[name] = cmd;
        _configuration.Save();
        ReloadCommands();
        return true;
    }

    public bool RemoveCommand(string name)
    {
        if (!_configuration.SearchCommands.Remove(name))
        {
            return false;
        }

        _configuration.Save();
        ReloadCommands();
        return true;
    }

    public void UpdateCommand(string name, SearchCommand cmd)
    {
        _configuration.SearchCommands[name] = cmd;
        _configuration.Save();
        ReloadCommands();
    }

    public void ResetToDefaults()
    {
        _configuration.Reset();
        ReloadCommands();
    }

    public void Dispose()
    { 
        foreach (var name in _registeredCommands)
        {
            Plugin.CommandManager.RemoveHandler(name);
        }
        _registeredCommands.Clear();
    }
    
    public void OnCommand(string command, string args)
    {
        search(command, args);
    }
}




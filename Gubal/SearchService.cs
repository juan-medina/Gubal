

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;

namespace Gubal;

public partial class SearchService : IDisposable
{
    private readonly Configuration _configuration;

    [GeneratedRegex(@"^https:\/\/[a-zA-Z0-9\-\.]+(:\d+)?(\/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    public SearchService(Configuration? configuration = null)
    {
        _configuration = configuration ?? new Configuration();
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
        }
    }

    public bool IsValidUrl(string url) => !string.IsNullOrEmpty(url) && UrlRegex().IsMatch(url);

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

            string url = searchCommand.Url.Replace("{text}", args);
            if (!IsValidUrl(url))
            {
                return;
            }

            Dalamud.Utility.Util.OpenLink(url);
        }
    }

    public void Dispose()
    { 
        foreach (var name in _configuration.SearchCommands.Keys)
        {
            Plugin.CommandManager.RemoveHandler(name);
        }
    }
    
    public void OnCommand(string command, string args)
    {
        search(command, args);
    }
}




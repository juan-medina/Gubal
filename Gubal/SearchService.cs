

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;

namespace Gubal;

public record SearchCommand(string Url, string HelpMessage, bool Enabled = true);

public partial class SearchService : IDisposable
{
    private readonly Dictionary<string, SearchCommand> _commands = new(StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex(@"^https:\/\/[a-zA-Z0-9\-\.]+(:\d+)?(\/.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    public SearchService()
    {
        _commands.Add("/wiki", new SearchCommand("https://ffxiv.consolegameswiki.com/mediawiki/index.php?search={text}", "Search in wiki"));
        foreach (var (name, cmd) in _commands)
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

        if (_commands.TryGetValue(command, out var searchCommand))
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
        foreach (var name in _commands.Keys)
        {
            Plugin.CommandManager.RemoveHandler(name);
        }
    }
    
    public void OnCommand(string command, string args)
    {
        search(command, args);
    }
}




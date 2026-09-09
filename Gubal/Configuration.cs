using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace Gubal;

public record SearchCommand(string Url, string HelpMessage, bool Enabled = true);

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public static Dictionary<string, SearchCommand> DefaultSearchCommands => new(StringComparer.OrdinalIgnoreCase)
    {
        { "/wiki", new SearchCommand("https://ffxiv.consolegameswiki.com/mediawiki/index.php?search={text}", "Search in the FFXIV wiki") },
        { "/lodestone", new SearchCommand("https://www.google.com/search?btnI=I&q=site:finalfantasyxiv.com/lodestone+{text}", "Search in the Lodestone") },
    };

    public Dictionary<string, SearchCommand> SearchCommands { get; set; } = DefaultSearchCommands;

    public void Reset()
    {
        SearchCommands = DefaultSearchCommands;
        Save();
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}

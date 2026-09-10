using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace Gubal.Windows;

public class ConfigWindow : Window, IDisposable
{
    private sealed class CommandEntry
    {
        public string Name { get; set; } = string.Empty;
        public string HelpMessage { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    private readonly Plugin plugin;
    private readonly Configuration configuration;

    private readonly List<CommandEntry> commandEntries = [];
    private string newCommandName = string.Empty;
    private string newHelpMessage = string.Empty;
    private string newUrl = string.Empty;
    private string errorMessage = string.Empty;

    public ConfigWindow(Plugin plugin) : base("Gubal Configuration###GubalConfigWindow2")
    {
        Size = new Vector2(750, 420);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.plugin = plugin;
        configuration = plugin.Configuration;

        LoadFromConfig();
    }

    public override void OnOpen()
    {
        LoadFromConfig();
        errorMessage = string.Empty;
    }

    public override void OnClose()
    {
        LoadFromConfig();
        newCommandName = string.Empty;
        newHelpMessage = string.Empty;
        newUrl = string.Empty;
        errorMessage = string.Empty;
    }

    public void Dispose() { }

    private void LoadFromConfig()
    {
        commandEntries.Clear();
        foreach (var (name, cmd) in configuration.SearchCommands)
        {
            commandEntries.Add(new CommandEntry
            {
                Name = name,
                HelpMessage = cmd.HelpMessage,
                Url = cmd.Url,
                Enabled = cmd.Enabled
            });
        }
    }

    public override void Draw()
    {
        ImGui.TextUnformatted("Configure custom slash commands to search web resources directly from chat.");
        ImGui.SameLine();
        ImGuiComponents.HelpMarker(
            "How Gubal works:\n" +
            "• Enter a slash command (e.g. /wiki).\n" +
            "• Provide a search URL containing {text} where your query will be inserted.\n" +
            "• In chat, type: /wiki Dreadwyrm Trance\n" +
            "  Gubal replaces {text} with 'Dreadwyrm Trance' and opens your browser.");

        ImGui.Spacing();

        var style = ImGui.GetStyle();
        var footerHeight = ImGui.GetFrameHeight() + style.ItemSpacing.Y + 8f;
        if (!string.IsNullOrEmpty(errorMessage))
        {
            footerHeight += ImGui.GetTextLineHeightWithSpacing();
        }

        DrawCommandsTable(footerHeight);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.35f, 0.35f, 1f), errorMessage);
        }

        DrawFooter();
    }

    private void DrawCommandsTable(float footerHeight)
    {
        var tableFlags = ImGuiTableFlags.Borders
                       | ImGuiTableFlags.RowBg
                       | ImGuiTableFlags.Resizable
                       | ImGuiTableFlags.ScrollY;

        if (ImGui.BeginTable("SearchCommandsTable", 6, tableFlags, new Vector2(0, -footerHeight)))
        {
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30);
            ImGui.TableSetupColumn("Command", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Help Message", ImGuiTableColumnFlags.WidthStretch, 160);
            ImGui.TableSetupColumn("URL ({text} placeholder)", ImGuiTableColumnFlags.WidthStretch, 260);
            ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 65);
            ImGui.TableSetupColumn(string.Empty, ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
            ImGui.TableNextColumn();
            ImGui.TableHeader("#");

            ImGui.TableNextColumn();
            ImGui.TableHeader("Command");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("The slash command to type in chat (e.g. /wiki). The leading '/' is added automatically.");
            }

            ImGui.TableNextColumn();
            ImGui.TableHeader("Help Message");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Optional description shown in Dalamud's /xlhelp command list.");
            }

            ImGui.TableNextColumn();
            ImGui.TableHeader("URL ({text} placeholder)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Search URL containing {text}. The placeholder will be replaced with your query arguments.");
            }

            ImGui.TableNextColumn();
            ImGui.TableHeader("Enabled");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Toggle whether this command is active in game without deleting it.");
            }

            ImGui.TableNextColumn();
            ImGui.TableHeader(string.Empty);

            int toRemove = -1;

            for (int i = 0; i < commandEntries.Count; i++)
            {
                var entry = commandEntries[i];
                ImGui.TableNextRow();

                // Column 0: #
                ImGui.TableNextColumn();
                ImGui.TextUnformatted($"{i + 1}");

                // Column 1: Command Name
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var name = entry.Name;
                if (ImGui.InputTextWithHint($"###cmd_{i}", "/command", ref name, 64))
                {
                    entry.Name = name;
                }

                // Column 2: Help Message
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var help = entry.HelpMessage;
                if (ImGui.InputTextWithHint($"###help_{i}", "Optional...", ref help, 256))
                {
                    entry.HelpMessage = help;
                }

                // Column 3: URL
                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                var url = entry.Url;
                if (ImGui.InputTextWithHint($"###url_{i}", "https://... with {text}", ref url, 512))
                {
                    entry.Url = url;
                }

                // Column 4: Enabled Checkbox
                ImGui.TableNextColumn();
                var enabled = entry.Enabled;
                if (ImGui.Checkbox($"###enabled_{i}", ref enabled))
                {
                    entry.Enabled = enabled;
                }

                // Column 5: Trash Icon Button
                ImGui.TableNextColumn();
                if (ImGuiComponents.IconButton($"###del_{i}", FontAwesomeIcon.Trash))
                {
                    toRemove = i;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Delete command");
                }
            }

            if (toRemove >= 0 && toRemove < commandEntries.Count)
            {
                commandEntries.RemoveAt(toRemove);
            }

            // New Row at the bottom
            ImGui.TableNextRow();

            // Column 0: #
            ImGui.TableNextColumn();
            ImGui.TextUnformatted($"{commandEntries.Count + 1}");

            // Column 1: New Command
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var enterCmd = ImGui.InputTextWithHint("###new_cmd", "/command", ref newCommandName, 64, ImGuiInputTextFlags.EnterReturnsTrue);

            // Column 2: New Help Message
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var enterHelp = ImGui.InputTextWithHint("###new_help", "Optional...", ref newHelpMessage, 256, ImGuiInputTextFlags.EnterReturnsTrue);

            // Column 3: New URL
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(-1);
            var enterUrl = ImGui.InputTextWithHint("###new_url", "https://... with {text}", ref newUrl, 512, ImGuiInputTextFlags.EnterReturnsTrue);

            // Column 4: Enabled placeholder (empty cell in new row)
            ImGui.TableNextColumn();

            // Column 5: + Icon Button
            ImGui.TableNextColumn();
            bool canAdd = !string.IsNullOrWhiteSpace(newCommandName);
            bool clickedAdd = false;

            if (canAdd)
            {
                if (ImGuiComponents.IconButton("###addNewCmd", FontAwesomeIcon.Plus))
                {
                    clickedAdd = true;
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Add command");
                }
            }

            if ((clickedAdd || enterCmd || enterHelp || enterUrl) && canAdd)
            {
                TryAddNewCommand();
            }

            ImGui.EndTable();
        }
    }

    private bool TryAddNewCommand()
    {
        var trimmedName = newCommandName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            errorMessage = "Command name cannot be empty.";
            return false;
        }

        var formattedName = "/" + trimmedName.TrimStart('/');
        if (formattedName == "/")
        {
            errorMessage = "Command name cannot be empty.";
            return false;
        }

        if (formattedName.Contains(' '))
        {
            errorMessage = "Command name cannot contain spaces.";
            return false;
        }

        if (commandEntries.Any(c => string.Equals(c.Name, formattedName, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = $"Command '{formattedName}' already exists.";
            return false;
        }

        var trimmedUrl = newUrl.Trim();
        if (string.IsNullOrWhiteSpace(trimmedUrl))
        {
            errorMessage = "URL cannot be empty.";
            return false;
        }

        if (!UrlValidator.HasTextPlaceholder(trimmedUrl))
        {
            errorMessage = "URL must contain the '{text}' placeholder.";
            return false;
        }

        if (!UrlValidator.IsValidUrl(trimmedUrl))
        {
            errorMessage = "URL must be a valid HTTPS link.";
            return false;
        }

        commandEntries.Add(new CommandEntry
        {
            Name = formattedName,
            HelpMessage = newHelpMessage.Trim(),
            Url = trimmedUrl,
            Enabled = true
        });

        newCommandName = string.Empty;
        newHelpMessage = string.Empty;
        newUrl = string.Empty;
        errorMessage = string.Empty;
        return true;
    }

    private void ResetToDefaults()
    {
        commandEntries.Clear();
        foreach (var (name, cmd) in Configuration.DefaultSearchCommands)
        {
            commandEntries.Add(new CommandEntry
            {
                Name = name,
                HelpMessage = cmd.HelpMessage,
                Url = cmd.Url,
                Enabled = cmd.Enabled
            });
        }
        newCommandName = string.Empty;
        newHelpMessage = string.Empty;
        newUrl = string.Empty;
        errorMessage = string.Empty;
    }

    private bool SaveConfig()
    {
        if (!string.IsNullOrWhiteSpace(newCommandName))
        {
            if (!TryAddNewCommand())
            {
                return false;
            }
        }

        var dict = new Dictionary<string, SearchCommand>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in commandEntries)
        {
            var trimmedName = entry.Name.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                errorMessage = "Command name cannot be empty.";
                return false;
            }

            var formattedName = "/" + trimmedName.TrimStart('/');
            if (formattedName == "/")
            {
                errorMessage = "Command name cannot be empty.";
                return false;
            }

            if (formattedName.Contains(' '))
            {
                errorMessage = $"Command '{formattedName}' cannot contain spaces.";
                return false;
            }

            if (dict.ContainsKey(formattedName))
            {
                errorMessage = $"Duplicate command '{formattedName}' found.";
                return false;
            }

            var trimmedUrl = entry.Url.Trim();
            if (string.IsNullOrWhiteSpace(trimmedUrl))
            {
                errorMessage = $"URL for '{formattedName}' cannot be empty.";
                return false;
            }

            if (!UrlValidator.HasTextPlaceholder(trimmedUrl))
            {
                errorMessage = $"URL for '{formattedName}' must contain the '{{text}}' placeholder.";
                return false;
            }

            if (!UrlValidator.IsValidUrl(trimmedUrl))
            {
                errorMessage = $"URL for '{formattedName}' must be a valid HTTPS link.";
                return false;
            }

            dict[formattedName] = new SearchCommand(trimmedUrl, entry.HelpMessage.Trim(), entry.Enabled);
        }

        configuration.SearchCommands = dict;
        configuration.Save();
        plugin.SearchService.ReloadCommands();
        errorMessage = string.Empty;
        return true;
    }

    private void DrawFooter()
    {
        var style = ImGui.GetStyle();

        using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, ImGui.GetFrameHeight() * 0.5f))
        {
            if (ImGui.Button("Reset to Defaults"))
            {
                ResetToDefaults();
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Restore default search commands (click Save to apply)");
            }

            var closeBtnWidth = ImGui.GetFrameHeight();
            var saveText = "Save";
            var saveBtnWidth = ImGui.CalcTextSize($"{FontAwesomeIcon.Save.ToIconString()}  {saveText}").X + style.FramePadding.X * 2 + 12f;
            var totalRightWidth = closeBtnWidth + style.ItemSpacing.X + saveBtnWidth;

            var availWidth = ImGui.GetContentRegionAvail().X;
            var targetX = ImGui.GetCursorPosX() + availWidth - totalRightWidth;
            if (targetX > ImGui.GetCursorPosX())
            {
                ImGui.SameLine();
                ImGui.SetCursorPosX(targetX);
            }

            if (ImGuiComponents.IconButton("###cancelConfig", FontAwesomeIcon.Times))
            {
                LoadFromConfig();
                newCommandName = string.Empty;
                newHelpMessage = string.Empty;
                newUrl = string.Empty;
                errorMessage = string.Empty;
                IsOpen = false;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Close");
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Save, "Save"))
            {
                if (SaveConfig())
                {
                    IsOpen = false;
                }
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Save changes");
            }
        }
    }
}

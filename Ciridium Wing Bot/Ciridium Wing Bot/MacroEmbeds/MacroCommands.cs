using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.MacroEmbeds
{
    class MacroCommands
    {
        public MacroCommands()
        {
            // macro
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_MACRO, 2, 2), HandleMacroCommand, AccessLevel.Pilot, CMDSUMMARY_MACRO, CMDSYNTAX_MACRO, CMDARGS_MACRO));
            // macro add
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_MACRO_ADD, 4, 1000), HandleMacroAddCommand, AccessLevel.Director, CMDSUMMARY_MACRO_ADD, CMDSYNTAX_MACRO_ADD, CMDARGS_MACRO_ADD));
            // macro list
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_MACRO_LIST), HandleMacroListCommand, AccessLevel.Pilot, CMDSUMMARY_MACRO_LIST, CMDSYNTAX_MACRO_LIST));
            // macro remove
            CommandService.AddCommand(new Command(new CommandKeys(CMDKEYS_MACRO_REMOVE, 3, 3), HandleMacroRemoveCommand, AccessLevel.Director, CMDSUMMARY_MACRO_REMOVE, CMDSYNTAX_MACRO_REMOVE, CMDARGS_MACRO_REMOVE));
        }

        #region /macro

        private const string CMDKEYS_MACRO = "macro";
        private const string CMDSYNTAX_MACRO = "macro <Key>";
        private const string CMDSUMMARY_MACRO = "Retrieves and prints a macro identified by a Key";
        private const string CMDARGS_MACRO = 
            "    <Key>\n" +
            "String key that identifies the macro";

        private async Task HandleMacroCommand(CommandContext context)
        {
            string key = context.Args[1].ToLower();
            if (MacroService.Count > 0)
            {
                MacroEmbed macroEmbed = MacroService.GetMacroEmbed(key);
                if (macroEmbed != null)
                {
                    await context.Channel.SendEmbedAsync(macroEmbed);
                }
                else
                {
                    await context.Channel.SendEmbedAsync(string.Format("Could not find a macro that goes by the key `{0}`. Use `{1}macro list` to get a list of all available macros", key, CommandService.Prefix), true);
                }
            }
            else
            {
                await context.Channel.SendEmbedAsync("No macros stored", true);
            }
        }

        #endregion
        #region /macro add

        private const string CMDKEYS_MACRO_ADD = "macro add";
        private const string CMDSYNTAX_MACRO_ADD = "macro add <Key> {<Title>}, {<Content>}";
        private const string CMDSUMMARY_MACRO_ADD = "Adds a new macro identified by a key, with title and content properties";
        private const string CMDARGS_MACRO_ADD =
            "    <Key>\n" +
            "String key that identifies the macro\n" +
            "    {<Title>}\n" +
            "A title/header printed separately from the content body. May not contain ','\n" +
            "    {<Content>}\n" +
            "The content body. Supports full markdown including named links";

        private async Task HandleMacroAddCommand(CommandContext context)
        {
            string key = context.Args[2].ToLower();
            if (!string.IsNullOrEmpty(key) && !key.ContainsAny(JSONObject.unsafeStringChars.ToArray()) && !key.Equals("add") && !key.Equals("list") && !key.Equals("remove"))
            {
                if (!MacroService.HasMacroEmbed(key))
                {
                    int splitstart = CMDKEYS_MACRO_ADD.Length + context.Args[2].Length + 3;
                    string[] splits = context.Message.Content.Substring(splitstart).Split(',');
                    if (splits.Length > 1)
                    {
                        string header = splits[0];
                        string content = context.Message.Content.Substring(splitstart + splits[0].Length + 1).Trim();
                        MacroEmbed result = new MacroEmbed(key, header, content);

                        string attachedImage = null;
                        IReadOnlyCollection<Attachment> attachments = context.Message.Attachments;
                        if ((attachments != null) && attachments.Count > 0)
                        {
                            foreach (Attachment attachment in attachments)
                            {
                                if (attachment.Url.IsValidImageURL() && string.IsNullOrEmpty(attachedImage))
                                {
                                    attachedImage = attachment.Url;
                                }
                                else
                                {
                                    result.Attachments.Add(attachment.Url);
                                }
                            }
                        }
                        else
                        {
                            Macros.TryGetImageURLFromText(content, out attachedImage);
                        }

                        if (!string.IsNullOrEmpty(attachedImage))
                        {
                            result.AttachedImage = attachedImage;
                        }

                        if (await MacroService.AddMacroEmbed(result))
                        {
                            await context.Channel.SendEmbedAsync(result);
                        }
                        else
                        {
                            await context.Channel.SendEmbedAsync("Saving the macro failed", true);
                        }
                    } 
                    else
                    {
                        await context.Channel.SendEmbedAsync("You need to supply both Title and Content, separated by a comma ','", true);
                    }
                }
                else
                {
                    await context.Channel.SendEmbedAsync(string.Format("A macro for key `{0}` already exists!", key), true);
                }
            }
            else
            {
                await context.Channel.SendEmbedAsync("Key contains unsupported characters or words!", true);
            }
        }

        #endregion
        #region /macro list

        private const string CMDKEYS_MACRO_LIST = "macro list";
        private const string CMDSYNTAX_MACRO_LIST = "macro list";
        private const string CMDSUMMARY_MACRO_LIST = "Lists all available macros";

        private async Task HandleMacroListCommand(CommandContext context)
        {
            if (MacroService.Count > 0)
            {
                List<EmbedField> macros = new List<EmbedField>();
                foreach(MacroEmbed macro in MacroService.Macros)
                {
                    macros.Add(new EmbedField(macro.Key, macro.Header, true));
                }
                await context.Channel.SendSafeEmbedList(MacroService.Count + " stored macros", macros);
            }
            else
            {
                await context.Channel.SendEmbedAsync("No macros stored");
            }
        }

        #endregion
        #region /macro remove

        private const string CMDKEYS_MACRO_REMOVE = "macro remove";
        private const string CMDSYNTAX_MACRO_REMOVE = "macro remove <Key>";
        private const string CMDSUMMARY_MACRO_REMOVE = "Removes a macro identified by the key";
        private const string CMDARGS_MACRO_REMOVE =
            "    <Key>\n" +
            "String key that identifies the macro";

        private async Task HandleMacroRemoveCommand(CommandContext context)
        {
            string key = context.Args[2].ToLower();

            MacroEmbed macro = MacroService.GetMacroEmbed(key);
            if (macro != null)
            {
                await MacroService.RemoveMacroEmbed(macro.Id);
                await context.Channel.SendEmbedAsync(string.Format("Successfully removed macro `{0}`", macro.Header));
            }
            else
            {
                await context.Channel.SendEmbedAsync(string.Format("No macro exists with key `{0}`!", key), true);
            }
        }

        #endregion
    }
}

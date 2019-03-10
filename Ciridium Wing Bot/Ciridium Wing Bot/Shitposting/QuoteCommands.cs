using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.Shitposting
{
    class QuoteCommands
    {
        public QuoteCommands()
        {
            CommandService.AddCommand(new CommandKeys(CMDKEYS_QUOTE), HandleQuoteCommand, AccessLevel.Basic, CMDSUMMARY_QUOTE, CMDSYNTAX_QUOTE, Command.NO_ARGUMENTS);
            CommandService.AddCommand(new CommandKeys(CMDKEYS_QUOTE_ADD, 4, 4), HandleAddQuoteCommand, AccessLevel.Pilot, CMDSUMMARY_QUOTE_ADD, CMDSYNTAX_QUOTE_ADD, CMDARGS_QUOTE_ADD);
        }

        #region quote

        private const string CMDKEYS_QUOTE = "quote";
        private const string CMDSYNTAX_QUOTE = "quote";
        private const string CMDSUMMARY_QUOTE = "Shows a random quote";

        internal async Task HandleQuoteCommand(CommandContext context)
        {
            Quote selected = QuoteService.RandomQuote;
            if (selected != null)
            {
                await context.Channel.SendEmbedAsync(selected.GetEmbed());
            }
            else
            {
                await context.Channel.SendEmbedAsync("I have no quotes stored!", true);
            }
        }

        #endregion
        #region quote add

        private const string CMDKEYS_QUOTE_ADD = "quote add";
        private const string CMDSYNTAX_QUOTE_ADD = "quote add <ChannelId> <MessageId>";
        private const string CMDSUMMARY_QUOTE_ADD = "Adds a message as a new quote";
        private const string CMDARGS_QUOTE_ADD =
            "    <ChannelId>" +
            "Either a uInt64 channel Id, a channel mention or 'this' (for current channel) that marks the channel the original message was sent to" +
            "    <MessageId>" +
            "The message Id of the message you want quoted";

        internal async Task HandleAddQuoteCommand(CommandContext context)
        {
            Quote newQuote = null;
            string message = string.Empty;
            bool error = true;
            ulong? channelId = null;
            if (context.Args[2].Equals("this"))
            {
                channelId = context.Channel.Id;
            }
            else if (context.Message.MentionedChannels.Count > 0)
            {
                channelId = new List<SocketGuildChannel>(context.Message.MentionedChannels)[0].Id;
            }
            else if (ulong.TryParse(context.Args[2], out ulong parsedChannelId))
            {
                channelId = parsedChannelId;
            }

            if (channelId != null)
            {
                SocketTextChannel channel = context.Guild.GetTextChannel((ulong)channelId);
                if (channel != null)
                {
                    if (ulong.TryParse(context.Args[3], out ulong messageId))
                    {
                        IMessage quotedMessage = await channel.GetMessageAsync(messageId);
                        List<IAttachment> attachments = new List<IAttachment>(quotedMessage.Attachments);
                        if (attachments.Count > 0)
                        {
                            newQuote = new Quote(channel.Name, quotedMessage.Content, quotedMessage.Author.Id, quotedMessage.Author.Username, quotedMessage.Timestamp.UtcDateTime, quotedMessage.GetMessageURL(context.Guild.Id), attachments[0].Url);
                        }
                        else
                        {
                            newQuote = new Quote(channel.Name, quotedMessage.Content, quotedMessage.Author.Id, quotedMessage.Author.Username, quotedMessage.Timestamp.UtcDateTime, quotedMessage.GetMessageURL(context.Guild.Id));
                        }
                        await QuoteService.AddQuote(newQuote);
                        error = false;
                    }
                    else
                    {
                        message = "U gonna give me a correct message Id, or I aint lifting a finger!";
                    }
                }
                else
                {
                    message = "Huh, what channel is that??";
                }
            }
            else
            {
                message = "Sorry, but I am too high to see that channel #*!=?!$";
            }
            if (error)
            {
                await context.Channel.SendEmbedAsync(message, error);
            }
            else
            {
                await context.Channel.SendMessageAsync("Stored a new Quote", embed: newQuote.GetEmbed().Build());
            }
        }

        #endregion
            #region quote list

        private const string CMDKEYS_QUOTE_LIST = "quote list";
        private const string CMDSYNTAX_QUOTE_LIST = "quote list";
        private const string CMDSUMMARY_QUOTE_LIST = "";
        private const string CMDARGS_QUOTE_LIST = "";

        internal async Task HandleListQuotesCommand(CommandContext context)
        {

        }

        #endregion
        #region quote delete

        private const string CMDKEYS_QUOTE_DELETE = "";
        private const string CMDSYNTAX_QUOTE_DELETE = "";
        private const string CMDSUMMARY_QUOTE_DELETE = "";
        private const string CMDARGS_QUOTE_DELETE = "";

        internal async Task HandleDeleteQuoteCommand(CommandContext context)
        {

        }

        #endregion
    }
}

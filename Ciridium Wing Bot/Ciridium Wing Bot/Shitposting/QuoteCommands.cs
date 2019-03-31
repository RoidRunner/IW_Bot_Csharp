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
            CommandService.AddCommand(new CommandKeys(CMDKEYS_QUOTE), HandleQuoteCommand, AccessLevel.Basic, CMDSUMMARY_QUOTE, CMDSYNTAX_QUOTE, Command.NO_ARGUMENTS, true);
            CommandService.AddCommand(new CommandKeys(CMDKEYS_QUOTE_ADD, 4, 4), HandleAddQuoteCommand, AccessLevel.Pilot, CMDSUMMARY_QUOTE_ADD, CMDSYNTAX_QUOTE_ADD, CMDARGS_QUOTE_ADD, true);
            CommandService.AddCommand(new CommandKeys(CMDKEYS_QUOTE_DELETE, 3, 3), HandleDeleteQuoteCommand, AccessLevel.Director, CMDSUMMARY_QUOTE_DELETE, CMDSYNTAX_QUOTE_DELETE, CMDARGS_QUOTE_DELETE);
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
                            newQuote = new Quote(channel.Name, quotedMessage.Id, quotedMessage.Content, quotedMessage.Author.Id, quotedMessage.Author.Username, quotedMessage.Timestamp.UtcDateTime, quotedMessage.GetMessageURL(context.Guild.Id), attachments[0].Url);
                        }
                        else
                        {
                            newQuote = new Quote(channel.Name, quotedMessage.Id, quotedMessage.Content, quotedMessage.Author.Id, quotedMessage.Author.Username, quotedMessage.Timestamp.UtcDateTime, quotedMessage.GetMessageURL(context.Guild.Id));
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
                await context.Channel.SendEmbedAsync(newQuote.GetEmbed());
            }
        }

        #endregion
        #region quote delete

        private const string CMDKEYS_QUOTE_DELETE = "quote delete";
        private const string CMDSYNTAX_QUOTE_DELETE = "quote delete <QuoteId>";
        private const string CMDSUMMARY_QUOTE_DELETE = "Removes a Quote from the list of saved quotes";
        private const string CMDARGS_QUOTE_DELETE =
            "    <QuoteId>" +
            "The quote Id of the Quote you want deleted";

        internal async Task HandleDeleteQuoteCommand(CommandContext context)
        {
            string message = string.Empty;
            bool error = true;
            if (int.TryParse(context.Args[2], out int quoteId))
            {
                if (QuoteService.HasQuote(quoteId))
                {
                    await QuoteService.RemoveQuote(quoteId);
                    message = "Successfully yeeted quote #" + quoteId;
                    error = false;
                }
                else
                {
                    message = "Sowwy, but I don't have that quoteId";
                }
            }
            else
            {
                message = "That aint no integer you gave me there to parse!";
            }
            await context.Channel.SendEmbedAsync(message, error);
        }

        #endregion
    }
}

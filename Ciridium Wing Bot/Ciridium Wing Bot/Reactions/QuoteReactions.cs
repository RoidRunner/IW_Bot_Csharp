using Ciridium.Shitposting;
using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium.Reactions
{
    class QuoteReactions
    {
        internal QuoteReactions()
        {
            ReactionService.AddReactionCommand(new ReactionCommand("quote", AccessLevel.Pilot, HandleQuoteReaction, true));
        }

        private async Task HandleQuoteReaction(ReactionContext context)
        {

            Quote newQuote;
            IMessage quotedMessage = context.Message;
            List<IAttachment> attachments = new List<IAttachment>(quotedMessage.Attachments);
            if (attachments.Count > 0)
            {
                newQuote = new Quote(context.Channel.Name, quotedMessage.Id, quotedMessage.Content, quotedMessage.Author.Id, quotedMessage.Author.Username, quotedMessage.Timestamp.UtcDateTime, quotedMessage.GetMessageURL(Var.Guild.Id), attachments[0].Url);
            }
            else
            {
                newQuote = new Quote(context.Channel.Name, quotedMessage.Id, quotedMessage.Content, quotedMessage.Author.Id, quotedMessage.Author.Username, quotedMessage.Timestamp.UtcDateTime, quotedMessage.GetMessageURL(Var.Guild.Id));
            }
            await QuoteService.AddQuote(newQuote);
            await context.Channel.SendEmbedAsync(newQuote.GetEmbed());
        }
    }
}

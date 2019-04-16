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
            ReactionService.AddReactionCommand(new ReactionCommand("quote", AccessLevel.Pilot, HandleQuoteReaction, SpecialChannelType.ShitpostingAllowed));
        }

        private async Task HandleQuoteReaction(ReactionContext context)
        {

            IMessage quotedMessage = context.Message;
            Quote newQuote = Quote.ParseMessageToQuote(quotedMessage);
            if (await QuoteService.AddQuote(newQuote))
            {
                await context.Channel.SendEmbedAsync(newQuote.GetEmbed());
            }
        }
    }
}

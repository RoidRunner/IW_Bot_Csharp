using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    static class Macros
    {
        private const string CODEBLOCKBASESTRING = "``````";
        private const string FATBASESTRING = "****";

        public static string MultiLineCodeBlock(object input)
        {
            return CODEBLOCKBASESTRING.Insert(3, input.ToString());
        }

        public static string Fat(string input)
        {
            return FATBASESTRING.Insert(2, input);
        }

        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, string message, bool error = false)
        {
            EmbedBuilder embed = new EmbedBuilder();
            if (error)
            {
                embed.Color = Var.ERRORCOLOR;
            } else
            {
                embed.Color = Var.BOTCOLOR;
            }
            embed.Description = message;
            return await channel.SendMessageAsync(string.Empty, embed: embed.Build());
        }

        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, string message, string embeddedmessage, bool error = false)
        {
            EmbedBuilder embed = new EmbedBuilder();
            if (error)
            {
                embed.Color = Var.ERRORCOLOR;
            } else
            {
                embed.Color = Var.BOTCOLOR;
            }
            embed.Description = embeddedmessage;
            return await channel.SendMessageAsync(message, embed: embed.Build());
        }

        public async static Task<Discord.Rest.RestUserMessage> SendEmbedAsync(this ISocketMessageChannel channel, EmbedBuilder embed)
        {
            return await channel.SendMessageAsync(string.Empty, embed: embed.Build());
        }
    }
}

using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ciridium
{
    static class Macros
    {
        public static Random Rand = new Random();

        private const string CODEBLOCKBASESTRING = "``````";
        private const string INLINECODEBLOCKBASESTRING = "``";
        private const string FATBASESTRING = "****";

        public static string MultiLineCodeBlock(object input)
        {
            return CODEBLOCKBASESTRING.Insert(3, input.ToString());
        }

        public static string InlineCodeBlock(object input)
        {
            return INLINECODEBLOCKBASESTRING.Insert(1, input.ToString());
        }

        public static string Fat(object input)
        {
            return FATBASESTRING.Insert(2, input.ToString());
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

        public async static Task SendSafeEmbedList(this ISocketMessageChannel channel, string title, List<EmbedField> embeds, string description = null)
        {
            List<EmbedBuilder> embedMessages = new List<EmbedBuilder>();
            EmbedBuilder CurrentBuilder = null;
            for (int i = 0; i < embeds.Count; i++)
            {
                if (i % 25 == 0)
                {
                    CurrentBuilder = new EmbedBuilder();
                    CurrentBuilder.Color = Var.BOTCOLOR;
                    CurrentBuilder.Title = title;
                    if (!string.IsNullOrEmpty(description))
                    {
                        CurrentBuilder.Description = description;
                    }
                    embedMessages.Add(CurrentBuilder);
                }

                EmbedField embed = embeds[i];
                if (CurrentBuilder != null)
                {
                    CurrentBuilder.AddField(embed.Title, embed.Value, embed.InLine);
                }
            }

            foreach (EmbedBuilder embedMessage in embedMessages)
            {
                await channel.SendEmbedAsync(embedMessage);
            }
        }

        public static string GetMessageURL(this IMessage message, ulong guildId)
        {
            return string.Format("https://discordapp.com/channels/{0}/{1}/{2}", guildId, message.Channel.Id, message.Id);
        }

        public static string MaxLength(this string str, int maxLength)
        {
            if (str.Length <= maxLength)
            {
                return str;
            }
            else
            {
                return str.Substring(0, maxLength);
            }
        }
    }

    public struct EmbedField
    {
        public string Title;
        public object Value;
        public bool InLine;

        public EmbedField(string title, object value, bool inLine = false)
        {
            Title = title;
            Value = value;
            InLine = inLine;
        }
    }
}

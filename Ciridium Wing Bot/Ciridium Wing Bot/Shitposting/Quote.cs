using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium.Shitposting
{
    internal class Quote
    {
        internal int Id;
        internal string ChannelName;
        internal ulong MessageId;
        internal string MessageContent;
        internal string ImageURL;
        internal ulong AuthorId;
        internal string AuthorName;
        internal DateTime Timestamp;
        internal string MessageURL;

        public Quote()
        {

        }

        public Quote(string channelName, ulong messageId, string messageContent, ulong authorId, string authorName, DateTime timestamp, string messageLink, string linkedImage = null)
        {
            ChannelName = channelName;
            MessageId = messageId;
            MessageContent = messageContent;
            ImageURL = linkedImage;
            AuthorId = authorId;
            AuthorName = authorName;
            Timestamp = timestamp;
            MessageURL = messageLink;
            SecureNulls();
        }

        private void SecureNulls()
        {
            if (ChannelName == null)
            {
                ChannelName = "Unknown Channel";
            }
            if (MessageContent == null)
            {
                MessageContent = string.Empty;
            }
            if (AuthorName == null)
            {
                AuthorName = "Unkown Author";
            }
        }

        internal EmbedBuilder GetEmbed()
        {
            EmbedBuilder quote = new EmbedBuilder();
            quote.Color = Var.BOTCOLOR;
            quote.Description = MessageContent;
            EmbedAuthorBuilder authorBuilder = new EmbedAuthorBuilder();

            SocketGuild guild = Var.Guild;
            SocketGuildUser author = null;
            if (guild != null)
            {
                author = guild.GetUser(AuthorId);
            }
            if (author != null)
            {
                if (!string.IsNullOrEmpty(author.Nickname))
                {
                    authorBuilder.Name = author.Nickname;
                }
                else
                {
                    authorBuilder.Name = author.Username;
                }
                authorBuilder.IconUrl = author.GetAvatarUrl();
                authorBuilder.Url = MessageURL;
            }
            else
            {
                authorBuilder.Name = AuthorName;
                authorBuilder.IconUrl = "https://cdn.discordapp.com/embed/avatars/0.png";
                authorBuilder.Url = MessageURL;
            }

            quote.Author = authorBuilder;
            if (!string.IsNullOrEmpty(ImageURL))
            {
                quote.ImageUrl = ImageURL;
            }
            EmbedFooterBuilder footer = new EmbedFooterBuilder();

            footer.Text = string.Format("#{0}, QuoteId: {1}", ChannelName, Id);

            quote.Footer = footer;
            quote.Timestamp = new DateTimeOffset(Timestamp, TimeSpan.Zero);

            return quote;
        }

        private const string JSON_ID = "Id";
        private const string JSON_CHANNEL_NAME = "ChannelName";
        private const string JSON_MESSAGE_ID = "MessageId";
        private const string JSON_CONTENT = "Content";
        private const string JSON_IMAGE_URL = "ImageURL";
        private const string JSON_AUTHOR_ID = "AuthorId";
        private const string JSON_AUTHOR_NAME = "AuthorName";
        private const string JSON_TIMESTAMP = "TimeStamp";
        private const string JSON_MESSAGE_URL = "MessageURL";

        internal bool FromJSON(JSONObject json)
        {
            string authorId_str = string.Empty;
            string timestamp_str = string.Empty;
            string messageId_str = string.Empty;
            json.GetField(ref authorId_str, JSON_AUTHOR_ID);
            json.GetField(ref timestamp_str, JSON_TIMESTAMP);
            json.GetField(ref ImageURL, JSON_IMAGE_URL);
            json.GetField(ref messageId_str, JSON_MESSAGE_ID);
            bool success = json.GetField(ref Id, JSON_ID) && json.GetField(ref MessageContent, JSON_CONTENT) && ulong.TryParse(authorId_str, out AuthorId) && json.GetField(ref AuthorName, JSON_AUTHOR_NAME)
                && json.GetField(ref MessageURL, JSON_MESSAGE_URL) && DateTime.TryParse(timestamp_str, Var.Culture, System.Globalization.DateTimeStyles.AssumeUniversal, out Timestamp) && json.GetField(ref ChannelName, JSON_CHANNEL_NAME) && ulong.TryParse(messageId_str, out MessageId);
            if (success)
            {
                Timestamp = DateTime.SpecifyKind(Timestamp, DateTimeKind.Utc);
                MessageContent = JSONObject.ReturnToUnsafeJSONString(MessageContent);
            }
            return success;
        }

        internal JSONObject ToJSON()
        {
            JSONObject result = new JSONObject();
            result.AddField(JSON_ID, Id);
            result.AddField(JSON_MESSAGE_ID, MessageId.ToString());
            result.AddField(JSON_CHANNEL_NAME, ChannelName);
            result.AddField(JSON_CONTENT, JSONObject.GetSafeJSONString(MessageContent));
            result.AddField(JSON_IMAGE_URL, ImageURL);
            result.AddField(JSON_AUTHOR_ID, AuthorId.ToString());
            result.AddField(JSON_AUTHOR_NAME, AuthorName);
            result.AddField(JSON_MESSAGE_URL, MessageURL);
            result.AddField(JSON_TIMESTAMP, Timestamp.ToString("s", Var.Culture));
            return result;
        }

        internal static Quote ParseMessageToQuote(IMessage msg)
        {
            List<IAttachment> attachments = new List<IAttachment>(msg.Attachments);
            if (attachments.Count > 0)
            {
                return new Quote(msg.Channel.Name, msg.Id, msg.Content, msg.Author.Id, msg.Author.Username, msg.Timestamp.UtcDateTime, msg.GetMessageURL(Var.Guild.Id), attachments[0].Url);
            }
            else
            {
                string attachment = null;
                foreach (string word in msg.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    word.Trim();
                    if (word.IsValidImageURL())
                    {
                        attachment = word;
                        break;
                    }
                }
                if (string.IsNullOrEmpty(attachment))
                {
                    return new Quote(msg.Channel.Name, msg.Id, msg.Content, msg.Author.Id, msg.Author.Username, msg.Timestamp.UtcDateTime, msg.GetMessageURL(Var.Guild.Id));
                }
                else
                {
                    return new Quote(msg.Channel.Name, msg.Id, msg.Content, msg.Author.Id, msg.Author.Username, msg.Timestamp.UtcDateTime, msg.GetMessageURL(Var.Guild.Id), attachment);
                }
            }
        }
    }
}

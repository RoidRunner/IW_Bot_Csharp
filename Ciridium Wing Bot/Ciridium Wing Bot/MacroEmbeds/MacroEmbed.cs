using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium.MacroEmbeds
{
    internal class MacroEmbed : PageStorable
    {
        internal string Key;
        internal string Header;
        internal string Content;
        internal string AttachedImage;
        internal List<string> Attachments = new List<string>();

        public MacroEmbed()
        {

        }
        
        internal MacroEmbed(string key, string header, string content, string attachedImage = null, string[] attachments = null)
        {
            Key = key;
            Header = header;
            Content = content;
            AttachedImage = attachedImage;
            if ((attachments != null) && attachments.Length > 0)
            {
                foreach (string attachment in attachments)
                {
                    if (attachment != attachedImage)
                    {
                        Attachments.Add(attachment);
                    }
                }
            }
        }

        private const string JSON_KEY = "Key";
        private const string JSON_CONTENT = "Content";
        private const string JSON_HEADER = "Header";
        private const string JSON_IMAGE = "AttachedImage";
        private const string JSON_ATTACHMENTS = "Attachments";

        internal override bool FromJSON(JSONObject json)
        {
            if (json.GetField(ref Key, JSON_KEY) && json.GetField(ref Content, JSON_CONTENT) && json.GetField(ref Header, JSON_HEADER))
            {
                Content = JSONObject.ReturnToUnsafeJSONString(Content);
                Header = JSONObject.ReturnToUnsafeJSONString(Header);
                
                json.GetField(ref AttachedImage, JSON_IMAGE);
                JSONObject attachments = json[JSON_ATTACHMENTS];
                if ((attachments != null) && attachments.IsArray && attachments.Count > 0)
                {
                    foreach (JSONObject jAttachment in attachments)
                    {
                        if (jAttachment.IsString)
                        {
                            Attachments.Add(jAttachment.str);
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal override JSONObject ToJSON()
        {
            JSONObject json = IdJSON;

            json.AddField(JSON_KEY, Key);
            json.AddField(JSON_CONTENT, JSONObject.GetSafeJSONString(Content));
            json.AddField(JSON_HEADER, Header);

            if (!string.IsNullOrEmpty(AttachedImage))
            {
                json.AddField(JSON_IMAGE, AttachedImage);
            }
            if (Attachments.Count > 0)
            {
                JSONObject attachments = new JSONObject();
                foreach (string attachment in Attachments)
                {
                    attachments.Add(attachment);
                }
            }

            return json;
        }

        internal EmbedBuilder GetEmbed()
        {
            EmbedBuilder result = new EmbedBuilder()
            {
                Title = Header,
                Description = Content,
                ImageUrl = AttachedImage
            };

            if (Attachments.Count > 0)
            {
                StringBuilder attachedString = new StringBuilder();

                foreach (string str in Attachments)
                {
                    attachedString.AppendLine(str);
                }

                result.AddField("Attachments", attachedString.ToString());
            }

            EmbedFooterBuilder footer = new EmbedFooterBuilder()
            {
                Text = "Key: " + Key
            };
            result.Footer = footer;
            return result;
        }
    }
}

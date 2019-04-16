using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium
{
    static class UnicodeEmoteService
    {
        internal static string GetEmote(Emotes emote)
        {
            switch (emote)
            {
                case Emotes.question:
                    return "\u2753";
                case Emotes.checkmark:
                    return "\u2705";
                case Emotes.cross:
                    return "\u274c";
                default:
                    return null;
            }
        }
    }

    public class Emote : IEmote
    {
        public string Name { get; private set; }

        public Emote (string emote)
        {
            Name = emote;
        }

        public Emote (Emotes emote)
        {
            Name = UnicodeEmoteService.GetEmote(emote);
        }
    }

    public enum Emotes
    {
        question,
        checkmark,
        cross
    }
}

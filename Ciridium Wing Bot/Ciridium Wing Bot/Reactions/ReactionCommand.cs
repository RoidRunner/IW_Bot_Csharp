using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium.Reactions
{
    internal struct ReactionContext
    {
        internal IMessage Message;
        internal SocketGuildUser User;
        internal ISocketMessageChannel Channel;
        internal SocketReaction Reaction;

        public ReactionContext(IMessage message, SocketGuildUser user, ISocketMessageChannel channel, SocketReaction reaction)
        {
            Message = message;
            User = user;
            Channel = channel;
            Reaction = reaction;
        }
    }

    internal struct ReactionCommand
    {
        internal string Emote;
        internal AccessLevel RequiredAccess;
        internal HandleReaction HandleReaction;

        public ReactionCommand(string emote, AccessLevel requiredAccess, HandleReaction handleReaction)
        {
            Emote = emote;
            RequiredAccess = requiredAccess;
            HandleReaction = handleReaction;
        }

        internal bool HasPermission(AccessLevel userlevel)
        {
            return userlevel >= RequiredAccess;
        }
    }
}

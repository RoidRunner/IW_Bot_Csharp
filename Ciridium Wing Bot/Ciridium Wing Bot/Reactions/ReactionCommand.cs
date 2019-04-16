using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium.Reactions
{
    internal struct ReactionContext
    {
        internal IMessage Message { get; private set; }
        internal SocketGuildUser User { get; private set; }
        internal ISocketMessageChannel Channel { get; private set; }
        internal SocketReaction Reaction { get; private set; }

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
        internal bool IsShitposting { get; private set; }
        internal bool RequiresMissionChannel { get; private set; }
        internal SpecialChannelType RequiredChannelType { get; private set; }

        public ReactionCommand(string emote, AccessLevel requiredAccess, HandleReaction handleReaction, SpecialChannelType channelType = SpecialChannelType.Normal)
        {
            Emote = emote;
            RequiredAccess = requiredAccess;
            HandleReaction = handleReaction;
            RequiredChannelType = channelType;
            IsShitposting = RequiredChannelType == SpecialChannelType.ShitpostingAllowed;
            RequiresMissionChannel = RequiredChannelType == SpecialChannelType.Mission;
        }

        internal bool HasPermission(AccessLevel userlevel)
        {
            return userlevel >= RequiredAccess;
        }
    }
}

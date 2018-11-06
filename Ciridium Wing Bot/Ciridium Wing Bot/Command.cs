using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    internal struct Command
    {
        public Command(CommandKeys key, AccessLevel accessLevel, HandleCommand handleCommand, string summary, string syntax) : this()
        {
            Key = key;
            AccessLevel = accessLevel;
            HandleCommand = handleCommand;
            Summary = summary;
            Syntax = syntax;
        }

        internal CommandKeys Key { get; private set; }
        internal AccessLevel AccessLevel { get; private set; }
        internal HandleCommand HandleCommand { get; private set; }
        internal string Summary { get; private set; }
        internal string Syntax { get; private set; }
    }

    internal delegate Task HandleCommand(CommandContext context);

    internal struct CommandKeys
    {
        internal string[] Keys { get; private set; }
        internal int FixedArgCnt { get; private set; }
        internal int MinArgCnt { get; private set; }
        internal int MaxArgCnt { get; private set; }

        internal CommandKeys(string key, int minArgCnt, int maxArgCnt)
        {
            Keys = key.Split(' ');
            FixedArgCnt = Keys.Length;
            MinArgCnt = minArgCnt;
            MaxArgCnt = maxArgCnt;
        }

        internal CommandKeys(string key)
        {
            Keys = key.Split(' ');
            FixedArgCnt = Keys.Length;
            MinArgCnt = 0;
            MaxArgCnt = FixedArgCnt;
        }

        internal bool Matches(string[] check)
        {
            int checkCnt = check.Length;
            // Bail out if arg cnt doesn't match
            if (checkCnt > MaxArgCnt || checkCnt < FixedArgCnt)
            {
                return false;
            } else
            {
                bool allKeysMatch = true;
                for (int i = 0; i < Keys.Length; i++)
                {
                    if (!Keys[i].Equals(check[i]))
                    {
                        allKeysMatch = false;
                        break;
                    }
                }
                return allKeysMatch;
            }
        }

        internal bool HasMinArgCnt(int argCnt)
        {
            return argCnt >= MinArgCnt;
        }

        internal string KeyList
        {
            get
            {
                StringBuilder strbuild = new StringBuilder();
                if (Keys.Length > 1)
                {
                    for (int i = 0; i < Keys.Length - 1; i++)
                    {
                        strbuild.Append(Keys[i]);
                        strbuild.Append(" ");
                    }
                }
                strbuild.Append(Keys[Keys.Length - 1]);
                return strbuild.ToString();
            }
        }
    }

    internal class CommandContext : SocketCommandContext
    {
        internal string[] Args { get; private set; }
        internal int ArgCnt { get; private set; }

        internal CommandContext(DiscordSocketClient client, SocketUserMessage msg, string[] args) : base(client, msg)
        {
            Args = args;
            ArgCnt = args.Length;
        }

        internal CommandContext(DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
        { 
            Args = msg.Content.Split(" ");
            Args[0] = Args[0].Substring(1);
            ArgCnt = Args.Length;
        }
    }
}

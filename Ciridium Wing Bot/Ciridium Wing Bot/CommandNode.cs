using System;
using System.Collections.Generic;
using System.Text;

namespace Ciridium
{
    class CommandNode : ICommand, ICommandParent
    {
        public AccessLevel RequiredAccess { get; private set; }
        public string CommandKey { get; private set; }
        public int MinKeys { get; private set; }
        public int MaxKeys { get; private set; }

        public HandleCommand HandleCommand { get; private set; }

        public string Help_Summary { get; private set; }
        public string Help_Detailed { get; private set; }

        public ICommandParent Parent { get; private set; }

        public bool IsRoot { get { return false; } }

        public ICommand AsICommand{ get { return this; } }

        public StringBuilder CommandKeys
        {
            get
            {
                StringBuilder parentKeys = Parent.CommandKeys;
                parentKeys.Append(' ');
                parentKeys.Append(CommandKey);
                return parentKeys;
            }
        }

        private Dictionary<string, ICommand> subCommands;

        public CommandNode(AccessLevel requiredAccess, string commandKey, HandleCommand handleCommand, string help_Syntax, string help_Args, ICommandParent parent)
        {
            RequiredAccess = requiredAccess;
            CommandKey = commandKey;
            HandleCommand = handleCommand;
            Help_Summary = help_Syntax;
            Help_Detailed = help_Args;
            Parent = parent;

            subCommands = new Dictionary<string, ICommand>();
        }

        public void AddSubCommand(ICommand command)
        {
            if (command.RequiredAccess < RequiredAccess)
            {
                throw new FormatException("A sub command cannot have a lower accesslevel as the parent command node");
            }
            //else if ()
            //{

            //}
            else
            {
                subCommands.Add(command.CommandKey, command);
            }
        }

        public GetCommandResult TryGetCommand(CommandContext context, int CMDArgLevel)
        {
            GetCommandResult result = new GetCommandResult(false, null, new List<string>());
            if (context.UserLevel < RequiredAccess)
            {
                result.Messages.Add(string.Format("You do not have the rights to execute this command! `/{0}` requires {1} access, you have {2}", CommandKeys.ToString(), RequiredAccess.ToString(), context.UserLevel.ToString()));
            }
            else if (CMDArgLevel == context.ArgCnt - 1)
            {
                result.Messages.Add(string.Format("You need to supply additional arguments to run a command starting with `/{0}`. Here is the help:", CommandKeys.ToString()));
                result.Messages.Add(Help_Detailed);
            }
            else if (subCommands.TryGetValue(context.Args[CMDArgLevel], out ICommand value))
            {
                result = value.TryGetCommand(context, ++CMDArgLevel);
            }
            else
            {
                result.Messages.Add(string.Format("Could not find a command starting with `/{0}` that matches your input. Here is the help:", CommandKeys.ToString()));
                result.Messages.Add(Help_Detailed);
            }
            return result;
        }
    }
}

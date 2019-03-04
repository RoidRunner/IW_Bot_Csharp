using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Ciridium
{
    interface ICommand
    {
        AccessLevel RequiredAccess { get; }
        string CommandKey { get; }
        int MinKeys { get; }
        int MaxKeys { get; }

        GetCommandResult TryGetCommand(CommandContext context, int CMDArgLevel);
        HandleCommand HandleCommand { get; }

        string Help_Summary { get; }
        string Help_Detailed { get; }

        ICommandParent Parent { get; }
    }

    interface ICommandParent
    {
        bool IsRoot { get; }
        ICommand AsICommand { get; }
        StringBuilder CommandKeys { get; }
    }

    struct GetCommandResult
    {
        public bool Success;
        public ICommand Result;
        public List<string> Messages;

        public GetCommandResult(bool success, ICommand result, List<string> messages = null)
        {
            Success = success;
            Result = result;
            Messages = messages;
        }
    }
}

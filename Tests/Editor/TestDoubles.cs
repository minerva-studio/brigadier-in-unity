using Brigadier.Context;
using System;
using System.Collections.Generic;

namespace Brigadier.Tests
{
    /// <summary>
    /// Stands in for upstream's Mockito <c>Command</c> mock: records every context it is run with.
    /// </summary>
    internal sealed class RecordingCommand<TSource>
    {
        private readonly Func<CommandContext<TSource>, int> _body;

        public RecordingCommand(int result) : this(c => result)
        {
        }

        public RecordingCommand(Func<CommandContext<TSource>, int> body)
        {
            _body = body;
        }

        public List<CommandContext<TSource>> Calls { get; } = new List<CommandContext<TSource>>();

        public int Run(CommandContext<TSource> context)
        {
            Calls.Add(context);
            return _body(context);
        }
    }

    /// <summary>
    /// Stands in for upstream's Mockito <c>ResultConsumer</c> mock: records every completion it is told about.
    /// </summary>
    internal sealed class RecordingConsumer<TSource>
    {
        public readonly struct Call
        {
            public Call(CommandContext<TSource> context, bool success, int result)
            {
                Context = context;
                Success = success;
                Result = result;
            }

            public CommandContext<TSource> Context { get; }

            public bool Success { get; }

            public int Result { get; }
        }

        public List<Call> Calls { get; } = new List<Call>();

        public void OnCommandComplete(CommandContext<TSource> context, bool success, int result)
        {
            Calls.Add(new Call(context, success, result));
        }

        /// <summary>
        /// Counts completions whose context carries exactly <paramref name="source"/>, compared by reference as upstream's <c>contextSourceMatches</c> does.
        /// </summary>
        public int Count(object source, bool success, int result)
        {
            var count = 0;
            foreach (var call in Calls)
            {
                if (ReferenceEquals(call.Context.Source, source) && call.Success == success && call.Result == result)
                {
                    count++;
                }
            }

            return count;
        }
    }
}

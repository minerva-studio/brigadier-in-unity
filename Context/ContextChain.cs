using Brigadier.Exceptions;
using System;
using System.Collections.Generic;

namespace Brigadier.Context
{
    public class ContextChain<TSource>
    {
        // TODO ideally those two would have separate types, but modifiers and executables expect full context
        private readonly List<CommandContext<TSource>> _modifiers;
        private readonly CommandContext<TSource> _executable;

        private ContextChain<TSource> _nextStageCache = null;

        public ContextChain(List<CommandContext<TSource>> modifiers, CommandContext<TSource> executable)
        {
            if (executable.Command == null)
            {
                throw new ArgumentException("Last command in chain must be executable");
            }
            _modifiers = modifiers;
            _executable = executable;
        }

        //PortNote: upstream returns Optional<ContextChain<S>>
        public static bool TryFlatten(CommandContext<TSource> rootContext, out ContextChain<TSource> chain)
        {
            var modifiers = new List<CommandContext<TSource>>();

            var current = rootContext;

            while (true)
            {
                var child = current.Child;
                if (child == null)
                {
                    // Last entry must be executable command
                    if (current.Command == null)
                    {
                        chain = null;
                        return false;
                    }

                    chain = new ContextChain<TSource>(modifiers, current);
                    return true;
                }

                modifiers.Add(current);
                current = child;
            }
        }

        /// <exception cref="CommandSyntaxException" />
        public static IList<TSource> RunModifier(CommandContext<TSource> modifier, TSource source, ResultConsumer<TSource> resultConsumer, bool forkedMode)
        {
            var sourceModifier = modifier.RedirectModifier;

            // Note: source currently in context is irrelevant at this point, since we might have updated it in one of earlier stages
            if (sourceModifier == null)
            {
                // Simple redirect, just propagate source to next node
                return new[] { source };
            }

            var contextToUse = modifier.CopyFor(source);
            try
            {
                return sourceModifier(contextToUse);
            }
            catch (CommandSyntaxException)
            {
                resultConsumer(contextToUse, false, 0);
                if (forkedMode)
                {
                    return Array.Empty<TSource>();
                }
                throw;
            }
        }

        /// <exception cref="CommandSyntaxException" />
        public static int RunExecutable(CommandContext<TSource> executable, TSource source, ResultConsumer<TSource> resultConsumer, bool forkedMode)
        {
            var contextToUse = executable.CopyFor(source);
            try
            {
                var result = executable.Command(contextToUse);
                resultConsumer(contextToUse, true, result);
                return forkedMode ? 1 : result;
            }
            catch (CommandSyntaxException)
            {
                resultConsumer(contextToUse, false, 0);
                if (forkedMode)
                {
                    return 0;
                }
                throw;
            }
        }

        /// <exception cref="CommandSyntaxException" />
        public int ExecuteAll(TSource source, ResultConsumer<TSource> resultConsumer)
        {
            if (_modifiers.Count == 0)
            {
                // Fast path - just a single stage
                return RunExecutable(_executable, source, resultConsumer, false);
            }

            var forkedMode = false;
            var currentSources = new List<TSource> { source };

            foreach (var modifier in _modifiers)
            {
                forkedMode |= modifier.IsForked();

                var nextSources = new List<TSource>();
                foreach (var sourceToRun in currentSources)
                {
                    nextSources.AddRange(RunModifier(modifier, sourceToRun, resultConsumer, forkedMode));
                }
                if (nextSources.Count == 0)
                {
                    return 0;
                }
                currentSources = nextSources;
            }

            var result = 0;
            foreach (var executionSource in currentSources)
            {
                result += RunExecutable(_executable, executionSource, resultConsumer, forkedMode);
            }

            return result;
        }

        //PortNote: a method, as a property would collide with the nested Stage type
        public Stage GetStage()
        {
            return _modifiers.Count == 0 ? Stage.Execute : Stage.Modify;
        }

        public CommandContext<TSource> TopContext
        {
            get
            {
                if (_modifiers.Count == 0)
                {
                    return _executable;
                }
                return _modifiers[0];
            }
        }

        public ContextChain<TSource> NextStage()
        {
            var modifierCount = _modifiers.Count;
            if (modifierCount == 0)
            {
                return null;
            }

            if (_nextStageCache == null)
            {
                _nextStageCache = new ContextChain<TSource>(_modifiers.GetRange(1, modifierCount - 1), _executable);
            }
            return _nextStageCache;
        }

        public enum Stage
        {
            Modify,
            Execute,
        }
    }
}

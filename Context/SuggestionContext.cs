using Brigadier.Tree;

namespace Brigadier.Context
{
    public class SuggestionContext<TSource>
    {
        public readonly CommandContextBuilder<TSource> Context;
        public readonly CommandNode<TSource> Parent;
        public readonly int StartPos;

        public SuggestionContext(CommandContextBuilder<TSource> context, CommandNode<TSource> parent, int startPos)
        {
            Context = context;
            Parent = parent;
            StartPos = startPos;
        }
    }
}

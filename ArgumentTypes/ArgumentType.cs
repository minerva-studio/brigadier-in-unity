using Brigadier.Context;
using Brigadier.Exceptions;
using Brigadier.Suggestion;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Brigadier.ArgumentTypes
{
    public abstract class ArgumentType<T>
    {
        /// <exception cref="CommandSyntaxException"></exception>
        public abstract T Parse(IStringReader reader);

        /// <exception cref="CommandSyntaxException"></exception>
        public virtual T Parse<TSource>(IStringReader reader, TSource source)
        {
            return Parse(reader);
        }

        public virtual Task<Suggestions> ListSuggestions<TSource>(CommandContext<TSource> context, SuggestionsBuilder builder)
        {
            return Suggestions.Empty();
        }

        public virtual IEnumerable<string> Examples => new string[0];
    }
}

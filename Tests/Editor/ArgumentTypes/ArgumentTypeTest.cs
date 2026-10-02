using Brigadier.ArgumentTypes;
using Brigadier.Builder;
using NUnit.Framework;

namespace Brigadier.Tests.ArgumentTypes
{
    /// <summary>
    /// Covers the source-aware <see cref="ArgumentType{T}.Parse{TSource}"/> overload. Upstream ships no test for it.
    /// </summary>
    public class ArgumentTypeTest
    {
        private sealed class SourceCapturingArgumentType : ArgumentType<string>
        {
            public object LastSource { get; private set; }

            public override string Parse(IStringReader reader)
            {
                return reader.ReadUnquotedString();
            }

            public override string Parse<TSource>(IStringReader reader, TSource source)
            {
                LastSource = source;
                return Parse(reader);
            }
        }

        [Test]
        public void TestParseWithSource_defaultsToSourcelessParse()
        {
            var reader = new StringReader("15");

            Assert.That(Arguments.Integer().Parse(reader, new object()), Is.EqualTo(15));
            Assert.That(reader.CanRead(), Is.False);
        }

        [Test]
        public void TestParseWithSource_receivesParseSource()
        {
            var type = new SourceCapturingArgumentType();
            var subject = new CommandDispatcher<object>();
            subject.Register(LiteralArgumentBuilder<object>.LiteralArgument("foo")
                .Then(RequiredArgumentBuilder<object, string>.RequiredArgument("bar", type).Executes(c => 1)));
            var source = new object();

            var parse = subject.Parse("foo value", source);

            Assert.That(parse.Reader.CanRead(), Is.False);
            Assert.That(type.LastSource, Is.SameAs(source));
            Assert.That(Arguments.GetString(parse.Context.Build("foo value"), "bar"), Is.EqualTo("value"));
        }
    }
}

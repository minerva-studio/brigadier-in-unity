using Brigadier.Builder;
using Brigadier.Context;
using NUnit.Framework;

namespace Brigadier.Tests.Context
{
    /// <summary>
    /// Regression for the port's <see cref="CommandContext{TSource}.CopyFor"/>, which compared sources with
    /// <c>Equals</c> where upstream compares references. Not an upstream test.
    /// </summary>
    public class CommandContextCopyForTest
    {
        private sealed class AlwaysEqualSource
        {
            public override bool Equals(object obj)
            {
                return obj is AlwaysEqualSource;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        private static CommandContext<object> BuildContext(CommandDispatcher<object> dispatcher, object source)
        {
            return new CommandContextBuilder<object>(dispatcher, source, dispatcher.Root, 0).Build("");
        }

        [Test]
        public void TestCopyFor_sameSourceReturnsSameContext()
        {
            var source = new AlwaysEqualSource();
            var context = BuildContext(new CommandDispatcher<object>(), source);

            Assert.That(context.CopyFor(source), Is.SameAs(context));
        }

        [Test]
        public void TestCopyFor_equalButDistinctSourceIsPreserved()
        {
            var source = new AlwaysEqualSource();
            var otherSource = new AlwaysEqualSource();
            var context = BuildContext(new CommandDispatcher<object>(), source);

            var copy = context.CopyFor(otherSource);

            Assert.That(copy, Is.Not.SameAs(context));
            Assert.That(copy.Source, Is.SameAs(otherSource));
            Assert.That(context.Source, Is.SameAs(source));
        }

        [Test]
        public void TestCopyFor_nullSourceDoesNotThrow()
        {
            var otherSource = new object();
            var context = BuildContext(new CommandDispatcher<object>(), null);

            Assert.That(context.CopyFor(null), Is.SameAs(context));
            Assert.That(context.CopyFor(otherSource).Source, Is.SameAs(otherSource));
        }

        [Test]
        public void TestExecuteAll_equalButDistinctSourceReachesCommand()
        {
            var command = new RecordingCommand<object>(1);
            var consumer = new RecordingConsumer<object>();
            var dispatcher = new CommandDispatcher<object>();
            dispatcher.Register(LiteralArgumentBuilder<object>.LiteralArgument("foo").Executes(command.Run));
            var compileSource = new AlwaysEqualSource();
            var runtimeSource = new AlwaysEqualSource();

            var topContext = dispatcher.Parse("foo", compileSource).Context.Build("foo");
            Assert.That(ContextChain<object>.TryFlatten(topContext, out var chain), Is.True);
            chain.ExecuteAll(runtimeSource, consumer.OnCommandComplete);

            Assert.That(command.Calls.Count, Is.EqualTo(1));
            Assert.That(command.Calls[0].Source, Is.SameAs(runtimeSource));
            Assert.That(consumer.Count(runtimeSource, true, 1), Is.EqualTo(1));
        }
    }
}

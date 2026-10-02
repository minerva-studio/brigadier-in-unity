using Brigadier.Builder;
using Brigadier.Context;
using NUnit.Framework;

namespace Brigadier.Tests.Context
{
    /// <summary>
    /// Port of upstream <c>com.mojang.brigadier.context.ContextChainTest</c>.
    /// </summary>
    public class ContextChainTest
    {
        private static LiteralArgumentBuilder<object> Literal(string name)
        {
            return LiteralArgumentBuilder<object>.LiteralArgument(name);
        }

        private static ContextChain<object> Flatten(CommandContext<object> topContext)
        {
            Assert.That(ContextChain<object>.TryFlatten(topContext, out var chain), Is.True);
            return chain;
        }

        [Test]
        public void TestExecuteAllForSingleCommand()
        {
            var consumer = new RecordingConsumer<object>();
            var command = new RecordingCommand<object>(4);

            var dispatcher = new CommandDispatcher<object>();
            dispatcher.Register(Literal("foo").Executes(command.Run));
            object source = "compile_source";

            var result = dispatcher.Parse("foo", source);
            var topContext = result.Context.Build("foo");
            var chain = Flatten(topContext);

            object runtimeSource = "runtime_source";
            Assert.That(chain.ExecuteAll(runtimeSource, consumer.OnCommandComplete), Is.EqualTo(4));

            Assert.That(command.Calls.Count, Is.EqualTo(1));
            Assert.That(command.Calls[0].Source, Is.SameAs(runtimeSource));

            Assert.That(consumer.Count(runtimeSource, true, 4), Is.EqualTo(1));
            Assert.That(consumer.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestExecuteAllForRedirectedCommand()
        {
            var consumer = new RecordingConsumer<object>();
            var command = new RecordingCommand<object>(4);

            object redirectedSource = "redirected_source";

            var dispatcher = new CommandDispatcher<object>();
            dispatcher.Register(Literal("foo").Executes(command.Run));
            dispatcher.Register(Literal("bar").Redirect(dispatcher.Root, context => redirectedSource));
            object source = "compile_source";

            var result = dispatcher.Parse("bar foo", source);
            var topContext = result.Context.Build("bar foo");
            var chain = Flatten(topContext);

            object runtimeSource = "runtime_source";
            Assert.That(chain.ExecuteAll(runtimeSource, consumer.OnCommandComplete), Is.EqualTo(4));

            Assert.That(command.Calls.Count, Is.EqualTo(1));
            Assert.That(command.Calls[0].Source, Is.SameAs(redirectedSource));

            Assert.That(consumer.Count(redirectedSource, true, 4), Is.EqualTo(1));
            Assert.That(consumer.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestSingleStageExecution()
        {
            var dispatcher = new CommandDispatcher<object>();
            dispatcher.Register(Literal("foo").Executes(context => 1));
            var source = new object();

            var result = dispatcher.Parse("foo", source);
            var topContext = result.Context.Build("foo");
            var chain = Flatten(topContext);

            Assert.That(chain.GetStage(), Is.EqualTo(ContextChain<object>.Stage.Execute));
            Assert.That(chain.TopContext, Is.SameAs(topContext));
            Assert.That(chain.NextStage(), Is.Null);
        }

        [Test]
        public void TestMultiStageExecution()
        {
            var dispatcher = new CommandDispatcher<object>();
            dispatcher.Register(Literal("foo").Executes(context => 1));
            dispatcher.Register(Literal("bar").Redirect(dispatcher.Root));
            var source = new object();

            var result = dispatcher.Parse("bar bar foo", source);
            var topContext = result.Context.Build("bar bar foo");
            var stage0 = Flatten(topContext);

            Assert.That(stage0.GetStage(), Is.EqualTo(ContextChain<object>.Stage.Modify));
            Assert.That(stage0.TopContext, Is.SameAs(topContext));

            var stage1 = stage0.NextStage();
            Assert.That(stage1, Is.Not.Null);
            Assert.That(stage1.GetStage(), Is.EqualTo(ContextChain<object>.Stage.Modify));
            Assert.That(stage1.TopContext, Is.SameAs(topContext.Child));

            var stage2 = stage1.NextStage();
            Assert.That(stage2, Is.Not.Null);
            Assert.That(stage2.GetStage(), Is.EqualTo(ContextChain<object>.Stage.Execute));
            Assert.That(stage2.TopContext, Is.SameAs(topContext.Child.Child));

            Assert.That(stage2.NextStage(), Is.Null);
        }

        [Test]
        public void TestMissingExecute()
        {
            var dispatcher = new CommandDispatcher<object>();
            dispatcher.Register(Literal("foo").Executes(context => 1));
            dispatcher.Register(Literal("bar").Redirect(dispatcher.Root));

            var source = new object();
            var result = dispatcher.Parse("bar bar", source);
            var topContext = result.Context.Build("bar bar");
            Assert.That(ContextChain<object>.TryFlatten(topContext, out var chain), Is.False);
            Assert.That(chain, Is.Null);
        }
    }
}

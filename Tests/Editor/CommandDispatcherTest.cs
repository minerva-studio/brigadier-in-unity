using Brigadier.ArgumentTypes;
using Brigadier.Builder;
using Brigadier.Context;
using Brigadier.Exceptions;
using NUnit.Framework;
using System.Collections.Generic;

namespace Brigadier.Tests
{
    /// <summary>
    /// Port of upstream <c>com.mojang.brigadier.CommandDispatcherTest</c>.
    /// </summary>
    public class CommandDispatcherTest
    {
        private CommandDispatcher<object> _subject;
        private RecordingCommand<object> _command;
        private object _source;

        [SetUp]
        public void SetUp()
        {
            _subject = new CommandDispatcher<object>();
            _command = new RecordingCommand<object>(42);
            _source = new object();
        }

        private static LiteralArgumentBuilder<object> Literal(string name)
        {
            return LiteralArgumentBuilder<object>.LiteralArgument(name);
        }

        private static RequiredArgumentBuilder<object, T> Argument<T>(string name, ArgumentType<T> type)
        {
            return RequiredArgumentBuilder<object, T>.RequiredArgument(name, type);
        }

        private static StringReader InputWithOffset(string input, int offset)
        {
            var result = new StringReader(input);
            result.Cursor = offset;
            return result;
        }

        [Test]
        public void TestCreateAndExecuteCommand()
        {
            _subject.Register(Literal("foo").Executes(_command.Run));

            Assert.That(_subject.Execute("foo", _source), Is.EqualTo(42));
            Assert.That(_command.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestCreateAndExecuteOffsetCommand()
        {
            _subject.Register(Literal("foo").Executes(_command.Run));

            Assert.That(_subject.Execute(InputWithOffset("/foo", 1), _source), Is.EqualTo(42));
            Assert.That(_command.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestCreateAndMergeCommands()
        {
            _subject.Register(Literal("base").Then(Literal("foo").Executes(_command.Run)));
            _subject.Register(Literal("base").Then(Literal("bar").Executes(_command.Run)));

            Assert.That(_subject.Execute("base foo", _source), Is.EqualTo(42));
            Assert.That(_subject.Execute("base bar", _source), Is.EqualTo(42));
            Assert.That(_command.Calls.Count, Is.EqualTo(2));
        }

        [Test]
        public void TestExecuteUnknownCommand()
        {
            _subject.Register(Literal("bar"));
            _subject.Register(Literal("baz"));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownCommand()));
            Assert.That(ex.Cursor, Is.EqualTo(0));
        }

        [Test]
        public void TestExecuteImpermissibleCommand()
        {
            _subject.Register(Literal("foo").Requires(s => false));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownCommand()));
            Assert.That(ex.Cursor, Is.EqualTo(0));
        }

        [Test]
        public void TestExecuteEmptyCommand()
        {
            _subject.Register(Literal(""));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownCommand()));
            Assert.That(ex.Cursor, Is.EqualTo(0));
        }

        [Test]
        public void TestExecuteUnknownSubcommand()
        {
            _subject.Register(Literal("foo").Executes(_command.Run));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo bar", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownArgument()));
            Assert.That(ex.Cursor, Is.EqualTo(4));
        }

        [Test]
        public void TestExecuteIncorrectLiteral()
        {
            _subject.Register(Literal("foo").Executes(_command.Run).Then(Literal("bar")));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo baz", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownArgument()));
            Assert.That(ex.Cursor, Is.EqualTo(4));
        }

        [Test]
        public void TestExecuteAmbiguousIncorrectArgument()
        {
            _subject.Register(
                Literal("foo").Executes(_command.Run)
                    .Then(Literal("bar"))
                    .Then(Literal("baz"))
            );

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo unknown", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownArgument()));
            Assert.That(ex.Cursor, Is.EqualTo(4));
        }

        [Test]
        public void TestExecuteSubcommand()
        {
            var subCommand = new RecordingCommand<object>(100);

            _subject.Register(Literal("foo").Then(
                Literal("a")
            ).Then(
                Literal("=").Executes(subCommand.Run)
            ).Then(
                Literal("c")
            ).Executes(_command.Run));

            Assert.That(_subject.Execute("foo =", _source), Is.EqualTo(100));
            Assert.That(subCommand.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestParseIncompleteLiteral()
        {
            _subject.Register(Literal("foo").Then(Literal("bar").Executes(_command.Run)));

            var parse = _subject.Parse("foo ", _source);
            Assert.That(parse.Reader.Remaining, Is.EqualTo(" "));
            Assert.That(parse.Context.Nodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestParseIncompleteArgument()
        {
            _subject.Register(Literal("foo").Then(Argument("bar", Arguments.Integer()).Executes(_command.Run)));

            var parse = _subject.Parse("foo ", _source);
            Assert.That(parse.Reader.Remaining, Is.EqualTo(" "));
            Assert.That(parse.Context.Nodes.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestExecuteAmbiguiousParentSubcommand()
        {
            var subCommand = new RecordingCommand<object>(100);

            _subject.Register(
                Literal("test")
                    .Then(
                        Argument("incorrect", Arguments.Integer())
                            .Executes(_command.Run)
                    )
                    .Then(
                        Argument("right", Arguments.Integer())
                            .Then(
                                Argument("sub", Arguments.Integer())
                                    .Executes(subCommand.Run)
                            )
                    )
            );

            Assert.That(_subject.Execute("test 1 2", _source), Is.EqualTo(100));
            Assert.That(subCommand.Calls.Count, Is.EqualTo(1));
            Assert.That(_command.Calls, Is.Empty);
        }

        [Test]
        public void TestExecuteAmbiguiousParentSubcommandViaRedirect()
        {
            var subCommand = new RecordingCommand<object>(100);

            var real = _subject.Register(
                Literal("test")
                    .Then(
                        Argument("incorrect", Arguments.Integer())
                            .Executes(_command.Run)
                    )
                    .Then(
                        Argument("right", Arguments.Integer())
                            .Then(
                                Argument("sub", Arguments.Integer())
                                    .Executes(subCommand.Run)
                            )
                    )
            );

            _subject.Register(Literal("redirect").Redirect(real));

            Assert.That(_subject.Execute("redirect 1 2", _source), Is.EqualTo(100));
            Assert.That(subCommand.Calls.Count, Is.EqualTo(1));
            Assert.That(_command.Calls, Is.Empty);
        }

        [Test]
        public void TestExecuteRedirectedMultipleTimes()
        {
            var concreteNode = _subject.Register(Literal("actual").Executes(_command.Run));
            var redirectNode = _subject.Register(Literal("redirected").Redirect(_subject.Root));

            const string input = "redirected redirected actual";

            var parse = _subject.Parse(input, _source);
            Assert.That(parse.Context.Range.Get(input), Is.EqualTo("redirected"));
            Assert.That(parse.Context.Nodes.Count, Is.EqualTo(1));
            Assert.That(parse.Context.RootNode, Is.SameAs(_subject.Root));
            Assert.That(parse.Context.Nodes[0].Range, Is.EqualTo(parse.Context.Range));
            Assert.That(parse.Context.Nodes[0].Node, Is.SameAs(redirectNode));

            var child1 = parse.Context.Child;
            Assert.That(child1, Is.Not.Null);
            Assert.That(child1.Range.Get(input), Is.EqualTo("redirected"));
            Assert.That(child1.Nodes.Count, Is.EqualTo(1));
            Assert.That(child1.RootNode, Is.SameAs(_subject.Root));
            Assert.That(child1.Nodes[0].Range, Is.EqualTo(child1.Range));
            Assert.That(child1.Nodes[0].Node, Is.SameAs(redirectNode));

            var child2 = child1.Child;
            Assert.That(child2, Is.Not.Null);
            Assert.That(child2.Range.Get(input), Is.EqualTo("actual"));
            Assert.That(child2.Nodes.Count, Is.EqualTo(1));
            Assert.That(child2.RootNode, Is.SameAs(_subject.Root));
            Assert.That(child2.Nodes[0].Range, Is.EqualTo(child2.Range));
            Assert.That(child2.Nodes[0].Node, Is.SameAs(concreteNode));

            Assert.That(_subject.Execute(parse), Is.EqualTo(42));
            Assert.That(_command.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestExecuteRedirected()
        {
            var source1 = new object();
            var source2 = new object();
            var modifierSources = new List<object>();
            RedirectModifier<object> modifier = context =>
            {
                modifierSources.Add(context.Source);
                return new[] { source1, source2 };
            };

            var concreteNode = _subject.Register(Literal("actual").Executes(_command.Run));
            var redirectNode = _subject.Register(Literal("redirected").Fork(_subject.Root, modifier));

            const string input = "redirected actual";
            var parse = _subject.Parse(input, _source);
            Assert.That(parse.Context.Range.Get(input), Is.EqualTo("redirected"));
            Assert.That(parse.Context.Nodes.Count, Is.EqualTo(1));
            Assert.That(parse.Context.RootNode, Is.EqualTo(_subject.Root));
            Assert.That(parse.Context.Nodes[0].Range, Is.EqualTo(parse.Context.Range));
            Assert.That(parse.Context.Nodes[0].Node, Is.SameAs(redirectNode));
            Assert.That(parse.Context.Source, Is.SameAs(_source));

            var parent = parse.Context.Child;
            Assert.That(parent, Is.Not.Null);
            Assert.That(parent.Range.Get(input), Is.EqualTo("actual"));
            Assert.That(parent.Nodes.Count, Is.EqualTo(1));
            Assert.That(parse.Context.RootNode, Is.EqualTo(_subject.Root));
            Assert.That(parent.Nodes[0].Range, Is.EqualTo(parent.Range));
            Assert.That(parent.Nodes[0].Node, Is.SameAs(concreteNode));
            Assert.That(parent.Source, Is.SameAs(_source));

            Assert.That(_subject.Execute(parse), Is.EqualTo(2));
            Assert.That(modifierSources, Is.EqualTo(new[] { _source }));
            Assert.That(_command.Calls.Count, Is.EqualTo(2));
            Assert.That(_command.Calls[0].Source, Is.SameAs(source1));
            Assert.That(_command.Calls[1].Source, Is.SameAs(source2));
        }

        [Test]
        public void TestExecuteOrphanedSubcommand()
        {
            _subject.Register(Literal("foo").Then(
                Argument("bar", Arguments.Integer())
            ).Executes(_command.Run));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo 5", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownCommand()));
            Assert.That(ex.Cursor, Is.EqualTo(5));
        }

        [Test]
        public void TestExecute_invalidOther()
        {
            var wrongCommand = new RecordingCommand<object>(0);
            _subject.Register(Literal("w").Executes(wrongCommand.Run));
            _subject.Register(Literal("world").Executes(_command.Run));

            Assert.That(_subject.Execute("world", _source), Is.EqualTo(42));
            Assert.That(wrongCommand.Calls, Is.Empty);
            Assert.That(_command.Calls.Count, Is.EqualTo(1));
        }

        [Test]
        public void Parse_noSpaceSeparator()
        {
            _subject.Register(Literal("foo").Then(Argument("bar", Arguments.Integer()).Executes(_command.Run)));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo$", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.DispatcherUnknownCommand()));
            Assert.That(ex.Cursor, Is.EqualTo(0));
        }

        [Test]
        public void TestExecuteInvalidSubcommand()
        {
            _subject.Register(Literal("foo").Then(
                Argument("bar", Arguments.Integer())
            ).Executes(_command.Run));

            var ex = Assert.Throws<CommandSyntaxException>(() => _subject.Execute("foo bar", _source));
            Assert.That(ex.Type, Is.SameAs(CommandSyntaxException.BuiltInExceptions.ReaderExpectedInt()));
            Assert.That(ex.Cursor, Is.EqualTo(4));
        }

        [Test]
        public void TestGetPath()
        {
            var bar = Literal("bar").Build();
            _subject.Register(Literal("foo").Then(bar));

            Assert.That(_subject.GetPath(bar), Is.EqualTo(new[] { "foo", "bar" }));
        }

        [Test]
        public void TestFindNodeExists()
        {
            var bar = Literal("bar").Build();
            _subject.Register(Literal("foo").Then(bar));

            Assert.That(_subject.FindNode(new[] { "foo", "bar" }), Is.SameAs(bar));
        }

        [Test]
        public void TestFindNodeDoesntExist()
        {
            Assert.That(_subject.FindNode(new[] { "foo", "bar" }), Is.Null);
        }
    }
}

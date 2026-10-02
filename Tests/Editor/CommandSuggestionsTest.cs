using Brigadier.ArgumentTypes;
using Brigadier.Builder;
using Brigadier.Context;
using Brigadier.Suggestion;
using NUnit.Framework;
using System.Collections.Generic;

namespace Brigadier.Tests
{
    /// <summary>
    /// Port of upstream <c>com.mojang.brigadier.CommandSuggestionsTest</c>.
    /// </summary>
    public class CommandSuggestionsTest
    {
        private CommandDispatcher<object> _subject;
        private object _source;

        [SetUp]
        public void SetUp()
        {
            _subject = new CommandDispatcher<object>();
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

        private static List<Suggestion.Suggestion> Expected(StringRange range, params string[] suggestions)
        {
            var expected = new List<Suggestion.Suggestion>();
            foreach (var suggestion in suggestions)
            {
                expected.Add(new Suggestion.Suggestion(range, suggestion));
            }

            return expected;
        }

        private void TestSuggestions(string contents, int cursor, StringRange range, params string[] suggestions)
        {
            var result = _subject.GetCompletionSuggestions(_subject.Parse(contents, _source), cursor).Result;
            Assert.That(result.Range, Is.EqualTo(range));
            Assert.That(result.List, Is.EqualTo(Expected(range, suggestions)));
        }

        private static StringReader InputWithOffset(string input, int offset)
        {
            var result = new StringReader(input);
            result.Cursor = offset;
            return result;
        }

        [Test]
        public void GetCompletionSuggestions_rootCommands()
        {
            _subject.Register(Literal("foo"));
            _subject.Register(Literal("bar"));
            _subject.Register(Literal("baz"));

            var result = _subject.GetCompletionSuggestions(_subject.Parse("", _source)).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.At(0)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.At(0), "bar", "baz", "foo")));
        }

        [Test]
        public void GetCompletionSuggestions_rootCommands_withInputOffset()
        {
            _subject.Register(Literal("foo"));
            _subject.Register(Literal("bar"));
            _subject.Register(Literal("baz"));

            var result = _subject.GetCompletionSuggestions(_subject.Parse(InputWithOffset("OOO", 3), _source)).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.At(3)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.At(3), "bar", "baz", "foo")));
        }

        [Test]
        public void GetCompletionSuggestions_rootCommands_partial()
        {
            _subject.Register(Literal("foo"));
            _subject.Register(Literal("bar"));
            _subject.Register(Literal("baz"));

            var result = _subject.GetCompletionSuggestions(_subject.Parse("b", _source)).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.Between(0, 1)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.Between(0, 1), "bar", "baz")));
        }

        [Test]
        public void GetCompletionSuggestions_rootCommands_partial_withInputOffset()
        {
            _subject.Register(Literal("foo"));
            _subject.Register(Literal("bar"));
            _subject.Register(Literal("baz"));

            var result = _subject.GetCompletionSuggestions(_subject.Parse(InputWithOffset("Zb", 1), _source)).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.Between(1, 2)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.Between(1, 2), "bar", "baz")));
        }

        [Test]
        public void GetCompletionSuggestions_subCommands()
        {
            _subject.Register(
                Literal("parent")
                    .Then(Literal("foo"))
                    .Then(Literal("bar"))
                    .Then(Literal("baz"))
            );

            var result = _subject.GetCompletionSuggestions(_subject.Parse("parent ", _source)).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.At(7)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.At(7), "bar", "baz", "foo")));
        }

        [Test]
        public void GetCompletionSuggestions_movingCursor_subCommands()
        {
            _subject.Register(
                Literal("parent_one")
                    .Then(Literal("faz"))
                    .Then(Literal("fbz"))
                    .Then(Literal("gaz"))
            );

            _subject.Register(
                Literal("parent_two")
            );

            TestSuggestions("parent_one faz ", 0, StringRange.At(0), "parent_one", "parent_two");
            TestSuggestions("parent_one faz ", 1, StringRange.Between(0, 1), "parent_one", "parent_two");
            TestSuggestions("parent_one faz ", 7, StringRange.Between(0, 7), "parent_one", "parent_two");
            TestSuggestions("parent_one faz ", 8, StringRange.Between(0, 8), "parent_one");
            TestSuggestions("parent_one faz ", 10, StringRange.At(0));
            TestSuggestions("parent_one faz ", 11, StringRange.At(11), "faz", "fbz", "gaz");
            TestSuggestions("parent_one faz ", 12, StringRange.Between(11, 12), "faz", "fbz");
            TestSuggestions("parent_one faz ", 13, StringRange.Between(11, 13), "faz");
            TestSuggestions("parent_one faz ", 14, StringRange.At(0));
            TestSuggestions("parent_one faz ", 15, StringRange.At(0));
        }

        [Test]
        public void GetCompletionSuggestions_subCommands_partial()
        {
            _subject.Register(
                Literal("parent")
                    .Then(Literal("foo"))
                    .Then(Literal("bar"))
                    .Then(Literal("baz"))
            );

            var parse = _subject.Parse("parent b", _source);
            var result = _subject.GetCompletionSuggestions(parse).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.Between(7, 8)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.Between(7, 8), "bar", "baz")));
        }

        [Test]
        public void GetCompletionSuggestions_subCommands_partial_withInputOffset()
        {
            _subject.Register(
                Literal("parent")
                    .Then(Literal("foo"))
                    .Then(Literal("bar"))
                    .Then(Literal("baz"))
            );

            var parse = _subject.Parse(InputWithOffset("junk parent b", 5), _source);
            var result = _subject.GetCompletionSuggestions(parse).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.Between(12, 13)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.Between(12, 13), "bar", "baz")));
        }

        [Test]
        public void GetCompletionSuggestions_redirect()
        {
            var actual = _subject.Register(Literal("actual").Then(Literal("sub")));
            _subject.Register(Literal("redirect").Redirect(actual));

            var parse = _subject.Parse("redirect ", _source);
            var result = _subject.GetCompletionSuggestions(parse).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.At(9)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.At(9), "sub")));
        }

        [Test]
        public void GetCompletionSuggestions_redirectPartial()
        {
            var actual = _subject.Register(Literal("actual").Then(Literal("sub")));
            _subject.Register(Literal("redirect").Redirect(actual));

            var parse = _subject.Parse("redirect s", _source);
            var result = _subject.GetCompletionSuggestions(parse).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.Between(9, 10)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.Between(9, 10), "sub")));
        }

        [Test]
        public void GetCompletionSuggestions_movingCursor_redirect()
        {
            var actualOne = _subject.Register(Literal("actual_one")
                .Then(Literal("faz"))
                .Then(Literal("fbz"))
                .Then(Literal("gaz"))
            );

            _subject.Register(Literal("actual_two"));

            _subject.Register(Literal("redirect_one").Redirect(actualOne));
            _subject.Register(Literal("redirect_two").Redirect(actualOne));

            TestSuggestions("redirect_one faz ", 0, StringRange.At(0), "actual_one", "actual_two", "redirect_one", "redirect_two");
            TestSuggestions("redirect_one faz ", 9, StringRange.Between(0, 9), "redirect_one", "redirect_two");
            TestSuggestions("redirect_one faz ", 10, StringRange.Between(0, 10), "redirect_one");
            TestSuggestions("redirect_one faz ", 12, StringRange.At(0));
            TestSuggestions("redirect_one faz ", 13, StringRange.At(13), "faz", "fbz", "gaz");
            TestSuggestions("redirect_one faz ", 14, StringRange.Between(13, 14), "faz", "fbz");
            TestSuggestions("redirect_one faz ", 15, StringRange.Between(13, 15), "faz");
            TestSuggestions("redirect_one faz ", 16, StringRange.At(0));
            TestSuggestions("redirect_one faz ", 17, StringRange.At(0));
        }

        [Test]
        public void GetCompletionSuggestions_redirectPartial_withInputOffset()
        {
            var actual = _subject.Register(Literal("actual").Then(Literal("sub")));
            _subject.Register(Literal("redirect").Redirect(actual));

            var parse = _subject.Parse(InputWithOffset("/redirect s", 1), _source);
            var result = _subject.GetCompletionSuggestions(parse).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.Between(10, 11)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.Between(10, 11), "sub")));
        }

        [Test]
        public void GetCompletionSuggestions_redirect_lots()
        {
            var loop = _subject.Register(Literal("redirect"));
            _subject.Register(
                Literal("redirect")
                    .Then(
                        Literal("loop")
                            .Then(
                                Argument("loop", Arguments.Integer())
                                    .Redirect(loop)
                            )
                    )
            );

            var result = _subject.GetCompletionSuggestions(_subject.Parse("redirect loop 1 loop 02 loop 003 ", _source)).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.At(33)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.At(33), "loop")));
        }

        [Test]
        public void GetCompletionSuggestions_redirect_contextualArgument()
        {
            var actual = _subject.Register(
                Literal("actual")
                    .Then(Argument("arg_one", Arguments.Word())
                        .Then(Argument("arg_two", Arguments.Word())
                            .Suggests((context, builder) =>
                            {
                                var argOne = Arguments.GetString(context, "arg_one");
                                builder.Suggest("contextual_" + argOne);
                                return builder.BuildFuture();
                            })
                        )
                    )
            );
            _subject.Register(Literal("redirect").Redirect(actual));

            var result = _subject.GetCompletionSuggestions(_subject.Parse("redirect first ", _source)).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.At(15)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.At(15), "contextual_first")));
        }

        [Test]
        public void GetCompletionSuggestions_execute_simulation()
        {
            var execute = _subject.Register(Literal("execute"));
            _subject.Register(
                Literal("execute")
                    .Then(
                        Literal("as")
                            .Then(
                                Argument("name", Arguments.Word())
                                    .Redirect(execute)
                            )
                    )
                    .Then(
                        Literal("store")
                            .Then(
                                Argument("name", Arguments.Word())
                                    .Redirect(execute)
                            )
                    )
                    .Then(
                        Literal("run")
                            .Executes(c => 0)
                    )
            );

            var parse = _subject.Parse("execute as Dinnerbone as", _source);
            var result = _subject.GetCompletionSuggestions(parse).Result;

            Assert.That(result.IsEmpty(), Is.True);
        }

        [Test]
        public void GetCompletionSuggestions_execute_simulation_partial()
        {
            var execute = _subject.Register(Literal("execute"));
            _subject.Register(
                Literal("execute")
                    .Then(
                        Literal("as")
                            .Then(Literal("bar").Redirect(execute))
                            .Then(Literal("baz").Redirect(execute))
                    )
                    .Then(
                        Literal("store")
                            .Then(
                                Argument("name", Arguments.Word())
                                    .Redirect(execute)
                            )
                    )
                    .Then(
                        Literal("run")
                            .Executes(c => 0)
                    )
            );

            var parse = _subject.Parse("execute as bar as ", _source);
            var result = _subject.GetCompletionSuggestions(parse).Result;

            Assert.That(result.Range, Is.EqualTo(StringRange.At(18)));
            Assert.That(result.List, Is.EqualTo(Expected(StringRange.At(18), "bar", "baz")));
        }
    }
}

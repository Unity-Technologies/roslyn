﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.AutomaticCompletion;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AutomaticCompletion
{
    public class AutomaticBracketCompletionTests : AbstractAutomaticBraceCompletionTests
    {
        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Creation()
        {
            using var session = CreateSession("$$");
            Assert.NotNull(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Attribute_TopLevel()
        {
            using var session = CreateSession("$$");
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Attribute_TopLevel2()
        {
            using var session = CreateSession("using System;$$");
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_String()
        {
            var code = @"class C
{
    string s = ""$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_String2()
        {
            var code = @"class C
{
    string s = @""
$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment()
        {
            var code = @"class C
{
    //$$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment2()
        {
            var code = @"class C
{
    /* $$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment3()
        {
            var code = @"class C
{
    /// $$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void InvalidLocation_Comment4()
        {
            var code = @"class C
{
    /** $$
}";
            using var session = CreateSession(code);
            Assert.Null(session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void MultiLine_Comment()
        {
            var code = @"class C
{
    void Method()
    {
        /* */$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void MultiLine_DocComment()
        {
            var code = @"class C
{
    void Method()
    {
        /** */$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String1()
        {
            var code = @"class C
{
    void Method()
    {
        var s = """"$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void String2()
        {
            var code = @"class C
{
    void Method()
    {
        var s = @""""$$
    }
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Attribute_OpenBracket()
        {
            var code = @"$$
class C { }";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Attribute_OpenBracket_Delete()
        {
            var code = @"$$
class C { }";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckBackspace(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Attribute_OpenBracket_Tab()
        {
            var code = @"$$
class C { }";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckTab(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Attribute_OpenBracket_CloseBracket()
        {
            var code = @"$$
class C { }";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
            CheckOverType(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Array_Multiple_Invalid()
        {
            var code = @"class C 
{
    int [$$]
}";

            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.AutomaticCompletion)]
        public void Array_Nested()
        {
            var code = @"class C
{
    int [] i = new int [arr$$]
}";
            using var session = CreateSession(code);
            Assert.NotNull(session);

            CheckStart(session.Session);
        }

        internal static Holder CreateSession(string code)
        {
            return CreateSession(
                TestWorkspace.CreateCSharp(code),
                BraceCompletionSessionProvider.Bracket.OpenCharacter, BraceCompletionSessionProvider.Bracket.CloseCharacter);
        }
    }
}

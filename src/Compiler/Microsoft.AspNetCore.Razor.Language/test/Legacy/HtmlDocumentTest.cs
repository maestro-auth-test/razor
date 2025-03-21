﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy;

public class HtmlDocumentTest() : ParserTestBase(layer: TestProject.Layer.Compiler)
{
    private static readonly TestFile Nested1000 = TestFile.Create("TestFiles/nested-1000.html", typeof(HtmlDocumentTest));

    [Fact]
    public void NestedCodeBlockWithMarkupSetsDotAsMarkup()
    {
        ParseDocumentTest("@if (true) { @if(false) { <div>@something.</div> } }");
    }

    [Fact]
    public void OutputsEmptyBlockWithEmptyMarkupSpanIfContentIsEmptyString()
    {
        ParseDocumentTest(string.Empty);
    }

    [Fact]
    public void OutputsWhitespaceOnlyContentAsSingleWhitespaceMarkupSpan()
    {
        ParseDocumentTest("          ");
    }

    [Fact]
    public void AcceptsSwapTokenAtEndOfFileAndOutputsZeroLengthCodeSpan()
    {
        ParseDocumentTest("@");
    }

    [Fact]
    public void CorrectlyHandlesOddlySpacedHTMLElements()
    {
        ParseDocumentTest("<div ><p class = 'bar'> Foo </p></div >");
    }

    [Fact]
    public void CorrectlyHandlesSingleLineOfMarkupWithEmbeddedStatement()
    {
        ParseDocumentTest("<div>Foo @if(true) {} Bar</div>");
    }

    [Fact]
    public void WithinSectionDoesNotCreateDocumentLevelSpan()
    {
        ParseDocumentTest("""
            @section Foo {
                <html></html>
            }
            """,
            new[] { SectionDirective.Directive, });
    }

    [Fact]
    public void ParsesWholeContentAsOneSpanIfNoSwapCharacterEncountered()
    {
        ParseDocumentTest("foo baz");
    }

    [Fact]
    public void HandsParsingOverToCodeParserWhenAtSignEncounteredAndEmitsOutput()
    {
        ParseDocumentTest("foo @bar baz");
    }

    [Fact]
    public void EmitsAtSignAsMarkupIfAtEndOfFile()
    {
        ParseDocumentTest("foo @");
    }

    [Fact]
    public void EmitsCodeBlockIfFirstCharacterIsSwapCharacter()
    {
        ParseDocumentTest("@bar");
    }

    [Fact]
    public void ParseDocumentDoesNotSwitchToCodeOnEmailAddressInText()
    {
        ParseDocumentTest("example@microsoft.com");
    }

    [Fact]
    public void DoesNotSwitchToCodeOnEmailAddressInAttribute()
    {
        ParseDocumentTest("<a href=\"mailto:example@microsoft.com\">Email me</a>");
    }

    [Fact]
    public void DoesNotReturnErrorOnMismatchedTags()
    {
        ParseDocumentTest("Foo <div><p></p></p> Baz");
    }

    [Fact]
    public void ReturnsOneMarkupSegmentIfNoCodeBlocksEncountered()
    {
        ParseDocumentTest("Foo Baz<!--Foo-->Bar<!--F> Qux");
    }

    [Fact]
    public void RendersTextPseudoTagAsMarkup()
    {
        ParseDocumentTest("Foo <text>Foo</text>");
    }

    [Fact]
    public void AcceptsEndTagWithNoMatchingStartTag()
    {
        ParseDocumentTest("Foo </div> Bar");
    }

    [Fact]
    public void NoLongerSupportsDollarOpenBraceCombination()
    {
        ParseDocumentTest("<foo>${bar}</foo>");
    }

    [Fact]
    public void IgnoresTagsInContentsOfScriptTag()
    {
        ParseDocumentTest(@"<script>foo<bar baz='@boz'></script>");
    }

    [Fact]
    public void DoesNotRenderExtraNewLineAtTheEndOfVerbatimBlock()
    {
        ParseDocumentTest("@{\r\n}\r\n<html>");
    }

    [Fact]
    public void DoesNotRenderExtraWhitespaceAndNewLineAtTheEndOfVerbatimBlock()
    {
        ParseDocumentTest("@{\r\n} \t\r\n<html>");
    }

    [Fact]
    public void DoesNotRenderNewlineAfterTextTagInVerbatimBlockIfFollowedByCSharp()
    {
        // ParseDocumentDoesNotRenderExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByCSharp
        ParseDocumentTest("@{<text>Blah</text>\r\n\r\n}<html>");
    }

    [Fact]
    public void RendersExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByHtml()
    {
        ParseDocumentTest("@{<text>Blah</text>\r\n<input/>\r\n}<html>");
    }

    [Fact]
    public void RendersNewlineAfterTextTagInVerbatimBlockIfFollowedByMarkupTransition()
    {
        // ParseDocumentRendersExtraNewlineAtTheEndTextTagInVerbatimBlockIfFollowedByMarkupTransition
        ParseDocumentTest("@{<text>Blah</text>\r\n@: Bleh\r\n}<html>");
    }

    [Fact]
    public void DoesNotIgnoreNewLineAtTheEndOfMarkupBlock()
    {
        ParseDocumentTest("@{\r\n}\r\n<html>\r\n");
    }

    [Fact]
    public void DoesNotIgnoreWhitespaceAtTheEndOfVerbatimBlockIfNoNewlinePresent()
    {
        ParseDocumentTest("@{\r\n}   \t<html>\r\n");
    }

    [Fact]
    public void HandlesNewLineInNestedBlock()
    {
        ParseDocumentTest("@{\r\n@if(true){\r\n} \r\n}\r\n<html>");
    }

    [Fact]
    public void HandlesNewLineAndMarkupInNestedBlock()
    {
        ParseDocumentTest("@{\r\n@if(true){\r\n} <input> }");
    }

    [Fact]
    public void HandlesExtraNewLineBeforeMarkupInNestedBlock()
    {
        ParseDocumentTest("@{\r\n@if(true){\r\n} \r\n<input> \r\n}<html>");
    }

    [Fact]
    public void ParseSectionIgnoresTagsInContentsOfScriptTag()
    {
        ParseDocumentTest(
            @"@section Foo { <script>foo<bar baz='@boz'></script> }",
            new[] { SectionDirective.Directive, });
    }

    [Fact]
    public void ParseBlockCanParse1000NestedElements()
    {
        var content = Nested1000.ReadAllText();

        // Assert - does not throw
        ParseDocument(content);
    }

    [Fact]
    public void WithDoubleTransitionInAttributeValue_DoesNotThrow()
    {
        var input = "{<span foo='@@' />}";
        ParseDocumentTest(input);
    }

    [Fact]
    public void WithDoubleTransitionAtEndOfAttributeValue_DoesNotThrow()
    {
        var input = "{<span foo='abc@@' />}";
        ParseDocumentTest(input);
    }

    [Fact]
    public void WithDoubleTransitionAtBeginningOfAttributeValue_DoesNotThrow()
    {
        var input = "{<span foo='@@def' />}";
        ParseDocumentTest(input);
    }

    [Fact]
    public void WithDoubleTransitionBetweenAttributeValue_DoesNotThrow()
    {
        var input = "{<span foo='abc @@ def' />}";
        ParseDocumentTest(input);
    }

    [Fact]
    public void WithDoubleTransitionWithExpressionBlock_DoesNotThrow()
    {
        var input = "{<span foo='@@@(2+3)' bar='@(2+3)@@@DateTime.Now' baz='@DateTime.Now@@' bat='@DateTime.Now @@' zoo='@@@DateTime.Now' />}";
        ParseDocumentTest(input);
    }

    [Fact]
    public void WithDoubleTransitionInEmail_DoesNotThrow()
    {
        var input = "{<span foo='abc@def.com abc@@def.com @@' />}";
        ParseDocumentTest(input);
    }

    [Fact]
    public void WithDoubleTransitionInRegex_DoesNotThrow()
    {
        var input = @"{<span foo=""/^[a-z0-9!#$%&'*+\/=?^_`{|}~.-]+@@[a-z0-9]([a-z0-9-]*[a-z0-9])?\.([a-z0-9]([a-z0-9-]*[a-z0-9])?)*$/i"" />}";
        ParseDocumentTest(input);
    }

    [Fact]
    public void WithUnexpectedTransitionsInAttributeValue_Throws()
    {
        ParseDocumentTest("<span foo='@ @' />");
    }

    [Fact]
    public void OpenAngle_01()
    {
        ParseDocumentTest("""
            < @("a").@("b")
            """);
    }

    [Fact]
    public void OpenAngle_02()
    {
        ParseDocumentTest("""
            < @("a").@b
            """);
    }

    [Fact]
    public void OpenAngle_03()
    {
        ParseDocumentTest("""
            < @("a")x@("b")
            """);
    }

    [Fact]
    public void OpenAngle_04()
    {
        ParseDocumentTest("""
            < @("a")x@b
            """);
    }

    [Fact]
    public void OpenAngle_05()
    {
        ParseDocumentTest("""
            < @("a").@@("b")
            """);
    }

    [Fact]
    public void OpenAngle_06()
    {
        ParseDocumentTest("""
            < @("a").@@b
            """);
    }

    [Fact]
    public void OpenAngle_07()
    {
        ParseDocumentTest("""
            < @("a")x@@("b")
            """);
    }

    [Fact]
    public void OpenAngle_08()
    {
        ParseDocumentTest("""
            < @("a")x@@b
            """);
    }

    [Fact]
    public void OpenAngle_09()
    {
        ParseDocumentTest("""
            < @("a")x@@@("b")
            """);
    }

    [Fact]
    public void OpenAngle_10()
    {
        ParseDocumentTest("""
            < @("a")x@@@b
            """);
    }

    [Fact]
    public void OpenAngle_11()
    {
        ParseDocumentTest("""
            < @@
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11327")]
    public void QuoteInAttributeName_01()
    {
        ParseDocumentTest("""
            <div class="test""></div>
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/razor/issues/11327")]
    public void QuoteInAttributeName_02()
    {
        ParseDocumentTest("""
            <div class="test"'></div>
            """);
    }
}

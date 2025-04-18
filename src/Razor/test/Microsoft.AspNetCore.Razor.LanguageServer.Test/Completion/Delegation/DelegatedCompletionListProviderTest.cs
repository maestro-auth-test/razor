﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT license. See License.txt in the project root for license information.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Hosting;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.LanguageServer;
using Microsoft.AspNetCore.Razor.Test.Common.Workspaces;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Completion.Delegation;

public class DelegatedCompletionListProviderTest : LanguageServerTestBase
{
    private readonly TestDelegatedCompletionListProvider _provider;
    private readonly VSInternalClientCapabilities _clientCapabilities;
    private readonly RazorCompletionOptions _defaultRazorCompletionOptions;

    public DelegatedCompletionListProviderTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _provider = TestDelegatedCompletionListProvider.Create(LoggerFactory);
        _clientCapabilities = new VSInternalClientCapabilities();
        _defaultRazorCompletionOptions = new RazorCompletionOptions(
            SnippetsSupported: true,
            AutoInsertAttributeQuotes: true,
            CommitElementsWithSpace: true);
    }

    [Fact]
    public async Task HtmlDelegation_Invoked()
    {
        // Arrange
        var completionContext = new VSInternalCompletionContext() { TriggerKind = CompletionTriggerKind.Invoked };
        var codeDocument = CreateCodeDocument("<");
        var documentContext = TestDocumentContext.Create("C:/path/to/file.cshtml", codeDocument);

        // Act
        await _provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: 1,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        // Assert
        var delegatedParameters = _provider.DelegatedParams;
        Assert.NotNull(delegatedParameters);
        Assert.Equal(RazorLanguageKind.Html, delegatedParameters.ProjectedKind);
        Assert.Equal(LspFactory.CreatePosition(0, 1), delegatedParameters.ProjectedPosition);
        Assert.Equal(CompletionTriggerKind.Invoked, delegatedParameters.Context.TriggerKind);
        Assert.Equal(1, delegatedParameters.Identifier.Version);
        Assert.Null(delegatedParameters.ProvisionalTextEdit);
    }

    [Fact]
    public async Task HtmlDelegation_TriggerCharacter()
    {
        // Arrange
        var completionContext = new VSInternalCompletionContext()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Typing,
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = "<",
        };
        var codeDocument = CreateCodeDocument("<");
        var documentContext = TestDocumentContext.Create("C:/path/to/file.cshtml", codeDocument);

        // Act
        await _provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: 1,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        // Assert
        var delegatedParameters = _provider.DelegatedParams;
        Assert.NotNull(delegatedParameters);
        Assert.Equal(RazorLanguageKind.Html, delegatedParameters.ProjectedKind);
        Assert.Equal(LspFactory.CreatePosition(0, 1), delegatedParameters.ProjectedPosition);
        Assert.Equal(CompletionTriggerKind.TriggerCharacter, delegatedParameters.Context.TriggerKind);
        Assert.Equal(VSInternalCompletionInvokeKind.Typing, delegatedParameters.Context.InvokeKind);
        Assert.Equal(1, delegatedParameters.Identifier.Version);
        Assert.Null(delegatedParameters.ProvisionalTextEdit);
    }

    [Fact]
    public async Task HtmlDelegation_UnsupportedTriggerCharacter_ReturnsNull()
    {
        // Arrange
        var completionContext = new VSInternalCompletionContext()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Typing,
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = "|",
        };
        var codeDocument = CreateCodeDocument("|");
        var documentContext = TestDocumentContext.Create("C:/path/to/file.cshtml", codeDocument);

        // Act
        await _provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: 1,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        // Assert
        var delegatedParameters = _provider.DelegatedParams;
        Assert.Null(delegatedParameters);
    }

    [Fact]
    public async Task Delegation_NullResult_ToIncompleteResult()
    {
        // Arrange
        var completionContext = new VSInternalCompletionContext()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Typing,
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = "<",
        };
        var codeDocument = CreateCodeDocument("<");
        var documentContext = TestDocumentContext.Create("C:/path/to/file.cshtml", codeDocument);
        var provider = TestDelegatedCompletionListProvider.CreateWithNullResponse(LoggerFactory);

        // Act
        var delegatedCompletionList = await provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: 1,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        // Assert
        Assert.NotNull(delegatedCompletionList);
        Assert.True(delegatedCompletionList.IsIncomplete);
    }

    [Fact]
    public async Task CSharp_Invoked()
    {
        // Arrange & Act
        var completionList = await GetCompletionListAsync("@$$", CompletionTriggerKind.Invoked);

        // Assert
        Assert.Contains(completionList.Items, item => item.Label == "System");
    }

    [Fact]
    public async Task CSharp_At_TranslatesToInvoked_Triggered()
    {
        // Arrange & Act
        var completionList = await GetCompletionListAsync("@$$", CompletionTriggerKind.TriggerCharacter);

        // Assert
        Assert.Contains(completionList.Items, item => item.Label == "System");
    }

    [Fact]
    public async Task CSharp_Operator_Triggered()
    {
        // Arrange & Act
        var completionList = await GetCompletionListAsync("@(DateTime.$$)", CompletionTriggerKind.TriggerCharacter);

        // Assert
        Assert.Contains(completionList.Items, item => item.Label == "Now");
    }

    [Fact]
    public async Task RazorDelegation_Noop()
    {
        // Arrange
        var completionContext = new VSInternalCompletionContext() { TriggerKind = CompletionTriggerKind.Invoked };
        var codeDocument = CreateCodeDocument("@functions ");
        var documentContext = TestDocumentContext.Create("C:/path/to/file.razor", codeDocument);

        // Act
        var completionList = await _provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: 11,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        // Assert
        Assert.Null(completionList);
        var delegatedParameters = _provider.DelegatedParams;
        Assert.Null(delegatedParameters);
    }

    [Fact]
    public async Task ProvisionalCompletion_TranslatesToCSharpWithProvisionalTextEdit()
    {
        // Arrange
        var completionContext = new VSInternalCompletionContext()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Typing,
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = ".",
        };
        var codeDocument = CreateCodeDocument("@DateTime.");
        var documentContext = TestDocumentContext.Create("C:/path/to/file.cshtml", codeDocument);

        // Act
        await _provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: 10,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        // Assert
        var delegatedParameters = _provider.DelegatedParams;
        Assert.NotNull(delegatedParameters);
        Assert.Equal(RazorLanguageKind.CSharp, delegatedParameters.ProjectedKind);

        // Just validating that we're generating code in a way that's different from the top-level document. Don't need to be specific.
        Assert.True(delegatedParameters.ProjectedPosition.Line > 2);
        Assert.Equal(CompletionTriggerKind.TriggerCharacter, delegatedParameters.Context.TriggerKind);
        Assert.Equal(VSInternalCompletionInvokeKind.Typing, delegatedParameters.Context.InvokeKind);
        Assert.Equal(1, delegatedParameters.Identifier.Version);
        Assert.NotNull(delegatedParameters.ProvisionalTextEdit);
    }

    [Fact]
    public async Task DotTriggerInMiddleOfCSharpImplicitExpressionNotTreatedAsProvisional()
    {
        // Arrange
        var completionContext = new VSInternalCompletionContext()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Typing,
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = ".",
        };
        var codeDocument = CreateCodeDocument("@DateTime.Now");
        var documentContext = TestDocumentContext.Create("C:/path/to/file.cshtml", codeDocument);

        // Act
        await _provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: 10,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        // Assert
        var delegatedParameters = _provider.DelegatedParams;
        Assert.NotNull(delegatedParameters);
        Assert.Equal(RazorLanguageKind.CSharp, delegatedParameters.ProjectedKind);

        // Just validating that we're generating code in a way that's different from the top-level document. Don't need to be specific.
        Assert.True(delegatedParameters.ProjectedPosition.Line > 2);
        Assert.Equal(CompletionTriggerKind.TriggerCharacter, delegatedParameters.Context.TriggerKind);
        Assert.Equal(VSInternalCompletionInvokeKind.Typing, delegatedParameters.Context.InvokeKind);
        Assert.Equal(1, delegatedParameters.Identifier.Version);
        Assert.Null(delegatedParameters.ProvisionalTextEdit);
    }

    [Theory]
    [InlineData("$$", true)]
    [InlineData("<$$", false)]
    [InlineData(">$$", true)]
    [InlineData("$$<", true)]
    [InlineData("$$>", false)] // This is the only case that returns false but should return true. It's unlikely a user will type this, but it's complex to solve. Consider this a known and acceptable bug.
    [InlineData("<div>$$</div>", true)]
    [InlineData("$$<div></div>", true)]
    [InlineData("<div></div>$$", true)]
    [InlineData("<$$div></div>", false)]
    [InlineData("<div$$></div>", false)]
    [InlineData("<div class=\"$$\"></div>", false)]
    [InlineData("<div><$$/div>", false)]
    [InlineData("<div></div$$>", false)]
    public async Task ShouldIncludeSnippets(string input, bool shouldIncludeSnippets)
    {
        var clientConnection = new TestClientConnection();

        TestFileMarkupParser.GetPosition(input, out var code, out var cursorPosition);
        var codeDocument = CreateCodeDocument(code);
        var documentContext = TestDocumentContext.Create("C:/path/to/file.cshtml", codeDocument);

        var generatedPosition = new LinePosition(0, cursorPosition);

        var documentMappingServiceMock = new StrictMock<IDocumentMappingService>();
        documentMappingServiceMock
            .Setup(x => x.TryMapToGeneratedDocumentPosition(It.IsAny<IRazorGeneratedDocument>(), It.IsAny<int>(), out generatedPosition, out It.Ref<int>.IsAny))
            .Returns(true);

        var completionProvider = new DelegatedCompletionListProvider(
            documentMappingServiceMock.Object,
            clientConnection,
            new CompletionListCache(),
            new CompletionTriggerAndCommitCharacters(TestLanguageServerFeatureOptions.Instance));

        var requestSent = false;
        clientConnection.RequestSent += (s, o) =>
        {
            requestSent = true;

            var @params = Assert.IsType<DelegatedCompletionParams>(o);
            Assert.Equal(shouldIncludeSnippets, @params.ShouldIncludeSnippets);
        };

        var completionContext = new VSInternalCompletionContext()
        {
            InvokeKind = VSInternalCompletionInvokeKind.Typing,
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = ".",
        };

        await completionProvider.GetCompletionListAsync(
            codeDocument,
            cursorPosition,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            DisposalToken);

        Assert.True(requestSent);
    }

    private async Task<RazorVSInternalCompletionList> GetCompletionListAsync(string content, CompletionTriggerKind triggerKind)
    {
        TestFileMarkupParser.GetPosition(content, out var output, out var cursorPosition);
        var codeDocument = CreateCodeDocument(output);
        var csharpSourceText = codeDocument.GetCSharpSourceText();
        var csharpDocumentUri = new Uri("C:/path/to/file.razor__virtual.g.cs");
        var serverCapabilities = new VSInternalServerCapabilities()
        {
            CompletionProvider = new CompletionOptions
            {
                ResolveProvider = true,
                TriggerCharacters = [" ", "(", "=", "#", ".", "<", "[", "{", "\"", "/", ":", "~"]
            }
        };
        await using var csharpServer = await CSharpTestLspServerHelpers.CreateCSharpLspServerAsync(
            csharpSourceText, csharpDocumentUri, serverCapabilities, DisposalToken);

        await csharpServer.OpenDocumentAsync(csharpDocumentUri, csharpSourceText.ToString());

        var triggerCharacter = triggerKind == CompletionTriggerKind.TriggerCharacter ? output[cursorPosition - 1].ToString() : null;
        var invocationKind = triggerKind == CompletionTriggerKind.TriggerCharacter ? VSInternalCompletionInvokeKind.Typing : VSInternalCompletionInvokeKind.Explicit;

        var completionContext = new VSInternalCompletionContext()
        {
            TriggerKind = triggerKind,
            TriggerCharacter = triggerCharacter,
            InvokeKind = invocationKind,
        };
        var documentContext = TestDocumentContext.Create("C:/path/to/file.razor", codeDocument);
        var provider = TestDelegatedCompletionListProvider.Create(csharpServer, LoggerFactory, DisposalToken);

        var completionList = await provider.GetCompletionListAsync(
            codeDocument,
            absoluteIndex: cursorPosition,
            completionContext,
            documentContext,
            _clientCapabilities,
            _defaultRazorCompletionOptions,
            correlationId: Guid.Empty,
            cancellationToken: DisposalToken);

        return completionList;
    }

    private class TestClientConnection(object response = null) : IClientConnection
    {
        public event EventHandler<object> NotificationSent;
        public event EventHandler<object> RequestSent;

        private object _response = response;

        public Task SendNotificationAsync<TParams>(string method, TParams @params, CancellationToken cancellationToken)
        {
            NotificationSent?.Invoke(this, @params);
            return Task.CompletedTask;
        }

        public Task SendNotificationAsync(string method, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> SendRequestAsync<TParams, TResponse>(string method, TParams @params, CancellationToken cancellationToken)
        {
            RequestSent?.Invoke(this, @params);
            return Task.FromResult((TResponse)_response);
        }
    }
}

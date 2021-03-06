﻿//
// ExternAliasContextHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class ExternAliasContextHandler : CompletionContextHandler
	{
		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			var document = completionContext.Document;
			var position = completionContext.Position;

			var tree = ctx.SyntaxTree;

			if (tree.IsInNonUserCode(position, cancellationToken))
				return Enumerable.Empty<CompletionData> ();

			var targetToken = tree.FindTokenOnLeftOfPosition(position, cancellationToken).GetPreviousTokenIfTouchingWord(position);
			if (targetToken.IsKind(SyntaxKind.AliasKeyword) && targetToken.Parent.IsKind(SyntaxKind.ExternAliasDirective))
			{
				var compilation = await document.GetCSharpCompilationAsync(cancellationToken).ConfigureAwait(false);
				var aliases = compilation.ExternalReferences.Where(r => r.Properties.Aliases != null).SelectMany(r => r.Properties.Aliases).ToSet();

				if (aliases.Any())
				{
					var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
					var usedAliases = root.ChildNodes().OfType<ExternAliasDirectiveSyntax>().Where(e => !e.Identifier.IsMissing).Select(e => e.Identifier.ValueText);
					foreach (var used in usedAliases) {
						aliases.Remove (used); 
					}
					aliases.Remove(MetadataReferenceProperties.GlobalAlias);
					return aliases.Select (e => engine.Factory.CreateGenericData (this, e, GenericDataType.Undefined));
				}
			}

			return Enumerable.Empty<CompletionData> ();
		}
	}
}
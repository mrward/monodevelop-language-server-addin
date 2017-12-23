//
// Based on:
// https://github.com/Microsoft/VSSDK-Extensibility-Samples
// LanguageServerProtocol/MockLanguageExtension/FooContentTypeDefinition.cs
//
// Copyright (c) Microsoft
//

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace MockLanguageExtension
{
	#pragma warning disable CS0649 // Field is never assigned to.

	public class FooContentDefinition
	{
		[Export]
		[Name("foo")]
		[BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
		internal static ContentTypeDefinition FooContentTypeDefinition;

		[Export]
		[FileExtension(".foo")]
		[ContentType("foo")]
		internal static FileExtensionToContentTypeDefinition FooFileExtensionDefinition;
	}

	#pragma warning restore CS0649
}
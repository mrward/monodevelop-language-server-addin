//
// CompletionItemExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Text.Adornments;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.LanguageServer.Client
{
	static class CompletionItemExtensions
	{
		public static IconId GetIcon (this CompletionItem item)
		{
			switch (item.Kind) {
				case CompletionItemKind.Property:
					return Stock.Property;

				case CompletionItemKind.Constructor:
				case CompletionItemKind.Method:
				case CompletionItemKind.Function:
					return Stock.Method;

				case CompletionItemKind.Constant:
				case CompletionItemKind.EnumMember:
				case CompletionItemKind.Keyword:
				case CompletionItemKind.Snippet:
				case CompletionItemKind.Text:
				case CompletionItemKind.Value:
					return Stock.Literal;

				case CompletionItemKind.Class:
				case CompletionItemKind.TypeParameter:
					return Stock.Class;

				case CompletionItemKind.Event:
					return Stock.Event;

				case CompletionItemKind.Field:
					return Stock.PrivateField;

				case CompletionItemKind.Interface:
					return Stock.Interface;

				case CompletionItemKind.Module:
				case CompletionItemKind.Unit:
					return Stock.NameSpace;

				case CompletionItemKind.Enum:
					return Stock.Enum;

				case CompletionItemKind.Variable:
					return "md-variable";

				case CompletionItemKind.File:
					return Stock.EmptyFileIcon;

				case CompletionItemKind.Folder:
					return Stock.OpenFolder;

				case CompletionItemKind.Reference:
					return Stock.Reference;

				case CompletionItemKind.Struct:
					return Stock.Struct;

				default:
					return null;
			}
		}

		public static ImageId GetImageId (this CompletionItem item)
		{
			switch (item.Kind) {
				case CompletionItemKind.Property:
					return CreateImageId (KnownImageIds.Property);

				case CompletionItemKind.Constructor:
				case CompletionItemKind.Method:
				case CompletionItemKind.Function:
					return CreateImageId (KnownImageIds.Method);

				case CompletionItemKind.Constant:
					return CreateImageId (KnownImageIds.Constant);

				case CompletionItemKind.EnumMember:
				case CompletionItemKind.Keyword:
				case CompletionItemKind.Text:
				case CompletionItemKind.Value:
					return CreateImageId (KnownImageIds.Literal);

				case CompletionItemKind.Class:
				case CompletionItemKind.TypeParameter:
					return CreateImageId (KnownImageIds.Class);

				case CompletionItemKind.Event:
					return CreateImageId (KnownImageIds.Event);

				case CompletionItemKind.Field:
					return CreateImageId (KnownImageIds.Field);

				case CompletionItemKind.Interface:
					return CreateImageId (KnownImageIds.Interface);

				case CompletionItemKind.Module:
				case CompletionItemKind.Unit:
					return CreateImageId (KnownImageIds.Namespace);

				case CompletionItemKind.Enum:
					return CreateImageId (KnownImageIds.Enumeration);

				case CompletionItemKind.Variable:
					return CreateImageId (KnownImageIds.LocalVariable);

				case CompletionItemKind.File:
					return CreateImageId (KnownImageIds.FileType);

				case CompletionItemKind.Folder:
					return CreateImageId (KnownImageIds.OpenFolder);

				case CompletionItemKind.Reference:
					return CreateImageId (KnownImageIds.Reference);

				case CompletionItemKind.Snippet:
					return CreateImageId (KnownImageIds.Snippet);

				case CompletionItemKind.Struct:
					return CreateImageId (KnownImageIds.Structure);

				default:
					return default (ImageId);
			}
		}

		static ImageId CreateImageId (int id)
		{
			return new ImageId (KnownImageIds.ImageCatalogGuid, id);
		}

		public static ImageElement GetImageElement (this CompletionItem item)
		{
			ImageId imageId = item.GetImageId ();
			if (imageId != null) {
				return new ImageElement (imageId);
			}

			return default (ImageElement);
		}

		public static string GetDescription (this CompletionItem item)
		{
			string description = item.Detail;
			string documentation = item.Documentation.GetStringValue ();

			if (!string.IsNullOrEmpty (documentation)) {
				if (description == null) {
					description = documentation;
				} else {
					description += Environment.NewLine + documentation;
				}
			}

			return description ?? string.Empty;
		}

		public static string GetInsertText (this CompletionItem item)
		{
			if (item.InsertText != null) {
				return item.InsertText;
			}

			string insertText = item?.TextEdit?.NewText;

			if (insertText != null) {
				return insertText;
			}

			return item.Label;
		}
	}
}

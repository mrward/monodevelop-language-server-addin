//
// WorkspaceSymbolSearchResult.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.LanguageServer.Client
{
	class WorkspaceSymbolSearchResult : SearchResult
	{
		SymbolInformation symbolInfo;
		SearchResultType searchResultType;

		public WorkspaceSymbolSearchResult (string pattern, SymbolInformation symbolInfo)
			: base (pattern, symbolInfo.Name, 0)
		{
			this.symbolInfo = symbolInfo;
			searchResultType = GetSearchResultType (symbolInfo);
		}

		public override SearchResultType SearchResultType {
			get { return searchResultType; }
		}

		static SearchResultType GetSearchResultType (SymbolInformation symbolInfo)
		{
			switch (symbolInfo.Kind) {
				case SymbolKind.File:
					return SearchResultType.File;
				case SymbolKind.Class:
				case SymbolKind.Interface:
					return SearchResultType.Type;
				default:
					return SearchResultType.Member;
			}
		}

		public override string PlainText {
			get {
				return symbolInfo.Name;
			}
		}

		public override bool CanActivate {
			get {
				return File != null;
			}
		}

		public override void Activate ()
		{
			var info = new FileOpenInformation (File, null);
			if (symbolInfo.Location.Range?.Start != null) {
				info.Line = symbolInfo.Location.Range.Start.Line + 1;
				info.Column = symbolInfo.Location.Range.Start.Character + 1;
			}

			IdeApp.Workbench.OpenDocument (info).Ignore ();
		}

		public override string Description {
			get {
				return GetDescription ();
			}
		}

		public override string File {
			get {
				if (symbolInfo.Location != null) {
					return symbolInfo.Location.Uri;
				}
				return null;
			}
		}

		public override Xwt.Drawing.Image Icon {
			get {
				return ImageService.GetIcon (GetStockIcon (), Gtk.IconSize.Menu);
			}
		}

		string GetDescription ()
		{
			string filePathDescription = null;
			if (File != null) {
				filePathDescription = GettextCatalog.GetString (" (file {0})", new FilePath (File));
			}

			switch (symbolInfo.Kind) {
				case SymbolKind.Array:
					return GettextCatalog.GetString ("array{0}", filePathDescription);
				case SymbolKind.Boolean:
					return GettextCatalog.GetString ("boolean{0}", filePathDescription);
				case SymbolKind.Constant:
					return GettextCatalog.GetString ("constant{0}", filePathDescription);
				case SymbolKind.Class:
					return GettextCatalog.GetString ("class{0}", filePathDescription);
				case SymbolKind.Constructor:
					return GettextCatalog.GetString ("constructor{0}", filePathDescription);
				case SymbolKind.Enum:
					return GettextCatalog.GetString ("enumeration{0}", filePathDescription);
				case SymbolKind.Field:
					return GettextCatalog.GetString ("field{0}", filePathDescription);
				case SymbolKind.File:
					return filePathDescription;
				case SymbolKind.Function:
					return GettextCatalog.GetString ("function{0}", filePathDescription);
				case SymbolKind.Interface:
					return GettextCatalog.GetString ("interface{0}", filePathDescription);
				case SymbolKind.Method:
					return GettextCatalog.GetString ("method{0}", filePathDescription);
				case SymbolKind.Module:
					return GettextCatalog.GetString ("module{0}", filePathDescription);
				case SymbolKind.Namespace:
					return GettextCatalog.GetString ("namespace{0}", filePathDescription);
				case SymbolKind.Number:
					return GettextCatalog.GetString ("field{0}", filePathDescription);
				case SymbolKind.Property:
					return GettextCatalog.GetString ("property{0}", filePathDescription);
				case SymbolKind.String:
					return GettextCatalog.GetString ("string{0}", filePathDescription);
				case SymbolKind.Variable:
					return GettextCatalog.GetString ("variable{0}", filePathDescription);
				default:
					return GettextCatalog.GetString ("symbol{0}", filePathDescription);
			}
		}

		IconId GetStockIcon ()
		{
			switch (symbolInfo.Kind) {
				case SymbolKind.Array:
				case SymbolKind.Number:
				case SymbolKind.Boolean:
				case SymbolKind.Constant:
				case SymbolKind.String:
					return Stock.Literal;
				case SymbolKind.Field:
				case SymbolKind.Variable:
					return Stock.Field;
				case SymbolKind.Class:
					return Stock.Class;
				case SymbolKind.Interface:
					return Stock.Interface;
				case SymbolKind.Constructor:
				case SymbolKind.Function:
				case SymbolKind.Method:
				case SymbolKind.Module:
					return Stock.Method;
				case SymbolKind.Enum:
					return Stock.Enum;
				case SymbolKind.File:
					return Stock.GenericFile;
				case SymbolKind.Namespace:
					return Stock.NameSpace;
				case SymbolKind.Package:
					return Stock.Package;
				case SymbolKind.Property:
					return Stock.Property;
				default:
					return IconId.Null;
			}
		}
	}
}

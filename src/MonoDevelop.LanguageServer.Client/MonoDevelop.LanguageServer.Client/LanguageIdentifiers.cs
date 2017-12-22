//
// LanguageIdentifiers.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core;

namespace MonoDevelop.LanguageServer.Client
{
	/// <summary>
	/// https://code.visualstudio.com/docs/languages/identifiers
	/// </summary>
	static class LanguageIdentifiers
	{
		public static string GetLanguageIdentifier (FilePath fileName)
		{
			string identifier = GetLanguageIdentifierFromFileNameWithoutExtension (fileName.FileNameWithoutExtension);
			if (identifier != null) {
				return identifier;
			}

			return GetLanguageIdentifierFromFileExtension (fileName.Extension);
		}

		static string GetLanguageIdentifierFromFileNameWithoutExtension (string fileNameWithoutExtension)
		{
			switch (fileNameWithoutExtension.ToLower ()) {
				case "dockerfile":
					return "dockerfile";
				case "make":
					return "makefile";
			}

			return null;
		}

		static string GetLanguageIdentifierFromFileExtension (string extension)
		{
			switch (extension.ToLower ()) {
				case ".bat":
					return "bat";
				case ".bib":
					return "bibtex";
				case ".clj":
					return "clojure";
				case ".coffee":
					return "coffeescript";
				case ".c":
					return "c";
				case ".cpp":
					return "cpp";
				case ".cs":
					return "csharp";
				case ".css":
					return "css";
				case ".cxx":
					return "cpp";
				case ".diff":
					return "diff";
				case ".fs":
					return "fsharp";
				case ".go":
					return "go";
				case ".groovy":
					return "groovy";
				case ".h":
					return "c";
				case ".handlebars":
					return "handlebars";
				case ".hbs":
					return "handlebars";
				case ".html":
					return "html";
				case ".hxx":
					return "cpp";
				case ".ini":
					return "ini";
				case ".jade":
					return "jade";
				case ".java":
					return "java";
				case ".js":
					return "javascript";
				case ".json":
					return "json";
				case ".latex":
					return "latex";
				case ".less":
					return "less";
				case ".lua":
					return "lua";
				case ".m":
					return "objective-c";
				case ".markdown":
					return "markdown";
				case ".md":
					return "markdown";
				case ".mm":
					return "objective-cpp";
				case ".php":
					return "php";
				case ".ps1":
					return "powershell";
				case ".pug":
					return "jade";
				case ".py":
					return "python";
				case ".r":
					return "r";
				case ".cshtml":
					return "razor";
				case ".rb":
					return "ruby";
				case ".rs":
					return "rust";
				case ".sass":
					return "sass";
				case ".scss":
					return "scss";
				case ".shader":
					return "shaderlab";
				case ".sh":
					return "shellscript";
				case ".sql":
					return "sql";
				case ".swift":
					return "swift";
				case ".ts":
					return "typescript";
				case ".tex":
					return "tex";
				case ".xml":
					return "xml";
				case ".xsl":
					return "xsl";
				case ".yaml":
					return "yaml";
			}

			return null;
		}
	}
}

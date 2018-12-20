//
// CodeTemplateFactory.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Based on CodeTemplateService
// https://github.com/mono/monodevelop/main/src/core/MonoDevelop.Ide/MonoDevelop.Ide.CodeTemplates/CodeTemplateService.cs
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
using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.CodeTemplates
{
	static class CodeTemplateFactory
	{
		public static CodeTemplate ConvertToTemplate (string snippet)
		{
			var result = new CodeTemplate ();
			var sb = new StringBuilder ();
			var nameBuilder = new StringBuilder ();
			bool readDollar = false;
			bool inBracketExpression = false;
			bool inExpressionContent = false;
			bool inVariable = false;
			int number = 0;
			foreach (var ch in snippet) {
				if (inVariable) {
					if (char.IsLetter (ch)) {
						nameBuilder.Append (ch);
					} else {
						sb.Append (ConvertVariable (nameBuilder.ToString ()));
						nameBuilder.Length = 0;
						inVariable = false;
					}
				}

				if (ch == '$') {
					readDollar = true;
					continue;
				}
				if (readDollar) {
					if (ch == '{') {
						number = 0;
						inBracketExpression = true;
						readDollar = false;
						continue;
					} else if (char.IsLetter (ch)) {
						inVariable = true;
					} else {
						sb.Append ("$$");
						readDollar = false;
					}
				}
				if (inBracketExpression) {
					if (ch == ':') {
						inBracketExpression = false;
						inExpressionContent = true;
						continue;
					}
					number = number * 10 + (ch - '0');
					continue;
				}

				if (inExpressionContent) {
					if (ch == '}') {
						if (number == 0) {
							sb.Append ("$end$");
							sb.Append (nameBuilder);
						} else {
							sb.Append ("$");
							sb.Append (nameBuilder);
							sb.Append ("$");
							result.AddVariable (new CodeTemplateVariable (nameBuilder.ToString ()) { Default = nameBuilder.ToString (), IsEditable = true });
						}
						nameBuilder.Length = 0;
						number = 0;
						inExpressionContent = false;
						continue;
					}
					nameBuilder.Append (ch);
					continue;
				}
				sb.Append (ch);
			}
			if (inVariable) {
				sb.Append (ConvertVariable (nameBuilder.ToString ()));
				nameBuilder.Length = 0;
				inVariable = false;
			}
			result.Code = sb.ToString ();
			result.CodeTemplateContext = CodeTemplateContext.Standard;
			result.CodeTemplateType = CodeTemplateType.Expansion;
			return result;
		}

		static string ConvertVariable (string textmateVariable)
		{
			switch (textmateVariable) {
				case "SELECTION":
				case "TM_SELECTED_TEXT":
					return "$selected$";
				case "TM_CURRENT_LINE":
				case "TM_CURRENT_WORD":
				case "TM_FILENAME":
				case "TM_FILEPATH":
				case "TM_FULLNAME":
				case "TM_LINE_INDEX":
				case "TM_LINE_NUMBER":
				case "TM_SOFT_TABS":
				case "TM_TAB_SIZE":
					return "$" + textmateVariable + "$";
			}
			return "";
		}
	}
}

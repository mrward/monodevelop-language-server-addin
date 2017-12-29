//
// LanguageClientParameterHintingData.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.LanguageServer.Client
{
	class LanguageClientParameterHintingData : ParameterHintingData
	{
		SignatureInformation signature;

		public LanguageClientParameterHintingData (SignatureInformation signature)
			: base (null)
		{
			this.signature = signature;
		}

		public override bool IsParameterListAllowed {
			get { return ParameterCount > 0; }
		}

		public override int ParameterCount {
			get { return signature.Parameters.Length; }
		}

		public override string GetParameterName (int parameter)
		{
			return signature.Parameters [parameter].Label;
		}

		public override Task<TooltipInformation> CreateTooltipInformation (
			TextEditor editor,
			DocumentContext ctx,
			int currentParameter,
			bool smartWrap,
			CancellationToken cancelToken)
		{
			var tooltipInfo = new TooltipInformation ();
			tooltipInfo.SummaryMarkup = EscapeMarkup (signature.Documentation);
			tooltipInfo.SignatureMarkup = EscapeMarkup (GetSignatureMarkup ());
			return Task.FromResult (tooltipInfo);
		}

		static string EscapeMarkup (string text)
		{
			return GLib.Markup.EscapeText (text ?? string.Empty);
		}

		string GetSignatureMarkup ()
		{
			if (string.IsNullOrEmpty (signature.Label)) {
				return string.Empty;
			}

			int wrapLineLength = 50;
			int currentLineLength = 0;

			var signatureBuilder = new StringBuilder ();
			foreach (char c in signature.Label) {

				if ((currentLineLength >= wrapLineLength) && (c == ' ')) {
					signatureBuilder.Append (Environment.NewLine);
					currentLineLength = 0;
				} else {
					signatureBuilder.Append (c);
				}

				currentLineLength++;
			}

			return signatureBuilder.ToString ();
		}
	}
}

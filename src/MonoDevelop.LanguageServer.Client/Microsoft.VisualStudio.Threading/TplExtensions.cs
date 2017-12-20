﻿//
// TplExtensions.cs
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

using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Threading
{
	public static class TplExtensions
	{
		/// <summary>
		/// Code is incorrect. It should execute each handler fully before the next
		/// in the list is executed.
		/// 
		/// https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualstudio.threading.tplextensions.invokeasync
		/// </summary>
		public static Task InvokeAsync<TEventArgs> (this AsyncEventHandler<TEventArgs> handlers, object sender, TEventArgs e)
			where TEventArgs : EventArgs
		{
			return handlers (sender, e);
		}
	}
}

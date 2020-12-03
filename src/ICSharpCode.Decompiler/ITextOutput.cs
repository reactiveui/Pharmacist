﻿// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.


using System.Reflection.Metadata;

using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	public interface ITextOutput
	{
		string IndentationString { get; set; }
		void Indent();
		void Unindent();
		void Write(char ch);
		void Write(string text);
		void WriteLine();
		void WriteReference(OpCodeInfo opCode, bool omitSuffix = false);
		void WriteReference(PEFile module, Handle handle, string text, string protocol = "decompile", bool isDefinition = false);
		void WriteReference(IType type, string text, bool isDefinition = false);
		void WriteReference(IMember member, string text, bool isDefinition = false);
		void WriteLocalReference(string text, object reference, bool isDefinition = false);

		void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false);
		void MarkFoldEnd();
	}

	public static class TextOutputExtensions
	{
		public static void Write(this ITextOutput output, string format, params object[] args)
		{
			output.Write(string.Format(format, args));
		}

		public static void WriteLine(this ITextOutput output, string text)
		{
			output.Write(text);
			output.WriteLine();
		}

		public static void WriteLine(this ITextOutput output, string format, params object[] args)
		{
			output.WriteLine(string.Format(format, args));
		}
	}
}

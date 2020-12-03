﻿// Copyright (c) 2018 Siegfried Pammer
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

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;

namespace ICSharpCode.Decompiler.Metadata
{
	public sealed class AssemblyResolutionException : FileNotFoundException
	{
		public IAssemblyReference Reference { get; }

		public AssemblyResolutionException(IAssemblyReference reference)
			: this(reference, null)
		{
		}

		public AssemblyResolutionException(IAssemblyReference reference, Exception innerException)
			: base($"Failed to resolve assembly: '{reference}'", innerException)
		{
			this.Reference = reference;
		}
	}

	public interface IAssemblyResolver
	{
#if !VSADDIN
		PEFile Resolve(IAssemblyReference reference);
		PEFile ResolveModule(PEFile mainModule, string moduleName);
#endif
	}

	public class AssemblyReferenceClassifier
	{
		/// <summary>
		/// For GAC assembly references, the WholeProjectDecompiler will omit the HintPath in the
		/// generated .csproj file.
		/// </summary>
		public virtual bool IsGacAssembly(IAssemblyReference reference)
		{
			return UniversalAssemblyResolver.GetAssemblyInGac(reference) != null;
		}

		/// <summary>
		/// For .NET Core framework references, the WholeProjectDecompiler will omit the
		/// assembly reference if the runtimePack is already included as an SDK.
		/// </summary>
		public virtual bool IsSharedAssembly(IAssemblyReference reference, out string runtimePack)
		{
			runtimePack = null;
			return false;
		}
	}

	public interface IAssemblyReference
	{
		string Name { get; }
		string FullName { get; }
		Version Version { get; }
		string Culture { get; }
		byte[] PublicKeyToken { get; }

		bool IsWindowsRuntime { get; }
		bool IsRetargetable { get; }
	}

	public class AssemblyNameReference : IAssemblyReference
	{
		string fullName;

		public string Name { get; private set; }

		public string FullName {
			get {
				if (fullName != null)
					return fullName;

				const string sep = ", ";

				var builder = new StringBuilder();
				builder.Append(Name);
				builder.Append(sep);
				builder.Append("Version=");
				builder.Append((Version ?? UniversalAssemblyResolver.ZeroVersion).ToString(fieldCount: 4));
				builder.Append(sep);
				builder.Append("Culture=");
				builder.Append(string.IsNullOrEmpty(Culture) ? "neutral" : Culture);
				builder.Append(sep);
				builder.Append("PublicKeyToken=");

				var pk_token = PublicKeyToken;
				if (pk_token != null && pk_token.Length > 0)
				{
					for (int i = 0; i < pk_token.Length; i++)
					{
						builder.Append(pk_token[i].ToString("x2"));
					}
				}
				else
					builder.Append("null");

				if (IsRetargetable)
				{
					builder.Append(sep);
					builder.Append("Retargetable=Yes");
				}

				return fullName = builder.ToString();
			}
		}

		public Version Version { get; private set; }

		public string Culture { get; private set; }

		public byte[] PublicKeyToken { get; private set; }

		public bool IsWindowsRuntime { get; private set; }

		public bool IsRetargetable { get; private set; }

		public static AssemblyNameReference Parse(string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException(nameof(fullName));
			if (fullName.Length == 0)
				throw new ArgumentException("Name can not be empty");

			var name = new AssemblyNameReference();
			var tokens = fullName.Split(',');
			for (int i = 0; i < tokens.Length; i++)
			{
				var token = tokens[i].Trim();

				if (i == 0)
				{
					name.Name = token;
					continue;
				}

				var parts = token.Split('=');
				if (parts.Length != 2)
					throw new ArgumentException("Malformed name");

				switch (parts[0].ToLowerInvariant())
				{
					case "version":
						name.Version = new Version(parts[1]);
						break;
					case "culture":
						name.Culture = parts[1] == "neutral" ? "" : parts[1];
						break;
					case "publickeytoken":
						var pk_token = parts[1];
						if (pk_token == "null")
							break;

						name.PublicKeyToken = new byte[pk_token.Length / 2];
						for (int j = 0; j < name.PublicKeyToken.Length; j++)
							name.PublicKeyToken[j] = Byte.Parse(pk_token.Substring(j * 2, 2), System.Globalization.NumberStyles.HexNumber);

						break;
				}
			}

			return name;
		}

		public override string ToString()
		{
			return FullName;
		}
	}

#if !VSADDIN
	public class AssemblyReference : IAssemblyReference
	{
		static readonly SHA1 sha1 = SHA1.Create();

		readonly System.Reflection.Metadata.AssemblyReference entry;

		public MetadataReader Metadata { get; }
		public AssemblyReferenceHandle Handle { get; }

		public bool IsWindowsRuntime => (entry.Flags & AssemblyFlags.WindowsRuntime) != 0;
		public bool IsRetargetable => (entry.Flags & AssemblyFlags.Retargetable) != 0;

		public string Name {
			get {
				try
				{
					return Metadata.GetString(entry.Name);
				}
				catch (BadImageFormatException)
				{
					return $"AR:{Handle}";
				}
			}
		}

		public string FullName {
			get {
				try
				{
					return entry.GetFullAssemblyName(Metadata);
				}
				catch (BadImageFormatException)
				{
					return $"fullname(AR:{Handle})";
				}
			}
		}

		public Version Version => entry.Version;
		public string Culture => Metadata.GetString(entry.Culture);
		byte[] IAssemblyReference.PublicKeyToken => GetPublicKeyToken();

		public byte[] GetPublicKeyToken()
		{
			if (entry.PublicKeyOrToken.IsNil)
				return null;
			var bytes = Metadata.GetBlobBytes(entry.PublicKeyOrToken);
			if ((entry.Flags & AssemblyFlags.PublicKey) != 0)
			{
				return sha1.ComputeHash(bytes).Skip(12).ToArray();
			}
			return bytes;
		}

		public AssemblyReference(MetadataReader metadata, AssemblyReferenceHandle handle)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Metadata = metadata;
			Handle = handle;
			entry = metadata.GetAssemblyReference(handle);
		}

		public AssemblyReference(PEFile module, AssemblyReferenceHandle handle)
		{
			if (module == null)
				throw new ArgumentNullException(nameof(module));
			if (handle.IsNil)
				throw new ArgumentNullException(nameof(handle));
			Metadata = module.Metadata;
			Handle = handle;
			entry = Metadata.GetAssemblyReference(handle);
		}

		public override string ToString()
		{
			return FullName;
		}
	}
#endif
}

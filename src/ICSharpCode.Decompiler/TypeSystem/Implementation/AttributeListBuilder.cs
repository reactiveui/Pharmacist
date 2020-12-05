﻿// Copyright (c) 2018 Daniel Grunwald
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Util;

using SRM = System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.TypeSystem.Implementation
{
	readonly struct AttributeListBuilder
	{
		readonly MetadataModule module;
		readonly List<IAttribute> attributes;

		public AttributeListBuilder(MetadataModule module)
		{
			Debug.Assert(module != null);
			this.module = module;
			this.attributes = new List<IAttribute>();
		}

		public AttributeListBuilder(MetadataModule module, int capacity)
		{
			Debug.Assert(module != null);
			this.module = module;
			this.attributes = new List<IAttribute>(capacity);
		}

		public void Add(IAttribute attr)
		{
			attributes.Add(attr);
		}

		/// <summary>
		/// Add a builtin attribute without any arguments.
		/// </summary>
		public void Add(KnownAttribute type)
		{
			// use the assemblies' cache for simple attributes
			Add(module.MakeAttribute(type));
		}

		/// <summary>
		/// Construct a builtin attribute with a single positional argument of known type.
		/// </summary>
		public void Add(KnownAttribute type, KnownTypeCode argType, object argValue)
		{
			Add(type, ImmutableArray.Create(new CustomAttributeTypedArgument<IType>(module.Compilation.FindType(argType), argValue)));
		}

		/// <summary>
		/// Construct a builtin attribute with a single positional argument of known type.
		/// </summary>
		public void Add(KnownAttribute type, TopLevelTypeName argType, object argValue)
		{
			Add(type, ImmutableArray.Create(new CustomAttributeTypedArgument<IType>(module.Compilation.FindType(argType), argValue)));
		}

		/// <summary>
		/// Construct a builtin attribute.
		/// </summary>
		public void Add(KnownAttribute type, ImmutableArray<CustomAttributeTypedArgument<IType>> fixedArguments)
		{
			Add(new DefaultAttribute(module.GetAttributeType(type), fixedArguments,
				ImmutableArray.Create<CustomAttributeNamedArgument<IType>>()));
		}

		#region MarshalAsAttribute (ConvertMarshalInfo)
		internal void AddMarshalInfo(BlobHandle marshalInfo)
		{
			if (marshalInfo.IsNil)
				return;
			var metadata = module.metadata;
			Add(ConvertMarshalInfo(metadata.GetBlobReader(marshalInfo)));
		}

		const string InteropServices = "System.Runtime.InteropServices";

		IAttribute ConvertMarshalInfo(SRM.BlobReader marshalInfo)
		{
			var b = new AttributeBuilder(module, KnownAttribute.MarshalAs);
			var unmanagedTypeType = module.Compilation.FindType(new TopLevelTypeName(InteropServices, nameof(UnmanagedType)));

			int type = marshalInfo.ReadByte();
			b.AddFixedArg(unmanagedTypeType, type);

			int size;
			switch (type)
			{
				case 0x1e: // FixedArray
					if (!marshalInfo.TryReadCompressedInteger(out size))
						size = 0;
					b.AddNamedArg("SizeConst", KnownTypeCode.Int32, size);
					if (marshalInfo.RemainingBytes > 0)
					{
						type = marshalInfo.ReadByte();
						if (type != 0x66) // None
							b.AddNamedArg("ArraySubType", unmanagedTypeType, type);
					}
					break;
				case 0x1d: // SafeArray
					if (marshalInfo.RemainingBytes > 0)
					{
						var varType = (VarEnum)marshalInfo.ReadByte();
						if (varType != VarEnum.VT_EMPTY)
						{
							var varEnumType = new TopLevelTypeName(InteropServices, nameof(VarEnum));
							b.AddNamedArg("SafeArraySubType", varEnumType, (int)varType);
						}
					}
					break;
				case 0x2a: // NATIVE_TYPE_ARRAY
					if (marshalInfo.RemainingBytes > 0)
					{
						type = marshalInfo.ReadByte();
					}
					else
					{
						type = 0x66; // Cecil uses NativeType.None as default.
					}
					if (type != 0x50)
					{ // Max
						b.AddNamedArg("ArraySubType", unmanagedTypeType, type);
					}
					var sizeParameterIndex = marshalInfo.TryReadCompressedInteger(out var value) ? value : -1;
					size = marshalInfo.TryReadCompressedInteger(out value) ? value : -1;
					var sizeParameterMultiplier = marshalInfo.TryReadCompressedInteger(out value) ? value : -1;
					if (size >= 0)
					{
						b.AddNamedArg("SizeConst", KnownTypeCode.Int32, size);
					}
					if (sizeParameterMultiplier != 0 && sizeParameterIndex >= 0)
					{
						b.AddNamedArg("SizeParamIndex", KnownTypeCode.Int16, (short)sizeParameterIndex);
					}
					break;
				case 0x2c: // CustomMarshaler
					var guidValue = marshalInfo.ReadSerializedString();
					var unmanagedType = marshalInfo.ReadSerializedString();
					var managedType = marshalInfo.ReadSerializedString();
					var cookie = marshalInfo.ReadSerializedString();
					if (managedType != null)
					{
						b.AddNamedArg("MarshalType", KnownTypeCode.String, managedType);
					}
					if (!string.IsNullOrEmpty(cookie))
					{
						b.AddNamedArg("MarshalCookie", KnownTypeCode.String, cookie);
					}
					break;
				case 0x17: // FixedSysString
					b.AddNamedArg("SizeConst", KnownTypeCode.Int32, marshalInfo.ReadCompressedInteger());
					break;
			}

			return b.Build();
		}
		#endregion

		#region Custom Attributes (ReadAttribute)
		public void Add(CustomAttributeHandleCollection attributes, SymbolKind target)
		{
			var metadata = module.metadata;
			foreach (var handle in attributes)
			{
				var attribute = metadata.GetCustomAttribute(handle);
				// Attribute types shouldn't be generic (and certainly not open), so we don't need a generic context.
				var ctor = module.ResolveMethod(attribute.Constructor, new GenericContext());
				var type = ctor.DeclaringType;
				if (IgnoreAttribute(type, target))
				{
					continue;
				}
				Add(new CustomAttribute(module, ctor, handle));
			}
		}

		bool IgnoreAttribute(IType attributeType, SymbolKind target)
		{
			if (attributeType.DeclaringType != null || attributeType.TypeParameterCount != 0)
				return false;
			switch (attributeType.Namespace)
			{
				case "System.Runtime.CompilerServices":
					switch (attributeType.Name)
					{
						case "DynamicAttribute":
							return true;
						case "NativeIntegerAttribute":
							return true;
						case "TupleElementNamesAttribute":
							return true;
						case "ExtensionAttribute":
							return true;
						case "DecimalConstantAttribute":
							return (target == SymbolKind.Field || target == SymbolKind.Parameter);
						case "IsReadOnlyAttribute":
							switch (target)
							{
								case SymbolKind.TypeDefinition:
								case SymbolKind.Parameter:
									return true;
								case SymbolKind.Method:
								case SymbolKind.Accessor:
									return true;
								case SymbolKind.ReturnType:
								case SymbolKind.Property:
								case SymbolKind.Indexer:
									return true;  // "ref readonly" is currently always active
								default:
									return false;
							}
						case "IsByRefLikeAttribute":
							return target == SymbolKind.TypeDefinition;
						case "IsUnmanagedAttribute":
							return target == SymbolKind.TypeParameter;
						case "NullableAttribute":
							return true;
						case "NullableContextAttribute":
							return (target == SymbolKind.TypeDefinition || target == SymbolKind.Method);
						default:
							return false;
					}
				case "System":
					return attributeType.Name == "ParamArrayAttribute" && target == SymbolKind.Parameter;
				default:
					return false;
			}
		}
		#endregion

		#region Security Attributes
		public void AddSecurityAttributes(DeclarativeSecurityAttributeHandleCollection securityDeclarations)
		{
			var metadata = module.metadata;
			foreach (var secDecl in securityDeclarations)
			{
				if (secDecl.IsNil)
					continue;
				try
				{
					AddSecurityAttributes(metadata.GetDeclarativeSecurityAttribute(secDecl));
				}
				catch (EnumUnderlyingTypeResolveException)
				{
					// ignore resolve errors
				}
				catch (BadImageFormatException)
				{
					// ignore invalid security declarations
				}
			}
		}

		public void AddSecurityAttributes(DeclarativeSecurityAttribute secDecl)
		{
			var securityActionType = module.Compilation.FindType(new TopLevelTypeName("System.Security.Permissions", "SecurityAction"));
			var securityAction = new CustomAttributeTypedArgument<IType>(securityActionType, (int)secDecl.Action);
			var metadata = module.metadata;
			var reader = metadata.GetBlobReader(secDecl.PermissionSet);
			if (reader.ReadByte() == '.')
			{
				// binary attribute
				var attributeCount = reader.ReadCompressedInteger();
				for (var i = 0; i < attributeCount; i++)
				{
					Add(ReadBinarySecurityAttribute(ref reader, securityAction));
				}
			}
			else
			{
				// for backward compatibility with .NET 1.0: XML-encoded attribute
				reader.Reset();
				Add(ReadXmlSecurityAttribute(ref reader, securityAction));
			}
		}

		private IAttribute ReadXmlSecurityAttribute(ref SRM.BlobReader reader, CustomAttributeTypedArgument<IType> securityAction)
		{
			var xml = reader.ReadUTF16(reader.RemainingBytes);
			var b = new AttributeBuilder(module, KnownAttribute.PermissionSet);
			b.AddFixedArg(securityAction);
			b.AddNamedArg("XML", KnownTypeCode.String, xml);
			return b.Build();
		}

		private IAttribute ReadBinarySecurityAttribute(ref SRM.BlobReader reader, CustomAttributeTypedArgument<IType> securityAction)
		{
			var attributeTypeName = reader.ReadSerializedString();
			var attributeType = module.TypeProvider.GetTypeFromSerializedName(attributeTypeName);

			reader.ReadCompressedInteger(); // ??
											// The specification seems to be incorrect here, so I'm using the logic from Cecil instead.
			var numNamed = reader.ReadCompressedInteger();

			var decoder = new Metadata.CustomAttributeDecoder<IType>(module.TypeProvider, module.metadata);
			var namedArgs = decoder.DecodeNamedArguments(ref reader, numNamed);

			return new DefaultAttribute(
				attributeType,
				fixedArguments: ImmutableArray.Create(securityAction),
				namedArguments: namedArgs);
		}
		#endregion

		public IAttribute[] Build()
		{
			if (attributes.Count == 0)
				return Empty<IAttribute>.Array;
			else
				return attributes.ToArray();
		}
	}

	struct AttributeBuilder
	{
		readonly ICompilation compilation;
		readonly IType attributeType;
		ImmutableArray<CustomAttributeTypedArgument<IType>>.Builder fixedArgs;
		ImmutableArray<CustomAttributeNamedArgument<IType>>.Builder namedArgs;

		public AttributeBuilder(MetadataModule module, KnownAttribute attributeType)
			: this(module, module.GetAttributeType(attributeType))
		{
		}

		public AttributeBuilder(MetadataModule module, IType attributeType)
		{
			this.compilation = module.Compilation;
			this.attributeType = attributeType;
			this.fixedArgs = ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<IType>>();
			this.namedArgs = ImmutableArray.CreateBuilder<CustomAttributeNamedArgument<IType>>();
		}

		public void AddFixedArg(CustomAttributeTypedArgument<IType> arg)
		{
			fixedArgs.Add(arg);
		}

		public void AddFixedArg(KnownTypeCode type, object value)
		{
			AddFixedArg(compilation.FindType(type), value);
		}

		public void AddFixedArg(TopLevelTypeName type, object value)
		{
			AddFixedArg(compilation.FindType(type), value);
		}

		public void AddFixedArg(IType type, object value)
		{
			fixedArgs.Add(new CustomAttributeTypedArgument<IType>(type, value));
		}

		public void AddNamedArg(string name, KnownTypeCode type, object value)
		{
			AddNamedArg(name, compilation.FindType(type), value);
		}

		public void AddNamedArg(string name, TopLevelTypeName type, object value)
		{
			AddNamedArg(name, compilation.FindType(type), value);
		}

		public void AddNamedArg(string name, IType type, object value)
		{
			CustomAttributeNamedArgumentKind kind;
			if (attributeType.GetFields(f => f.Name == name, GetMemberOptions.ReturnMemberDefinitions).Any())
				kind = CustomAttributeNamedArgumentKind.Field;
			else
				kind = CustomAttributeNamedArgumentKind.Property;
			namedArgs.Add(new CustomAttributeNamedArgument<IType>(name, kind, type, value));
		}

		public IAttribute Build()
		{
			return new DefaultAttribute(attributeType, fixedArgs.ToImmutable(), namedArgs.ToImmutable());
		}
	}
}

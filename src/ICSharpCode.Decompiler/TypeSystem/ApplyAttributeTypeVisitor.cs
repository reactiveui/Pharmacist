// Copyright (c) 2018 Daniel Grunwald
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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.Decompiler.Util;

using SRM = System.Reflection.Metadata;

namespace ICSharpCode.Decompiler.TypeSystem
{
    /// <summary>
    /// Introduces 'dynamic' and tuple types based on attribute values.
    /// </summary>
    sealed class ApplyAttributeTypeVisitor : TypeVisitor
    {
        public static IType ApplyAttributesToType(
            IType inputType,
            ICompilation compilation,
            SRM.CustomAttributeHandleCollection? attributes,
            SRM.MetadataReader metadata,
            Nullability nullableContext,
            bool typeChildrenOnly = false,
            bool isSignatureReturnType = false)
        {
            var hasDynamicAttribute = false;
            bool[] dynamicAttributeData = null;
            var hasNativeIntegersAttribute = false;
            bool[] nativeIntegersAttributeData = null;
            string[] tupleElementNames = null;
            Nullability nullability;
            Nullability[] nullableAttributeData = null;
            nullability = nullableContext;
            if (attributes != null)
            {
                foreach (var attrHandle in attributes.Value)
                {
                    var attr = metadata.GetCustomAttribute(attrHandle);
                    var attrType = attr.GetAttributeType(metadata);
                    if (attrType.IsKnownType(metadata, KnownAttribute.Dynamic))
                    {
                        hasDynamicAttribute = true;
                        var ctor = attr.DecodeValue(Metadata.MetadataExtensions.minimalCorlibTypeProvider);
                        if (ctor.FixedArguments.Length == 1)
                        {
                            var arg = ctor.FixedArguments[0];
                            if (arg.Value is ImmutableArray<SRM.CustomAttributeTypedArgument<IType>> values
                                && values.All(v => v.Value is bool))
                            {
                                dynamicAttributeData = values.SelectArray(v => (bool)v.Value);
                            }
                        }
                    }
                    else if (attrType.IsKnownType(metadata, KnownAttribute.NativeInteger))
                    {
                        hasNativeIntegersAttribute = true;
                        var ctor = attr.DecodeValue(Metadata.MetadataExtensions.minimalCorlibTypeProvider);
                        if (ctor.FixedArguments.Length == 1)
                        {
                            var arg = ctor.FixedArguments[0];
                            if (arg.Value is ImmutableArray<SRM.CustomAttributeTypedArgument<IType>> values
                                && values.All(v => v.Value is bool))
                            {
                                nativeIntegersAttributeData = values.SelectArray(v => (bool)v.Value);
                            }
                        }
                    }
                    else if (attrType.IsKnownType(metadata, KnownAttribute.TupleElementNames))
                    {
                        var ctor = attr.DecodeValue(Metadata.MetadataExtensions.minimalCorlibTypeProvider);
                        if (ctor.FixedArguments.Length == 1)
                        {
                            var arg = ctor.FixedArguments[0];
                            if (arg.Value is ImmutableArray<SRM.CustomAttributeTypedArgument<IType>> values
                                && values.All(v => v.Value is string || v.Value == null))
                            {
                                tupleElementNames = values.SelectArray(v => (string)v.Value);
                            }
                        }
                    }
                    else if (attrType.IsKnownType(metadata, KnownAttribute.Nullable))
                    {
                        var ctor = attr.DecodeValue(Metadata.MetadataExtensions.minimalCorlibTypeProvider);
                        if (ctor.FixedArguments.Length == 1)
                        {
                            var arg = ctor.FixedArguments[0];
                            if (arg.Value is ImmutableArray<SRM.CustomAttributeTypedArgument<IType>> values
                                && values.All(v => v.Value is byte b && b <= 2))
                            {
                                nullableAttributeData = values.SelectArray(v => (Nullability)(byte)v.Value);
                            }
                            else if (arg.Value is byte b && b <= 2)
                            {
                                nullability = (Nullability)b;
                            }
                        }
                    }
                }
            }

            if (hasDynamicAttribute || hasNativeIntegersAttribute || nullability != Nullability.Oblivious || nullableAttributeData != null)
            {
                var visitor = new ApplyAttributeTypeVisitor(
                    compilation, hasDynamicAttribute, dynamicAttributeData,
                    hasNativeIntegersAttribute, nativeIntegersAttributeData,
                    tupleElementNames,
                    nullability, nullableAttributeData
                );
                if (isSignatureReturnType && hasDynamicAttribute
                    && inputType.SkipModifiers().Kind == TypeKind.ByReference
                    && attributes.Value.HasKnownAttribute(metadata, KnownAttribute.IsReadOnly))
                {
                    // crazy special case: `ref readonly` return takes one dynamic index more than
                    // a non-readonly `ref` return.
                    visitor.dynamicTypeIndex++;
                }
                if (typeChildrenOnly)
                {
                    return inputType.VisitChildren(visitor);
                }
                else
                {
                    return inputType.AcceptVisitor(visitor);
                }
            }
            else
            {
                return inputType;
            }
        }

        readonly ICompilation compilation;
        readonly bool hasDynamicAttribute;
        readonly bool[] dynamicAttributeData;
        readonly bool hasNativeIntegersAttribute;
        readonly bool[] nativeIntegersAttributeData;
        readonly string[] tupleElementNames;
        readonly Nullability defaultNullability;
        readonly Nullability[] nullableAttributeData;
        int dynamicTypeIndex = 0;
        int tupleTypeIndex = 0;
        int nullabilityTypeIndex = 0;
        int nativeIntTypeIndex = 0;

        private ApplyAttributeTypeVisitor(ICompilation compilation,
            bool hasDynamicAttribute, bool[] dynamicAttributeData,
            bool hasNativeIntegersAttribute, bool[] nativeIntegersAttributeData,
            string[] tupleElementNames,
            Nullability defaultNullability, Nullability[] nullableAttributeData)
        {
            this.compilation = compilation ?? throw new ArgumentNullException(nameof(compilation));
            this.hasDynamicAttribute = hasDynamicAttribute;
            this.dynamicAttributeData = dynamicAttributeData;
            this.hasNativeIntegersAttribute = hasNativeIntegersAttribute;
            this.nativeIntegersAttributeData = nativeIntegersAttributeData;
            this.tupleElementNames = tupleElementNames;
            this.defaultNullability = defaultNullability;
            this.nullableAttributeData = nullableAttributeData;
        }

        public override IType VisitPointerType(PointerType type)
        {
            dynamicTypeIndex++;
            return base.VisitPointerType(type);
        }

        Nullability GetNullability()
        {
            if (nullabilityTypeIndex < nullableAttributeData?.Length)
                return nullableAttributeData[nullabilityTypeIndex++];
            else
                return defaultNullability;
        }

        void ExpectDummyNullabilityForGenericValueType()
        {
            var n = GetNullability();
            Debug.Assert(n == Nullability.Oblivious);
        }

        public override IType VisitArrayType(ArrayType type)
        {
            var nullability = GetNullability();
            dynamicTypeIndex++;
            return base.VisitArrayType(type).ChangeNullability(nullability);
        }

        public override IType VisitByReferenceType(ByReferenceType type)
        {
            dynamicTypeIndex++;
            return base.VisitByReferenceType(type);
        }

        public override IType VisitParameterizedType(ParameterizedType type)
        {
            var useTupleTypes = true;
            if (useTupleTypes && TupleType.IsTupleCompatible(type, out var tupleCardinality))
            {
                if (tupleCardinality > 1)
                {
                    var valueTupleAssembly = type.GetDefinition()?.ParentModule;
                    ImmutableArray<string> elementNames = default;
                    if (tupleElementNames != null && tupleTypeIndex < tupleElementNames.Length)
                    {
                        var extractedValues = new string[tupleCardinality];
                        Array.Copy(tupleElementNames, tupleTypeIndex, extractedValues, 0,
                            Math.Min(tupleCardinality, tupleElementNames.Length - tupleTypeIndex));
                        elementNames = ImmutableArray.CreateRange(extractedValues);
                    }
                    tupleTypeIndex += tupleCardinality;
                    ExpectDummyNullabilityForGenericValueType();
                    var elementTypes = ImmutableArray.CreateBuilder<IType>(tupleCardinality);
                    do
                    {
                        var normalArgCount = Math.Min(type.TypeArguments.Count, TupleType.RestPosition - 1);
                        for (var i = 0; i < normalArgCount; i++)
                        {
                            dynamicTypeIndex++;
                            elementTypes.Add(type.TypeArguments[i].AcceptVisitor(this));
                        }
                        if (type.TypeArguments.Count == TupleType.RestPosition)
                        {
                            type = type.TypeArguments.Last() as ParameterizedType;
                            ExpectDummyNullabilityForGenericValueType();
                            dynamicTypeIndex++;
                            if (type != null && TupleType.IsTupleCompatible(type, out var nestedCardinality))
                            {
                                tupleTypeIndex += nestedCardinality;
                            }
                            else
                            {
                                Debug.Fail("TRest should be another value tuple");
                                type = null;
                            }
                        }
                        else
                        {
                            type = null;
                        }
                    } while (type != null);
                    Debug.Assert(elementTypes.Count == tupleCardinality);
                    return new TupleType(
                        compilation,
                        elementTypes.MoveToImmutable(),
                        elementNames,
                        valueTupleAssembly
                    );
                }
                else
                {
                    // C# doesn't have syntax for tuples of cardinality <= 1
                    tupleTypeIndex += tupleCardinality;
                }
            }
            // Visit generic type and type arguments.
            // Like base implementation, except that it increments dynamicTypeIndex.
            var genericType = type.GenericType.AcceptVisitor(this);
            if (genericType.IsReferenceType != true && !genericType.IsKnownType(KnownTypeCode.NullableOfT))
            {
                ExpectDummyNullabilityForGenericValueType();
            }
            var changed = type.GenericType != genericType;
            var arguments = new IType[type.TypeArguments.Count];
            for (var i = 0; i < type.TypeArguments.Count; i++)
            {
                dynamicTypeIndex++;
                arguments[i] = type.TypeArguments[i].AcceptVisitor(this);
                changed = changed || arguments[i] != type.TypeArguments[i];
            }
            if (!changed)
                return type;
            return new ParameterizedType(genericType, arguments);
        }

        public override IType VisitFunctionPointerType(FunctionPointerType type)
        {
            dynamicTypeIndex++;
            if (type.ReturnIsRefReadOnly)
            {
                dynamicTypeIndex++;
            }
            var returnType = type.ReturnType.AcceptVisitor(this);
            var changed = type.ReturnType != returnType;
            var parameters = new IType[type.ParameterTypes.Length];
            for (var i = 0; i < parameters.Length; i++)
            {
                dynamicTypeIndex += type.ParameterReferenceKinds[i] switch
                {
                    ReferenceKind.None => 1,
                    ReferenceKind.Ref => 1,
                    ReferenceKind.Out => 2, // in/out also count the modreq
                    ReferenceKind.In => 2,
                    _ => throw new NotSupportedException()
                };
                parameters[i] = type.ParameterTypes[i].AcceptVisitor(this);
                changed = changed || parameters[i] != type.ParameterTypes[i];
            }
            if (!changed)
                return type;
            return type.WithSignature(returnType, parameters.ToImmutableArray());
        }

        public override IType VisitTypeDefinition(ITypeDefinition type)
        {
            IType newType = type;
            var ktc = type.KnownTypeCode;
            if (ktc == KnownTypeCode.Object && hasDynamicAttribute)
            {
                if (dynamicAttributeData == null || dynamicTypeIndex >= dynamicAttributeData.Length)
                    newType = SpecialType.Dynamic;
                else if (dynamicAttributeData[dynamicTypeIndex])
                    newType = SpecialType.Dynamic;
            }
            else if ((ktc == KnownTypeCode.IntPtr || ktc == KnownTypeCode.UIntPtr) && hasNativeIntegersAttribute)
            {
                // native integers use the same indexing logic as 'dynamic'
                if (nativeIntegersAttributeData == null || nativeIntTypeIndex >= nativeIntegersAttributeData.Length)
                    newType = (ktc == KnownTypeCode.IntPtr ? SpecialType.NInt : SpecialType.NUInt);
                else if (nativeIntegersAttributeData[nativeIntTypeIndex])
                    newType = (ktc == KnownTypeCode.IntPtr ? SpecialType.NInt : SpecialType.NUInt);
                nativeIntTypeIndex++;
            }
            if (type.IsReferenceType == true)
            {
                var nullability = GetNullability();
                return newType.ChangeNullability(nullability);
            }
            else
            {
                return newType;
            }
        }

        public override IType VisitOtherType(IType type)
        {
            type = base.VisitOtherType(type);
            if (type.Kind == TypeKind.Unknown && type.IsReferenceType == true)
            {
                var nullability = GetNullability();
                type = type.ChangeNullability(nullability);
            }
            return type;
        }

        public override IType VisitTypeParameter(ITypeParameter type)
        {
            var nullability = GetNullability();
            return type.ChangeNullability(nullability);
        }
    }
}

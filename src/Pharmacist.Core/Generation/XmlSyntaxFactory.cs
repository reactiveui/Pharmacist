// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using ICSharpCode.Decompiler.TypeSystem;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Pharmacist.Core.Generation
{
    /// <summary>
    /// This class was originally from StyleCop. https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Helpers/XmlSyntaxFactory.cs
    /// All credit goes to the StyleCop team.
    /// </summary>
    internal static class XmlSyntaxFactory
    {
        static XmlSyntaxFactory()
        {
            // Make sure the newline is included. Otherwise the comment and the method will be on the same line.
            InheritdocSyntax = SyntaxFactory.ParseLeadingTrivia(@"/// <inheritdoc />" + Environment.NewLine);
        }

        /// <summary>
        /// Gets a inheritdoc leading trivia comment.
        /// </summary>
        public static SyntaxTriviaList InheritdocSyntax { get; }

        public static SyntaxTriviaList GenerateSummaryComment(string summaryText, string parameterFormat, IMethod entity)
        {
            var parameters = entity.Parameters.Select(x => (x.Name, string.Format(CultureInfo.InvariantCulture, parameterFormat, x.Name)));

            return GenerateSummaryComment(summaryText, parameters);
        }

        public static SyntaxTriviaList GenerateSummarySeeAlsoComment(string summaryText, string seeAlsoText)
        {
            var text = string.Format(CultureInfo.InvariantCulture, summaryText, "<see cref=\"" + seeAlsoText.Replace("<", "{").Replace(">", "}") + "\" />");
            string template = "/// <summary>" + Environment.NewLine +
                              $"/// {text}" + Environment.NewLine +
                              "/// </summary>" + Environment.NewLine;

            return SyntaxFactory.ParseLeadingTrivia(template);
        }

        public static SyntaxTriviaList GenerateSummarySeeAlsoComment(string summaryText, string seeAlsoText, params (string paramName, string paramText)[] parameters)
        {
            var text = string.Format(CultureInfo.InvariantCulture, summaryText, "<see cref=\"" + seeAlsoText.Replace("<", "{").Replace(">", "}") + "\" />");
            var sb = new StringBuilder("/// <summary>")
                .AppendLine()
                .Append("/// ").AppendLine(text)
                .AppendLine("/// </summary>");

            foreach (var parameter in parameters)
            {
                sb.AppendLine($"/// <param name=\"{parameter.paramName}\">{parameter.paramText}</param>");
            }

            return SyntaxFactory.ParseLeadingTrivia(sb.ToString());
        }

        public static SyntaxTriviaList GenerateSummaryComment(string summaryText)
        {
            string template = "/// <summary>" + Environment.NewLine +
                              $"/// {summaryText}" + Environment.NewLine +
                              "/// </summary>" + Environment.NewLine;

            return SyntaxFactory.ParseLeadingTrivia(template);
        }

        public static SyntaxTriviaList GenerateSummaryComment(string summaryText, string returnValueText)
        {
            string template = "/// <summary>" + Environment.NewLine +
                              $"/// {summaryText}" + Environment.NewLine +
                              "/// </summary>" + Environment.NewLine +
                              $"/// <returns>{returnValueText}///<returns>" + Environment.NewLine;

            return SyntaxFactory.ParseLeadingTrivia(template);
        }

        public static SyntaxTriviaList GenerateSummaryComment(string summaryText, IEnumerable<(string paramName, string paramText)> parameters)
        {
            var sb = new StringBuilder("/// <summary>")
                .AppendLine()
                .Append("/// ").AppendLine(summaryText)
                .AppendLine("/// </summary>");

            foreach (var parameter in parameters)
            {
                sb.AppendLine($"/// <param name=\"{parameter.paramName}\">{parameter.paramText}</param>");
            }

            return SyntaxFactory.ParseLeadingTrivia(sb.ToString());
        }

        public static SyntaxTriviaList GenerateSummaryComment(string summaryText, IEnumerable<(string paramName, string paramText)> parameters, string returnValueText)
        {
            var sb = new StringBuilder("/// <summary>")
                .AppendLine()
                .Append("/// ").AppendLine(summaryText)
                .AppendLine("/// </summary>");

            foreach (var parameter in parameters)
            {
                sb.AppendLine($"/// <param name=\"{parameter.paramName}\">{parameter.paramText}</param>");
            }

            sb.Append("/// <returns>").Append(returnValueText).AppendLine("</returns>");
            return SyntaxFactory.ParseLeadingTrivia(sb.ToString());
        }

        public static string ConvertToDocument(this IType currentType)
        {
            return currentType.GenerateFullGenericName().Replace("<", "{").Replace(">", "}");
        }

        public static string ConvertToDocument(this IMethod method)
        {
            var stringBuilder = new StringBuilder(method.DeclaringType.ConvertToDocument() + "." + method.Name).Append("(");

            for (int i = 0; i < method.Parameters.Count; ++i)
            {
                var parameter = method.Parameters[i];

                if (i != 0)
                {
                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(parameter.Type.ConvertToDocument());
            }

            stringBuilder.Append(")");

            return stringBuilder.ToString();
        }

        public static string ConvertToDocument(this IEvent eventDetails)
        {
            return eventDetails.DeclaringType.ConvertToDocument() + "." + eventDetails.Name;
        }
    }
}

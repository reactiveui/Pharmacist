﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Pharmacist.Core.Generation;

using PublicApiGenerator;

using Shouldly;

using Splat;

using Xunit;

namespace Pharmacist.Tests.API
{
    /// <summary>
    /// Tests to make sure that the API matches the approved ones.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ApiApprovalTests
    {
        private static readonly Regex _removeCoverletSectionRegex = new Regex(@"^namespace Coverlet\.Core\.Instrumentation\.Tracker.*?^}", RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Tests to make sure the splat project is approved.
        /// </summary>
        [Fact]
        public void EventBuilderProject()
        {
            CheckApproval(typeof(XmlSyntaxFactory).Assembly);
        }

        private static void CheckApproval(Assembly assembly, [CallerMemberName]string memberName = null, [CallerFilePath]string filePath = null)
        {
            var targetFrameworkName = Assembly.GetExecutingAssembly().GetTargetFrameworkName();

            var sourceDirectory = Path.GetDirectoryName(filePath);

            var approvedFileName = Path.Combine(sourceDirectory, $"ApiApprovalTests.{memberName}.{targetFrameworkName}.approved.txt");
            var receivedFileName = Path.Combine(sourceDirectory, $"ApiApprovalTests.{memberName}.{targetFrameworkName}.received.txt");

            if (!File.Exists(approvedFileName))
            {
                File.Create(approvedFileName).Close();
            }

            if (!File.Exists(receivedFileName))
            {
                File.Create(receivedFileName).Close();
            }

            var approvedPublicApi = File.ReadAllText(approvedFileName);

            var receivedPublicApi = Filter(ApiGenerator.GeneratePublicApi(assembly));

            if (!string.Equals(receivedPublicApi, approvedPublicApi, StringComparison.InvariantCulture))
            {
                File.WriteAllText(receivedFileName, receivedPublicApi);
                ShouldlyConfiguration.DiffTools.GetDiffTool().Open(receivedFileName, approvedFileName, true);
            }

            Assert.Equal(approvedPublicApi, receivedPublicApi);
        }

        private static string Filter(string text)
        {
            text = _removeCoverletSectionRegex.Replace(text, string.Empty);
            return string.Join(Environment.NewLine, text.Split(
                new[]
                {
                    Environment.NewLine
                }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(l =>
                    !l.StartsWith("[assembly: AssemblyVersion(", StringComparison.InvariantCulture) &&
                    !l.StartsWith("[assembly: AssemblyFileVersion(", StringComparison.InvariantCulture) &&
                    !l.StartsWith("[assembly: AssemblyInformationalVersion(", StringComparison.InvariantCulture) &&
                    !string.IsNullOrWhiteSpace(l)));
        }
    }
}

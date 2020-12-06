// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Pharmacist.Core
{
    internal static class TemplateManager
    {
        /// <summary>
        /// Gets the template for the header of the file.
        /// </summary>
        public const string HeaderTemplate = "Pharmacist.Core.Templates.HeaderTemplate.txt";

        public static async Task<string> GetTemplateAsync(string templateName)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var manifestStream = assembly.GetManifestResourceStream(templateName);

            if (manifestStream is null)
            {
                throw new InvalidOperationException("Could not find the valid template");
            }

            using var streamReader = new StreamReader(manifestStream, Encoding.UTF8);
            return await streamReader.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}

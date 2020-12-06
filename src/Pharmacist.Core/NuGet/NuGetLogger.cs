// Copyright (c) 2019-2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using NuGet.Common;

using Splat;

using ILogger = NuGet.Common.ILogger;
using LogLevel = NuGet.Common.LogLevel;

namespace Pharmacist.Core.NuGet
{
    /// <summary>
    /// A logger provider for the NuGet clients API.
    /// </summary>
    internal class NuGetLogger : ILogger, IEnableLogger
    {
        /// <inheritdoc />
        public void Log(LogLevel level, string data)
        {
            switch (level)
            {
                case LogLevel.Warning:
                    this.Log().Warn(data);
                    break;
                case LogLevel.Error:
                    this.Log().Error(data);
                    break;
                case LogLevel.Information:
                    this.Log().Info(data);
                    break;
                case LogLevel.Debug:
                    this.Log().Debug(data);
                    break;
                case LogLevel.Verbose:
                    this.Log().Info(data);
                    break;
                case LogLevel.Minimal:
                    this.Log().Info(data);
                    break;
                default:
                    this.Log().Info(data);
                    break;
            }
        }

        /// <inheritdoc />
        public void Log(ILogMessage message)
        {
            Log(message.Level, message.Message);
        }

        /// <inheritdoc />
        public Task LogAsync(LogLevel level, string data)
        {
            Log(level, data);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task LogAsync(ILogMessage message)
        {
            Log(message);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void LogDebug(string data)
        {
            this.Log().Debug(data);
        }

        /// <inheritdoc />
        public void LogError(string data)
        {
            this.Log().Error(data);
        }

        /// <inheritdoc />
        public void LogInformation(string data)
        {
            this.Log().Info(data);
        }

        /// <inheritdoc />
        public void LogInformationSummary(string data)
        {
            this.Log().Info(data);
        }

        /// <inheritdoc />
        public void LogMinimal(string data)
        {
            this.Log().Info(data);
        }

        /// <inheritdoc />
        public void LogVerbose(string data)
        {
            this.Log().Info(data);
        }

        /// <inheritdoc />
        public void LogWarning(string data)
        {
            this.Log().Warn(data);
        }
    }
}

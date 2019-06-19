﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

using Pharmacist.Core.NuGet;

using Shouldly;

using Xunit;

namespace Pharmacist.Tests
{
    /// <summary>
    /// Tests to check the NuGetPackageHelper class.
    /// </summary>
    public class NuGetPackageHelperTests
    {
        private static readonly string[] ExpectedTizenFiles =
        {
            "ElmSharp.dll",
            "ElmSharp.Wearable.dll",
            "Tizen.Account.AccountManager.dll",
            "Tizen.Account.FidoClient.dll",
            "Tizen.Account.OAuth2.dll",
            "Tizen.Account.SyncManager.dll",
            "Tizen.Applications.Alarm.dll",
            "Tizen.Applications.AttachPanel.dll",
            "Tizen.Applications.Badge.dll",
            "Tizen.Applications.Common.dll",
            "Tizen.Applications.DataControl.dll",
            "Tizen.Applications.MessagePort.dll",
            "Tizen.Applications.Notification.dll",
            "Tizen.Applications.NotificationEventListener.dll",
            "Tizen.Applications.PackageManager.dll",
            "Tizen.Applications.Preference.dll",
            "Tizen.Applications.RemoteView.dll",
            "Tizen.Applications.Service.dll",
            "Tizen.Applications.Shortcut.dll",
            "Tizen.Applications.ToastMessage.dll",
            "Tizen.Applications.UI.dll",
            "Tizen.Applications.WatchApplication.dll",
            "Tizen.Applications.WidgetApplication.dll",
            "Tizen.Applications.WidgetControl.dll",
            "Tizen.Content.Download.dll",
            "Tizen.Content.MediaContent.dll",
            "Tizen.Content.MimeType.dll",
            "Tizen.Context.dll",
            "Tizen.dll",
            "Tizen.Location.dll",
            "Tizen.Location.Geofence.dll",
            "Tizen.Log.dll",
            "Tizen.Maps.dll",
            "Tizen.Messaging.dll",
            "Tizen.Messaging.Push.dll",
            "Tizen.Multimedia.AudioIO.dll",
            "Tizen.Multimedia.Camera.dll",
            "Tizen.Multimedia.dll",
            "Tizen.Multimedia.MediaCodec.dll",
            "Tizen.Multimedia.MediaPlayer.dll",
            "Tizen.Multimedia.Metadata.dll",
            "Tizen.Multimedia.Radio.dll",
            "Tizen.Multimedia.Recorder.dll",
            "Tizen.Multimedia.Remoting.dll",
            "Tizen.Multimedia.StreamRecorder.dll",
            "Tizen.Multimedia.Util.dll",
            "Tizen.Multimedia.Vision.dll",
            "Tizen.Network.Bluetooth.dll",
            "Tizen.Network.Connection.dll",
            "Tizen.Network.IoTConnectivity.dll",
            "Tizen.Network.Nfc.dll",
            "Tizen.Network.Nsd.dll",
            "Tizen.Network.Smartcard.dll",
            "Tizen.Network.WiFi.dll",
            "Tizen.Network.WiFiDirect.dll",
            "Tizen.NUI.dll",
            "Tizen.PhonenumberUtils.dll",
            "Tizen.Pims.Calendar.dll",
            "Tizen.Pims.Contacts.dll",
            "Tizen.Security.dll",
            "Tizen.Security.PrivacyPrivilegeManager.dll",
            "Tizen.Security.SecureRepository.dll",
            "Tizen.Security.TEEC.dll",
            "Tizen.Sensor.dll",
            "Tizen.System.dll",
            "Tizen.System.Feedback.dll",
            "Tizen.System.Information.dll",
            "Tizen.System.MediaKey.dll",
            "Tizen.System.PlatformConfig.dll",
            "Tizen.System.Storage.dll",
            "Tizen.System.SystemSettings.dll",
            "Tizen.System.Usb.dll",
            "Tizen.Telephony.dll",
            "Tizen.Tracer.dll",
            "Tizen.Uix.InputMethod.dll",
            "Tizen.Uix.InputMethodManager.dll",
            "Tizen.Uix.Stt.dll",
            "Tizen.Uix.SttEngine.dll",
            "Tizen.Uix.Tts.dll",
            "Tizen.Uix.TtsEngine.dll",
            "Tizen.Uix.VoiceControl.dll",
            "Tizen.WebView.dll",
        };

        private static readonly string[] ExpectedTizenDirectories =
        {
            "NETStandard.Library" + Path.DirectorySeparatorChar + "2.0.0",
            "Tizen.NET.API4" + Path.DirectorySeparatorChar + "4.0.1.14152"
        };

        /// <summary>
        /// Check to make sure that the tizen packages produce the correct files.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task CanGetTizenPackage()
        {
            await GetAndCheckTizenPackage().ConfigureAwait(false);
        }

        /// <summary>
        /// Checks that making multiple attempts against the NuGet system will still give valid results.
        /// </summary>
        /// <returns>A task to monitor the progress.</returns>
        [Fact]
        public async Task MultipleTizenAttemptsWork()
        {
            await GetAndCheckTizenPackage().ConfigureAwait(false);
            await GetAndCheckTizenPackage().ConfigureAwait(false);
        }

        [Fact]
        public async Task CanGetNuGetProtocolAndDependencies()
        {
            var package = new[] { new PackageIdentity("NuGet.Protocol", new NuGetVersion("5.0.0")) };
            var frameworks = new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };

            var result = (await NuGetPackageHelper
                              .DownloadPackageFilesAndFolder(package, frameworks, packageOutputDirectory: TestUtilities.PackageDirectory)
                              .ConfigureAwait(false)).ToList();

            result.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task CanGetNetCoreApp20()
        {
            var package = new[] { new PackageIdentity("Microsoft.NETCore.App", new NuGetVersion("2.0.0")) };
            var frameworks = new[] { FrameworkConstants.CommonFrameworks.NetCoreApp20 };

            var result = (await NuGetPackageHelper
                              .DownloadPackageFilesAndFolder(package, frameworks, packageOutputDirectory: TestUtilities.PackageDirectory)
                              .ConfigureAwait(false)).ToList();

            result.ShouldNotBeEmpty();

            result.SelectMany(x => x.files).Where(x => x.EndsWith(".dll")).ShouldNotBeEmpty();
        }

        [Fact]
        public async Task CanGetNetFramework()
        {
            var package = new[] { new PackageIdentity("Microsoft.NETFramework.ReferenceAssemblies.net461", new NuGetVersion("1.0.0-preview.2")) };

            var frameworks = new[] { FrameworkConstants.CommonFrameworks.Net461 };

            var result = (await NuGetPackageHelper
                              .DownloadPackageFilesAndFolder(package, frameworks, packageOutputDirectory: TestUtilities.PackageDirectory)
                              .ConfigureAwait(false)).ToList();

            result.ShouldNotBeEmpty();

            result.SelectMany(x => x.files).Where(x => x.EndsWith(".dll")).ShouldNotBeEmpty();
        }

        private static async Task GetAndCheckTizenPackage()
        {
            var package = new[] { new PackageIdentity("Tizen.NET.API4", new NuGetVersion("4.0.1.14152")) };
            var frameworks = new[] { FrameworkConstants.CommonFrameworks.NetStandard20 };

            var result = (await NuGetPackageHelper
                              .DownloadPackageFilesAndFolder(package, frameworks, packageOutputDirectory: TestUtilities.PackageDirectory)
                              .ConfigureAwait(false)).ToList();

            var actualFiles = result.SelectMany(x => x.files).Where(x => x.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)).ToList();
            var actualDirectories = result.Select(x => x.folder).ToList();
            Assert.True(actualFiles.All(File.Exists));
            Assert.True(actualDirectories.All(Directory.Exists));

            var actualFileNames = actualFiles.Select(Path.GetFileName).ToList();
            var actualDirectoryNames = actualDirectories.Select(x => x.Replace(TestUtilities.PackageDirectory + Path.DirectorySeparatorChar, string.Empty)).ToList();
            ExpectedTizenFiles.ShouldHaveSameContents(actualFileNames);
            ExpectedTizenDirectories.ShouldHaveSameContents(actualDirectoryNames);
        }
    }
}

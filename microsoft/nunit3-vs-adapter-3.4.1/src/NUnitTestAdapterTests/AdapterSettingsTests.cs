﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using NUnit.Framework;

namespace NUnit.VisualStudio.TestAdapter.Tests
{
    public class AdapterSettingsTests
    {
        private AdapterSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new AdapterSettings();
        }

        [Test]
        public void NullContextThrowsException()
        {
            Assert.That(() => _settings.Load((IDiscoveryContext)null), Throws.ArgumentNullException);
        }

        [Test]
        public void NullStringThrowsException()
        {
            Assert.That(() => _settings.Load((string)null), Throws.ArgumentNullException);
        }

        [Test]
        public void EmptyStringThrowsException()
        {
            Assert.That(() => _settings.Load(string.Empty), Throws.ArgumentException);
        }

        [Test]
        public void DefaultSettings()
        {
            _settings.Load("<RunSettings/>");
            Assert.That(_settings.MaxCpuCount, Is.EqualTo(-1));
            Assert.Null(_settings.ResultsDirectory);
            Assert.Null(_settings.TargetFrameworkVersion);
            Assert.Null(_settings.TargetPlatform);
            Assert.Null(_settings.TestAdapterPaths);
            Assert.IsEmpty(_settings.TestProperties);
            Assert.Null(_settings.InternalTraceLevel);
            Assert.Null(_settings.WorkDirectory);
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(-1));
            Assert.That(_settings.DefaultTimeout, Is.EqualTo(0));
            Assert.That(_settings.Verbosity, Is.EqualTo(0));
            Assert.False(_settings.ShadowCopyFiles);
            Assert.False(_settings.UseVsKeepEngineRunning);
            Assert.Null(_settings.BasePath);
            Assert.Null(_settings.PrivateBinPath);
            Assert.That(_settings.RandomSeed, Is.EqualTo(-1));
        }

        [Test]
        public void ResultsDirectorySetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><ResultsDirectory>./myresults</ResultsDirectory></RunConfiguration></RunSettings>");
            Assert.That(_settings.ResultsDirectory, Is.EqualTo("./myresults"));
        }

        [Test]
        public void MaxCpuCountSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><MaxCpuCount>42</MaxCpuCount></RunConfiguration></RunSettings>");
            Assert.That(_settings.MaxCpuCount, Is.EqualTo(42));
        }

        [Test]
        public void TargetFrameworkVersionSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><TargetFrameworkVersion>Framework45</TargetFrameworkVersion></RunConfiguration></RunSettings>");
            Assert.That(_settings.TargetFrameworkVersion, Is.EqualTo("Framework45"));
        }

        [Test]
        public void TargetPlatformSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><TargetPlatform>x86</TargetPlatform></RunConfiguration></RunSettings>");
            Assert.That(_settings.TargetPlatform, Is.EqualTo("x86"));
        }

        [Test]
        public void TestAdapterPathsSetting()
        {
            _settings.Load("<RunSettings><RunConfiguration><TestAdapterPaths>/first/path;/second/path</TestAdapterPaths></RunConfiguration></RunSettings>");
            Assert.That(_settings.TestAdapterPaths, Is.EqualTo("/first/path;/second/path"));
        }

        [Test]
        public void TestRunParameterSettings()
        {
            _settings.Load("<RunSettings><TestRunParameters><Parameter name='Answer' value='42'/><Parameter name='Question' value='Why?'/></TestRunParameters></RunSettings>");
            Assert.That(_settings.TestProperties.Count, Is.EqualTo(2));
            Assert.That(_settings.TestProperties["Answer"], Is.EqualTo("42"));
            Assert.That(_settings.TestProperties["Question"], Is.EqualTo("Why?"));
        }

        [Test]
        public void InternalTraceLevel()
        {
            _settings.Load("<RunSettings><NUnit><InternalTraceLevel>Debug</InternalTraceLevel></NUnit></RunSettings>");
            Assert.That(_settings.InternalTraceLevel, Is.EqualTo("Debug"));
        }

        [Test]
        public void WorkDirectorySetting()
        {
            _settings.Load("<RunSettings><NUnit><WorkDirectory>/my/work/dir</WorkDirectory></NUnit></RunSettings>");
            Assert.That(_settings.WorkDirectory, Is.EqualTo("/my/work/dir"));
        }

        [Test]
        public void NumberOfTestWorkersSetting()
        {
            _settings.Load("<RunSettings><NUnit><NumberOfTestWorkers>12</NumberOfTestWorkers></NUnit></RunSettings>");
            Assert.That(_settings.NumberOfTestWorkers, Is.EqualTo(12));
        }

        [Test]
        public void DefaultTimeoutSetting()
        {
            _settings.Load("<RunSettings><NUnit><DefaultTimeout>5000</DefaultTimeout></NUnit></RunSettings>");
            Assert.That(_settings.DefaultTimeout, Is.EqualTo(5000));
        }

        [Test]
        public void ShadowCopySetting()
        {
            _settings.Load("<RunSettings><NUnit><ShadowCopyFiles>true</ShadowCopyFiles></NUnit></RunSettings>");
            Assert.True(_settings.ShadowCopyFiles);
        }

        [Test]
        public void VerbositySetting()
        {
            _settings.Load("<RunSettings><NUnit><Verbosity>1</Verbosity></NUnit></RunSettings>");
            Assert.That(_settings.Verbosity, Is.EqualTo(1));
        }

        [Test]
        public void UseVsKeepEngineRunningSetting()
        {
            _settings.Load("<RunSettings><NUnit><UseVsKeepEngineRunning>true</UseVsKeepEngineRunning></NUnit></RunSettings>");
            Assert.True(_settings.UseVsKeepEngineRunning);
        }

        [Test]
        public void BasePathSetting()
        {
            _settings.Load("<RunSettings><NUnit><BasePath>..</BasePath></NUnit></RunSettings>");
            Assert.That(_settings.BasePath, Is.EqualTo(".."));
        }

        [Test]
        public void PrivateBinPathSetting()
        {
            _settings.Load("<RunSettings><NUnit><PrivateBinPath>dir1;dir2</PrivateBinPath></NUnit></RunSettings>");
            Assert.That(_settings.PrivateBinPath, Is.EqualTo("dir1;dir2"));
        }

        [Test]
        public void RandomSeedSetting()
        {
            _settings.Load("<RunSettings><NUnit><RandomSeed>12345</RandomSeed></NUnit></RunSettings>");
            Assert.That(_settings.RandomSeed, Is.EqualTo(12345));
        }
    }
}

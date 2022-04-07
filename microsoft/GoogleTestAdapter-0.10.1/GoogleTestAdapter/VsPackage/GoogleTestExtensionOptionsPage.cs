﻿using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using GoogleTestAdapter.Settings;
using GoogleTestAdapter.TestAdapter.Settings;
using GoogleTestAdapter.VsPackage.Commands;
using GoogleTestAdapter.VsPackage.Debugging;
using GoogleTestAdapter.VsPackage.Helpers;
using GoogleTestAdapter.VsPackage.OptionsPages;
using GoogleTestAdapter.VsPackage.ReleaseNotes;

namespace GoogleTestAdapter.VsPackage
{

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(typeof(GeneralOptionsDialogPage), SettingsWrapper.OptionsCategoryName, SettingsWrapper.PageGeneralName, 0, 0, true)]
    [ProvideOptionPage(typeof(ParallelizationOptionsDialogPage), SettingsWrapper.OptionsCategoryName, SettingsWrapper.PageParallelizationName, 0, 0, true)]
    [ProvideOptionPage(typeof(GoogleTestOptionsDialogPage), SettingsWrapper.OptionsCategoryName, SettingsWrapper.PageGoogleTestName, 0, 0, true)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class GoogleTestExtensionOptionsPage : Package, IGoogleTestExtensionOptionsPage, IDisposable
    {
        private const string PackageGuidString = "e7c90fcb-0943-4908-9ae8-3b6a9d22ec9e";

        private readonly string _debuggingNamedPipeId = Guid.NewGuid().ToString();

        private IGlobalRunSettingsInternal _globalRunSettings;

        private GeneralOptionsDialogPage _generalOptions;
        private ParallelizationOptionsDialogPage _parallelizationOptions;
        private GoogleTestOptionsDialogPage _googleTestOptions;

        // ReSharper disable once NotAccessedField.Local
        private DebuggerAttacherService _debuggerAttacherService;

        protected override void Initialize()
        {
            base.Initialize();

            var componentModel = (IComponentModel)GetGlobalService(typeof(SComponentModel));
            _globalRunSettings = componentModel.GetService<IGlobalRunSettingsInternal>();

            _generalOptions = (GeneralOptionsDialogPage)GetDialogPage(typeof(GeneralOptionsDialogPage));
            _parallelizationOptions = (ParallelizationOptionsDialogPage)GetDialogPage(typeof(ParallelizationOptionsDialogPage));
            _googleTestOptions = (GoogleTestOptionsDialogPage)GetDialogPage(typeof(GoogleTestOptionsDialogPage));

            _globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();

            _generalOptions.PropertyChanged += OptionsChanged;
            _parallelizationOptions.PropertyChanged += OptionsChanged;
            _googleTestOptions.PropertyChanged += OptionsChanged;

            SwitchCatchExceptionsOptionCommand.Initialize(this);
            SwitchBreakOnFailureOptionCommand.Initialize(this);
            SwitchParallelExecutionOptionCommand.Initialize(this);
            SwitchPrintTestOutputOptionCommand.Initialize(this);

            var thread = new Thread(DisplayReleaseNotesIfNecessary);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var logger = new ActivityLogLogger(this, () => _generalOptions.DebugMode);
            var debuggerAttacher = new VsDebuggerAttacher(this);
            _debuggerAttacherService = new DebuggerAttacherService(_debuggingNamedPipeId, debuggerAttacher, logger);
        }

        public IVsActivityLog GetActivityLog()
        {
            return GetService(typeof(SVsActivityLog)) as IVsActivityLog;
        }


        public void Dispose()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _generalOptions?.Dispose();
                _parallelizationOptions?.Dispose();
                _googleTestOptions?.Dispose();

                _debuggerAttacherService.Dispose();
            }
            base.Dispose(disposing);
        }

        public bool CatchExtensions
        {
            get { return _googleTestOptions.CatchExceptions; }
            set
            {
                _googleTestOptions.CatchExceptions = value;
                RefreshVsUi();
            }
        }

        public bool BreakOnFailure
        {
            get { return _googleTestOptions.BreakOnFailure; }
            set
            {
                _googleTestOptions.BreakOnFailure = value;
                RefreshVsUi();
            }
        }

        public bool PrintTestOutput
        {
            get { return _generalOptions.PrintTestOutput; }
            set
            {
                _generalOptions.PrintTestOutput = value;
                RefreshVsUi();
            }
        }

        public bool ParallelTestExecution
        {
            get { return _parallelizationOptions.EnableParallelTestExecution; }
            set
            {
                _parallelizationOptions.EnableParallelTestExecution = value;
                RefreshVsUi();
            }
        }

        private void DisplayReleaseNotesIfNecessary()
        {
            var versionProvider = new VersionProvider(this);

            Version formerlyInstalledVersion = versionProvider.FormerlyInstalledVersion;
            Version currentVersion = versionProvider.CurrentVersion;

            versionProvider.UpdateLastVersion();

            if (!_generalOptions.ShowReleaseNotes
                || (formerlyInstalledVersion != null && formerlyInstalledVersion >= currentVersion))
                return;

            var creator = new ReleaseNotesCreator(formerlyInstalledVersion, currentVersion);
            DisplayReleaseNotes(creator.CreateHtml());
        }

        private void DisplayReleaseNotes(string html)
        {
            string htmlFileBase = Path.GetTempFileName();
            string htmlFile = Path.ChangeExtension(htmlFileBase, "html");
            File.Delete(htmlFileBase);

            File.WriteAllText(htmlFile, html);

            using (var dialog = new ReleaseNotesDialog {HtmlFile = new Uri($"file://{htmlFile}")})
            {
                dialog.ShowReleaseNotesChanged +=
                    (sender, args) => _generalOptions.ShowReleaseNotes = args.ShowReleaseNotes;
                dialog.Closed += (sender, args) => File.Delete(htmlFile);
                dialog.ShowDialog();
            }
        }

        private void RefreshVsUi()
        {
            var vsShell = (IVsUIShell)GetService(typeof(IVsUIShell));
            if (vsShell != null)
            {
                int hr = vsShell.UpdateCommandUI(Convert.ToInt32(false));
                ErrorHandler.ThrowOnFailure(hr);
            }
        }

        private void OptionsChanged(object sender, PropertyChangedEventArgs e)
        {
            _globalRunSettings.RunSettings = GetRunSettingsFromOptionPages();
        }

        private RunSettings GetRunSettingsFromOptionPages()
        {
            return new RunSettings
            {
                PrintTestOutput = _generalOptions.PrintTestOutput,
                TestDiscoveryRegex = _generalOptions.TestDiscoveryRegex,
                TestDiscoveryTimeoutInSeconds = _generalOptions.TestDiscoveryTimeoutInSeconds,
                WorkingDir = _generalOptions.WorkingDir,
                PathExtension = _generalOptions.PathExtension,
                TraitsRegexesBefore = _generalOptions.TraitsRegexesBefore,
                TraitsRegexesAfter = _generalOptions.TraitsRegexesAfter,
                TestNameSeparator = _generalOptions.TestNameSeparator,
                ParseSymbolInformation = _generalOptions.ParseSymbolInformation,
                DebugMode = _generalOptions.DebugMode,
                TimestampOutput = _generalOptions.TimestampOutput,
                ShowReleaseNotes = _generalOptions.ShowReleaseNotes,
                AdditionalTestExecutionParam = _generalOptions.AdditionalTestExecutionParams,
                BatchForTestSetup = _generalOptions.BatchForTestSetup,
                BatchForTestTeardown = _generalOptions.BatchForTestTeardown,
                KillProcessesOnCancel = _generalOptions.KillProcessesOnCancel,

                CatchExceptions = _googleTestOptions.CatchExceptions,
                BreakOnFailure = _googleTestOptions.BreakOnFailure,
                RunDisabledTests = _googleTestOptions.RunDisabledTests,
                NrOfTestRepetitions = _googleTestOptions.NrOfTestRepetitions,
                ShuffleTests = _googleTestOptions.ShuffleTests,
                ShuffleTestsSeed = _googleTestOptions.ShuffleTestsSeed,

                ParallelTestExecution = _parallelizationOptions.EnableParallelTestExecution,
                MaxNrOfThreads = _parallelizationOptions.MaxNrOfThreads,

                UseNewTestExecutionFramework = _generalOptions.UseNewTestExecutionFramework2,

                DebuggingNamedPipeId = _debuggingNamedPipeId
            };
        }
    }

}

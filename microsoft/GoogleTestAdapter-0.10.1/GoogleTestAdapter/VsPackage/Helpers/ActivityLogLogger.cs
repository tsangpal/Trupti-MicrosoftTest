﻿using System;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Settings;
using Microsoft.VisualStudio.Shell.Interop;

namespace GoogleTestAdapter.VsPackage.Helpers
{
    public class ActivityLogLogger : LoggerBase
    {
        private readonly GoogleTestExtensionOptionsPage _package;

        public ActivityLogLogger(GoogleTestExtensionOptionsPage package, Func<bool> inDebugMode) : base(inDebugMode)
        {
            _package = package;
        }

        public override void Log(Severity severity, string message)
        {
            var activityLog = _package.GetActivityLog();
            if (activityLog == null)
            {
                Console.WriteLine($"Google Test Adapter: {severity} - {message}");
                return;
            }

            __ACTIVITYLOG_ENTRYTYPE activitylogEntrytype;
            switch (severity)
            {
                case Severity.Info:
                    activitylogEntrytype = __ACTIVITYLOG_ENTRYTYPE.ALE_INFORMATION;
                    break;
                case Severity.Warning:
                    activitylogEntrytype = __ACTIVITYLOG_ENTRYTYPE.ALE_WARNING;
                    break;
                case Severity.Error:
                    activitylogEntrytype = __ACTIVITYLOG_ENTRYTYPE.ALE_ERROR;
                    break;
                default:
                    throw new Exception($"Unknown enum literal: {severity}");
            }

            activityLog.LogEntry((uint)activitylogEntrytype, SettingsWrapper.OptionsCategoryName, message);
        }
    }

}
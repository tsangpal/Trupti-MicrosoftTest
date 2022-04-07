﻿namespace GoogleTestAdapter.VsPackage.Commands
{

    internal sealed class SwitchParallelExecutionOptionCommand : AbstractSwitchBooleanOptionCommand
    {

        private SwitchParallelExecutionOptionCommand(IGoogleTestExtensionOptionsPage package) : base(package, 0x0102) {}

        private static SwitchParallelExecutionOptionCommand Instance
        {
            get; set;
        }

        internal static void Initialize(IGoogleTestExtensionOptionsPage package)
        {
            Instance = new SwitchParallelExecutionOptionCommand(package);
        }

        protected override bool Value
        {
            get { return Package.ParallelTestExecution; }
            set { Package.ParallelTestExecution = value; }
        }

    }

}
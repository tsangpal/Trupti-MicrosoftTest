using System;
using System.Threading;
using FluentAssertions;
using GoogleTestAdapter.Common;
using GoogleTestAdapter.Framework;
using GoogleTestAdapter.TestAdapter.Framework;
using GoogleTestAdapter.Tests.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using static GoogleTestAdapter.Tests.Common.TestMetadata.TestCategories;

namespace GoogleTestAdapter.VsPackage.Debugging
{
    [TestClass]
    public class MessageBasedDebuggerAttacherTests
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(4);
        public static readonly TimeSpan Tolerance = TestMetadata.Tolerance;

        private Mock<IDebuggerAttacher> MockDebuggerAttacher { get; } = new Mock<IDebuggerAttacher>();
        private Mock<ILogger> MockLogger { get; } = new Mock<ILogger>();

        [TestInitialize]
        public void Setup()
        {
            MockDebuggerAttacher.Reset();
            MockLogger.Reset();
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_AttachingSucceeds_DebugOutputGenerated()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(GetAttachDebuggerAction(() => true));

            DoTest(true);

            MockLogger.Verify(l => l.DebugInfo(It.IsAny<string>()), Times.Exactly(3));
            MockLogger.Verify(l => l.DebugInfo(It.Is<string>(s => s.ToLower().Contains("server"))), Times.Exactly(2));
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_AttachingFails_ErrorOutputGenerated()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(GetAttachDebuggerAction(() => false));

            DoTest(false);

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("unknown reasons"))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_AttachingThrows_ErrorOutputGenerated()
        {
            MockDebuggerAttacher
                .Setup(a => a.AttachDebugger(It.IsAny<int>()))
                .Returns(GetAttachDebuggerAction(() => { throw new Exception("my message"); }));

            DoTest(false);

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("my message"))), Times.Once);
        }

        [TestMethod]
        [TestCategory(Integration)]
        public void AttachDebugger_NoPipeAvailable_ErrorOutputGenerated()
        {
            var client = new MessageBasedDebuggerAttacher(Guid.NewGuid().ToString(), Timeout, MockLogger.Object);
            client.AttachDebugger(2017).Should().BeFalse();

            MockLogger.Verify(l => l.LogError(It.Is<string>(s => s.Contains("Could not connect to NamedPipe"))), Times.Once);
        }

        private void DoTest(bool expectedResult)
        {
            string pipeId = Guid.NewGuid().ToString();
            int debuggeeProcessId = 2017;

            // ReSharper disable once UnusedVariable
            using (var service = new DebuggerAttacherService(pipeId, MockDebuggerAttacher.Object, MockLogger.Object))
            {
                var client = new MessageBasedDebuggerAttacher(pipeId, Timeout, MockLogger.Object);

                client.AttachDebugger(debuggeeProcessId).Should().Be(expectedResult);

                MockDebuggerAttacher.Verify(a => a.AttachDebugger(It.Is<int>(processId => processId == debuggeeProcessId)),
                    Times.Once);
            }
        }

        public static Func<bool> GetAttachDebuggerAction(Func<bool> function)
        {
            return () =>
            {
                Thread.Sleep(25);
                return function();
            };
        }

    }
}
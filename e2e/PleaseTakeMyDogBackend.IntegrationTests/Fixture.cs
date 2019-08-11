using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PleaseTakeMyDogBackend.IntegrationTests
{
    public abstract class Fixture : IDisposable
    {
        private readonly IMessageSink messageSink;
        public Process Process { get; set; }
        public bool ProcessStarting { get; set; }

        protected Fixture(IMessageSink messageSink)
        {
            this.messageSink = messageSink;

            var dir = Directory.GetCurrentDirectory();
            var dirSplit = dir.Split('\\');
            var workingDir = string.Join('\\', dirSplit.Take(dirSplit.Length - 5));
            Console.WriteLine(workingDir);

            this.Process = new Process
            {
                StartInfo =
                {
                    FileName = "sam",
                    Arguments = "local start-api",
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            this.Process.OutputDataReceived += ProcessOnOutputDataReceived;
            this.Process.ErrorDataReceived += ProcessOnErrorDataReceived;

            this.Process.Start();
            this.ProcessStarting = true;

            this.Process.BeginOutputReadLine();
            this.Process.BeginErrorReadLine();

            while (this.ProcessStarting)
                Thread.Sleep(100);
        }

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
            this.ProcessStarting = false;
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);

            if (e.Data.Contains("Running on"))
                this.ProcessStarting = false;
        }

        public void Dispose()
        {
            this.messageSink.OnMessage(new DiagnosticMessage("Shutting down {0}", this.GetType().Name));
            this.Process.Kill();
            this.Process.Dispose();
            this.OnDisposing();
        }

        protected virtual void OnDisposing() => this.Process.Dispose();
    }
}

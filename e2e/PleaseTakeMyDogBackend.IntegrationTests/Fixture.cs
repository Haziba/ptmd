using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace PleaseTakeMyDogBackend.IntegrationTests
{
	public abstract class Fixture : IDisposable
	{
		private readonly IMessageSink messageSink;
	    public Process Process { get; set; }

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

            this.Process.BeginOutputReadLine();
            this.Process.BeginErrorReadLine();

		    Thread.Sleep(3000);
		}

        private void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data);
        }

		public void Dispose()
		{
			this.messageSink.OnMessage(new DiagnosticMessage("Shutting down {0}", this.GetType().Name));
		    this.Process.Dispose();
			this.OnDisposing();
		}

		protected virtual void OnDisposing() => this.Process.Dispose();
	}
}

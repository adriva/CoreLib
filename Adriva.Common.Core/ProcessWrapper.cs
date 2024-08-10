using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading;

namespace Adriva.Common.Core
{
    public class ProcessWrapper
    {
        private readonly string Executable;

        private readonly string[] Arguments;

        public ProcessWrapper(string executable, params string[] args)
        {
            this.Executable = executable;

            if (null != args)
            {
                this.Arguments = args.Select(arg => $"\"{arg}\"").ToArray();
            }
        }

        public async Task<bool> RunAsync(int timeoutInSeconds, ILogger logger, string workingDirectory = null)
        {
            using (StringWriter output = new StringWriter())
            {
                return await this.RunAsync(timeoutInSeconds, output, logger, workingDirectory);
            }
        }

        /// <summary>
        /// <params name="timeoutInSeconds">Time, in seconds, to wait for the process to finish. 0 means wait indefinitely.</params>
        /// </summary>
        public async Task<bool> RunAsync(int timeoutInSeconds, TextWriter output, ILogger logger, string workingDirectory = null)
        {
            timeoutInSeconds = Math.Max(0, timeoutInSeconds);

            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo()
                {
                    Arguments = string.Join(" ", this.Arguments),
                    FileName = this.Executable,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                process.Start();

                bool hasGracefulExit = await Task<bool>.Run(() => process.WaitForExit(timeoutInSeconds * 1000));

                if (!hasGracefulExit)
                {
                    bool hasKilled = false;
                    int retryCount = 3;

                    while (!hasKilled && 0 < retryCount)
                    {
                        try
                        {
                            process.Kill();
                            hasKilled = true;
                        }
                        catch (Exception error)
                        {
                            logger.LogWarning($"Couldn't kill process {this.Executable}. Error {error.GetType().FullName}");
                            logger.LogWarning(error, "May retry.");
                            hasKilled = false;
                            --retryCount;
                            Thread.Yield();
                        }
                    }
                }

                var outputData = await process.StandardOutput.ReadToEndAsync();
                var errorData = await process.StandardError.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(outputData))
                {
                    await output.WriteAsync(outputData);
                }

                if (!string.IsNullOrWhiteSpace(errorData))
                {
                    await output.WriteAsync(errorData);
                }

                logger.LogTrace($"Process output from {process.Id} [{process.StartInfo.FileName}]");
                logger.LogTrace(output.ToString());

                return hasGracefulExit ? 0 == process.ExitCode : false;
            }
        }
    }
}

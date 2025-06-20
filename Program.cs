using System;
using System.Threading.Tasks;

namespace AntiIdleWindows
{
	/// <summary>
	/// Main program entry point
	/// </summary>
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("=== Anti-Idle Windows Utility ===");
			Console.WriteLine();

			// Parse command line arguments
			var method = KeepAliveMethod.Hybrid;
			var interval = 30;

			if (args.Length > 0)
			{
				if (Enum.TryParse<KeepAliveMethod>(args[0], true, out var parsedMethod))
				{
					method = parsedMethod;
				}
			}

			if (args.Length > 1)
			{
				if (int.TryParse(args[1], out var parsedInterval) && parsedInterval > 0)
				{
					interval = parsedInterval;
				}
			}

			// Handle Ctrl+C gracefully
			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				Console.WriteLine("\nShutting down gracefully...");
				ConsoleInterface.Stop();
				SystemKeepAlive.Stop();
				Environment.Exit(0);
			};

			// Start services
			SystemKeepAlive.Start(method, interval);
			ConsoleInterface.Start();

			// Wait for exit command
			await ConsoleInterface.WaitForExit();

			// Cleanup
			SystemKeepAlive.Stop();
			Console.WriteLine("Program terminated.");
		}
	}
}
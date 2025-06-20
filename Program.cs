using System;
using System.Threading.Tasks;

namespace AntiIdleWindows
{
	class Program
	{
		static async Task Main(string[] args)
		{
			Console.WriteLine("=== Anti-Idle Windows Utility ===");
			Console.WriteLine();

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

			Console.CancelKeyPress += (_, e) =>
			{
				e.Cancel = true;
				Console.WriteLine("\nShutting down gracefully...");
				ConsoleInterface.Stop();
				SystemKeepAlive.Stop();
				Environment.Exit(0);
			};

			SystemKeepAlive.Start(method, interval);
			ConsoleInterface.Start();

			await ConsoleInterface.WaitForExit();

			SystemKeepAlive.Stop();
			Console.WriteLine("Program terminated.");
		}
	}
}
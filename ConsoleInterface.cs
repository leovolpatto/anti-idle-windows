using System;
using System.Threading;
using System.Threading.Tasks;

namespace AntiIdleWindows
{
	/// <summary>
	/// Handles console input and user interaction
	/// </summary>
	public static class ConsoleInterface
	{
		private static CancellationTokenSource? _inputCancellationSource;
		private static Task? _inputTask;
		private static volatile bool _shouldExit = false;
		private static readonly object _consoleLock = new object();

		/// <summary>
		/// Starts the console interface on a separate thread
		/// </summary>
		public static void Start()
		{
			_inputCancellationSource = new CancellationTokenSource();
			_inputTask = Task.Run(() => HandleInput(_inputCancellationSource.Token));

			// Subscribe to status changes
			SystemKeepAlive.StatusChanged += OnStatusChanged;

			ShowHelp();
		}

		/// <summary>
		/// Stops the console interface
		/// </summary>
		public static void Stop()
		{
			_shouldExit = true;
			_inputCancellationSource?.Cancel();
			SystemKeepAlive.StatusChanged -= OnStatusChanged;
		}

		/// <summary>
		/// Waits for the interface to complete
		/// </summary>
		public static async Task WaitForExit()
		{
			if (_inputTask != null)
			{
				await _inputTask;
			}
		}

		private static void OnStatusChanged(string message)
		{
			// Thread-safe console output
			lock (_consoleLock)
			{
				// Clear current line and write message
				Console.Write("\r" + new string(' ', Math.Max(Console.WindowWidth - 1, 80)) + "\r");
				Console.WriteLine(message);

				if (!_shouldExit && SystemKeepAlive.IsRunning)
				{
					Console.Write("> ");
				}
			}
		}

		private static async Task HandleInput(CancellationToken cancellationToken)
		{
			try
			{
				lock (_consoleLock)
				{
					Console.Write("> ");
				}

				while (!cancellationToken.IsCancellationRequested && !_shouldExit)
				{
					try
					{
						// Read input in a separate task to make it cancellable
						string? input = null;
						var readTask = Task.Run(() =>
						{
							try
							{
								return Console.ReadLine();
							}
							catch
							{
								return null;
							}
						});

						// Wait for either input or cancellation
						var completedTask = await Task.WhenAny(readTask, Task.Delay(-1, cancellationToken));

						if (completedTask == readTask && !cancellationToken.IsCancellationRequested)
						{
							input = await readTask;

							if (!string.IsNullOrEmpty(input))
							{
								var success = ProcessCommand(input.Trim().ToLower());

								// Debug output
								if (!success && input != "q" && input != "quit" && input != "exit")
								{
									lock (_consoleLock)
									{
										Console.WriteLine($"Debug: Command '{input}' not recognized");
									}
								}
							}

							if (!_shouldExit && !cancellationToken.IsCancellationRequested)
							{
								lock (_consoleLock)
								{
									Console.Write("> ");
								}
							}
						}
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (Exception ex)
					{
						lock (_consoleLock)
						{
							Console.WriteLine($"Input error: {ex.Message}");
							Console.Write("> ");
						}
					}
				}
			}
			catch (Exception ex)
			{
				lock (_consoleLock)
				{
					Console.WriteLine($"Error in input handling: {ex.Message}");
				}
			}
		}

		private static bool ProcessCommand(string input)
		{
			switch (input)
			{
				case "p":
				case "pause":
					SystemKeepAlive.Pause();
					return true;

				case "r":
				case "resume":
					SystemKeepAlive.Resume();
					return true;

				case "t":
				case "toggle":
					SystemKeepAlive.Toggle();
					return true;

				case "s":
				case "status":
					ShowStatus();
					return true;

				case "h":
				case "help":
					ShowHelp();
					return true;

				case "q":
				case "quit":
				case "exit":
					_shouldExit = true;
					return true;

				case "":
					// Empty input, do nothing but don't show error
					return true;

				default:
					lock (_consoleLock)
					{
						Console.WriteLine("Unknown command. Type 'h' for help.");
					}
					return false;
			}
		}

		private static void ShowHelp()
		{
			lock (_consoleLock)
			{
				Console.WriteLine("Available commands:");
				Console.WriteLine("  p, pause  - Pause keep-alive (allows system to sleep)");
				Console.WriteLine("  r, resume - Resume keep-alive");
				//Console.WriteLine("  t, toggle - Toggle pause/resume");
				Console.WriteLine("  s, status - Show current status");
				Console.WriteLine("  h, help   - Show this help");
				Console.WriteLine("  q, quit   - Exit program");
				Console.WriteLine();
				Console.WriteLine("Command line usage:");
				Console.WriteLine("  anti-idle-windows.exe [method] [interval]");
				Console.WriteLine();
				Console.WriteLine("Keep-alive methods:");
				Console.WriteLine("  ExecutionState - Uses Windows API (recommended, default)");
				Console.WriteLine("  MouseJiggle    - Simulates minimal mouse movement");
				Console.WriteLine("  Hybrid         - Combines both methods (default)");
				Console.WriteLine();
				Console.WriteLine("Examples:");
				Console.WriteLine("  anti-idle-windows.exe");
				Console.WriteLine("  anti-idle-windows.exe ExecutionState 60");
				Console.WriteLine("  anti-idle-windows.exe MouseJiggle 30");
				Console.WriteLine("  anti-idle-windows.exe Hybrid 45");
				Console.WriteLine();
			}
		}

		private static void ShowStatus()
		{
			var status = SystemKeepAlive.IsRunning ?
					(SystemKeepAlive.IsPaused ? "RUNNING (PAUSED)" : "RUNNING (ACTIVE)") :
					"STOPPED";

			lock (_consoleLock)
			{
				Console.WriteLine($"Status: {status}");
				Console.WriteLine($"Method: {SystemKeepAlive.CurrentMethod}");
				Console.WriteLine($"Interval: {SystemKeepAlive.CurrentInterval} seconds");

				if (SystemKeepAlive.IsPaused)
				{
					Console.WriteLine("⚠️  System can currently go idle/sleep");
				}
				else if (SystemKeepAlive.IsRunning)
				{
					Console.WriteLine("✅ System is being kept awake");
				}

				Console.WriteLine();
			}
		}
	}
}
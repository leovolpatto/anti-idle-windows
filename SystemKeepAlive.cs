using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AntiIdleWindows
{
	public static class SystemKeepAlive
	{
		#region Windows API Imports

		[Flags]
		public enum ExecutionState : uint
		{
			ES_AWAYMODE_REQUIRED = 0x00000040,
			ES_CONTINUOUS = 0x80000000,
			ES_DISPLAY_REQUIRED = 0x00000002,
			ES_SYSTEM_REQUIRED = 0x00000001
		}

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern uint SetThreadExecutionState(ExecutionState esFlags);

		[DllImport("user32.dll")]
		private static extern bool SetCursorPos(int x, int y);

		[DllImport("user32.dll")]
		private static extern bool GetCursorPos(out POINT lpPoint);

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
		}

		#endregion

		private static CancellationTokenSource? _cancellationTokenSource;
		private static Task? _keepAliveTask;
		private static volatile bool _isPaused = false;
		private static volatile bool _isRunning = false;
		private static KeepAliveMethod _currentMethod = KeepAliveMethod.ExecutionState;
		private static int _currentInterval = 30;
		private static readonly object _lockObject = new object();
		private const int MOUSE_MOV_DISTANCE = 500;
		private const int MOUSE_MOV_SPEED = 30;

		/// <summary>
		/// Event fired when the service status changes
		/// </summary>
		public static event Action<string>? StatusChanged;

		/// <summary>
		/// Gets whether the keep-alive service is currently running
		/// </summary>
		public static bool IsRunning => _isRunning;

		/// <summary>
		/// Gets whether the keep-alive service is currently paused
		/// </summary>
		public static bool IsPaused => _isPaused;

		/// <summary>
		/// Gets the current method being used
		/// </summary>
		public static KeepAliveMethod CurrentMethod => _currentMethod;

		/// <summary>
		/// Gets the current interval in seconds
		/// </summary>
		public static int CurrentInterval => _currentInterval;

		public static void Start(KeepAliveMethod method = KeepAliveMethod.ExecutionState, int intervalSeconds = 30)
		{
			lock (_lockObject)
			{
				if (_isRunning)
				{
					StatusChanged?.Invoke("Keep-alive service is already running.");
					return;
				}

				_currentMethod = method;
				_currentInterval = intervalSeconds;
				_isPaused = false;
				_cancellationTokenSource = new CancellationTokenSource();

				_keepAliveTask = Task.Run(() => KeepAliveLoop(_cancellationTokenSource.Token));
				_isRunning = true;

				StatusChanged?.Invoke($"Keep-alive service started using {method} method (interval: {intervalSeconds}s)");
			}
		}

		public static void Pause()
		{
			lock (_lockObject)
			{
				if (!_isRunning)
				{
					StatusChanged?.Invoke("Keep-alive service is not running.");
					return;
				}

				if (_isPaused)
				{
					StatusChanged?.Invoke("Keep-alive service is already paused.");
					return;
				}

				_isPaused = true;
				// Reset execution state to allow system to sleep
				SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);
				StatusChanged?.Invoke("Keep-alive service PAUSED. System can now go idle/sleep.");
			}
		}

		public static void Resume()
		{
			lock (_lockObject)
			{
				if (!_isRunning)
				{
					StatusChanged?.Invoke("Keep-alive service is not running.");
					return;
				}

				if (!_isPaused)
				{
					StatusChanged?.Invoke("Keep-alive service is already active.");
					return;
				}

				_isPaused = false;
				StatusChanged?.Invoke("Keep-alive service RESUMED. System will stay awake.");
			}
		}

		public static void Toggle()
		{
			if (_isPaused){
				Resume();
				return;
			}
			
			Pause();
		}

		public static void Stop()
		{
			lock (_lockObject)
			{
				if (!_isRunning)
				{
					StatusChanged?.Invoke("Keep-alive service is not running.");
					return;
				}

				_cancellationTokenSource?.Cancel();

				SetThreadExecutionState(ExecutionState.ES_CONTINUOUS);

				_isPaused = false;
				_isRunning = false;

				StatusChanged?.Invoke("Keep-alive service stopped.");
			}
		}

		private static async Task KeepAliveLoop(CancellationToken cancellationToken)
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					if (!_isPaused)
					{
						switch (_currentMethod)
						{
							case KeepAliveMethod.ExecutionState:
								PreventSleepViaExecutionState();
								break;

							case KeepAliveMethod.MouseJiggle:
								JiggleMouse();
								break;

							case KeepAliveMethod.Hybrid:
								JiggleMouse();
								PreventSleepViaExecutionState();
								//if (_currentInterval >= 60) // Only jiggle mouse occasionally for hybrid mode
									//JiggleMouse();
								break;
						}

						StatusChanged?.Invoke($"[{DateTime.Now:HH:mm:ss}] System kept awake using {_currentMethod}");
					}
					else
					{
						StatusChanged?.Invoke($"[{DateTime.Now:HH:mm:ss}] Keep-alive PAUSED - system can idle");
					}

					await Task.Delay(TimeSpan.FromSeconds(_currentInterval), cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				StatusChanged?.Invoke("Keep-alive loop terminated.");
			}
			catch (Exception ex)
			{
				StatusChanged?.Invoke($"Error in keep-alive loop: {ex.Message}");
			}
		}

		private static void PreventSleepViaExecutionState()
		{
			var result = SetThreadExecutionState(
					ExecutionState.ES_CONTINUOUS |
					ExecutionState.ES_DISPLAY_REQUIRED |
					ExecutionState.ES_SYSTEM_REQUIRED
			);

			if (result == 0)
			{
				StatusChanged?.Invoke("Warning: Failed to set thread execution state.");
			}
		}

		private static void JiggleMouse()
		{
			if (GetCursorPos(out POINT currentPos))
			{
				int currentX = currentPos.X;


				for (int x = currentX; x < currentX + MOUSE_MOV_DISTANCE; x += MOUSE_MOV_SPEED)
				{
					SetCursorPos(x, currentPos.Y);
					Thread.Sleep(1);
				}

				for (int x = currentX + MOUSE_MOV_DISTANCE; x > currentX; x -= MOUSE_MOV_SPEED)
				{
					SetCursorPos(x, currentPos.Y);
					Thread.Sleep(1);
				}
			}
		}
	}
}
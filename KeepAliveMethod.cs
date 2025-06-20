namespace AntiIdleWindows
{
	public enum KeepAliveMethod
	{
		/// <summary>
		/// Uses Windows API SetThreadExecutionState (recommended)
		/// </summary>
		ExecutionState,

		/// <summary>
		/// Simulates minimal mouse movement
		/// </summary>
		MouseJiggle,

		/// <summary>
		/// Combines both methods for maximum effectiveness
		/// </summary>
		Hybrid
	}
}
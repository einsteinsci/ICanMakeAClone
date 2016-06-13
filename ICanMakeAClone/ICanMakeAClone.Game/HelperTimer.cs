using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Xenko.Games;

namespace ICanMakeAClone
{
	public class HelperTimer
	{
		public TimeSpan InitialTime
		{ get; private set; }

		public TimeSpan TimeLeft
		{ get; private set; }

		public bool IsRunning
		{ get; private set; }

		public Action OnCompletion
		{ get; private set; }

		public string Label
		{ get; private set; }

		public HelperTimer(TimeSpan timeout, bool startNow, string label, Action onCompletion)
		{
			InitialTime = timeout;
			TimeLeft = timeout;
			IsRunning = startNow;
			Label = label;
			OnCompletion = onCompletion;
		}
		public HelperTimer(TimeSpan timeout, string label, Action onCompletion) : this(timeout, true, label, onCompletion)
		{ }
		public HelperTimer(TimeSpan timeout, Action onCompletion) : this(timeout, true, null, onCompletion)
		{ }

		public void Update(GameTime gameTime)
		{
			if (IsRunning)
			{
				TimeLeft -= gameTime.Elapsed;
			}

			if (TimeLeft.TotalMilliseconds <= 0 && IsRunning)
			{
				TimeLeft = TimeSpan.Zero; // keep things clean
				IsRunning = false;
				OnCompletion();
			}
		}

		public void Reset()
		{
			IsRunning = false;
			TimeLeft = InitialTime;
		}

		public void Stop()
		{
			IsRunning = false;
		}

		public void Start()
		{
			IsRunning = true;
		}

		public void Toggle()
		{
			IsRunning = !IsRunning;
		}

		public override string ToString()
		{
			return (IsRunning ? "[RUNNING] " : "[STOPPED] ") + TimeLeft + " / " + InitialTime;
		}
	}
}

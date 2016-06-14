using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICanMakeAClone.ONAF2;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone.AI
{
	public abstract class AIBase : IPlayerSource
	{
		public static readonly Vector2 VENT_TARGET_L = new Vector2(50, 365);
		public static readonly Vector2 VENT_TARGET_R = new Vector2(1230, 365);
		public static readonly Vector2 LIGHT_TARGET = new Vector2(750, 550);
		public static readonly Vector2 LAPTOP_TARGET = new Vector2(640, 680);
		public static readonly Vector2 REDMAN_CANCEL_TARGET = new Vector2(288, 203);

		public virtual Vector2 MousePos
		{ get; protected set; }

		public virtual bool ShowsMouse => true;

		public abstract string SourceName
		{ get; }

		public Level Level
		{ get; private set; }

		public bool IsClicking
		{ get; protected set; }

		public bool WasClicking
		{ get; protected set; }

		public VentState? TargetedVent
		{ get; protected set; }

		public bool TargetLight
		{ get; protected set; }

		public bool DoingLaptopThings
		{ get; protected set; }

		public bool TargetingLaptop
		{ get; protected set; }

		public bool IsLaptopUp => Level.UI.State == UIState.Laptop;

		protected AIBase(Level level)
		{
			Level = level;
		}

		public abstract List<string> GetDebugLines();

		#region INPUT
		public virtual bool IsButtonDown(MouseButton mb)
		{
			return IsClicking && mb == MouseButton.Left;
		}
		public virtual bool IsButtonPressed(MouseButton mb)
		{
			return IsClicking && !WasClicking && mb == MouseButton.Left;
		}
		public virtual bool IsButtonReleased(MouseButton mb)
		{
			return !IsClicking && WasClicking && mb == MouseButton.Left;
		}
		#endregion INPUT

		public void ClickOn(Vector2 spot)
		{
			MousePos = spot;
			IsClicking = true;
			WasClicking = false; // to allow clicking one frame after another
		}
		
		public virtual void Reset()
		{
			IsClicking = false;
			WasClicking = false;
			TargetingLaptop = false;
			TargetedVent = null;
			TargetLight = false;
			DoingLaptopThings = false;

			MousePos = Level.Main.WindowCenter;
		}

		public void ToggleLaptop()
		{
			if (Level.Office.IsLightOn)
			{
				TargetingLaptop = true;
				MousePos = LAPTOP_TARGET;
				if (!IsLaptopUp)
				{
					DoingLaptopThings = true;
				}
			}
		}

		public virtual void Update(GameTime gt, InputManager input)
		{
			WasClicking = IsClicking;
			IsClicking = false;
			TargetingLaptop = false;

			if (MousePos == LAPTOP_TARGET)
			{
				MousePos = Level.Main.WindowCenter;
			}

			RunAI(gt);

			if (!IsLaptopUp)
			{
				if (TargetedVent == VentState.Left)
				{
					MousePos = VENT_TARGET_L;

					if (Level.Office.CameraOffset.X >= 0)
					{
						ClickOn(VENT_TARGET_L);
						TargetedVent = null;
					}
				}
				else if (TargetedVent == VentState.Right)
				{
					MousePos = VENT_TARGET_R;

					if (Level.Office.CameraOffset.X <= Office.MAX_CAMERA_OFFSET)
					{
						ClickOn(VENT_TARGET_R);
						TargetedVent = null;
					}
				}

				if (TargetLight)
				{
					ClickOn(LIGHT_TARGET + Level.Office.CameraOffset);
					TargetLight = false;
				}
			}

			// if these don't match
			if ((IsLaptopUp ^ DoingLaptopThings) && !IsClicking && TargetedVent == null)
			{
				ToggleLaptop();
				//return;
			}

			if (Level.Laptop.IsLaptopSwitching && MousePos == LAPTOP_TARGET)
			{
				MousePos = Level.Main.WindowCenter;
			}
		}

		public abstract void RunAI(GameTime gt);
	}
}
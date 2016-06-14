using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICanMakeAClone.AI;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone
{
	public class InputRegion
	{
		public RectangleF Region
		{ get; set; }

		public bool IsEnabled
		{ get; set; }

		public event Action<InputManager> Click;

		public event Action<InputManager> MouseDown;
		public event Action<InputManager> MouseUp;

		#region constructors

		public InputRegion(RectangleF region, bool enabled = true, Action<InputManager> click = null)
		{
			Region = region;
			IsEnabled = enabled;
			Click += click;
		}
		public InputRegion(Vector2 topLeft, Vector2 size, bool enabled = true, Action<InputManager> click = null)
		{
			Region = new RectangleF(topLeft.X, topLeft.Y, size.X, size.Y);
			IsEnabled = enabled;
			Click += click;
		}
		public InputRegion(float x, float y, float width, float height, bool enabled = true, Action<InputManager> click = null)
		{
			Region = new RectangleF(x, y, width, height);
			IsEnabled = enabled;
			Click += click;
		}

		#endregion constructors

		public void Update(InputManager input, Vector2 windowSize)
		{
			if (IsEnabled && Region.Contains(input.GetMousePosPx(windowSize)))
			{
				if (input.IsMouseButtonPressed(MouseButton.Left))
				{
					Click?.Invoke(input);
				}
				else if (input.IsMouseButtonReleased(MouseButton.Left))
				{
					MouseUp?.Invoke(input);
				}

				if (input.IsMouseButtonDown(MouseButton.Left))
				{
					MouseDown?.Invoke(input);
				}
			}
		}

		public void Update(IPlayerSource bot)
		{
			if (IsEnabled && Region.Contains(bot.MousePos))
			{
				if (bot.IsButtonPressed(MouseButton.Left))
				{
					Click?.Invoke(null);
				}
				else if (bot.IsButtonReleased(MouseButton.Left))
				{
					MouseUp?.Invoke(null);
				}

				if (bot.IsButtonDown(MouseButton.Left))
				{
					MouseDown?.Invoke(null);
				}
			}
		}

		public void SetXY(Vector2 xy)
		{
			Region = new RectangleF(xy.X, xy.Y, Region.Width, Region.Height);
		}
	}
}

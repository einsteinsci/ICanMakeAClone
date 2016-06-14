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
	public class PlayerMouseInput : IPlayerSource
	{
		public string SourceName => "Player";

		public Vector2 MousePos
		{ get; private set; }

		public Level Level
		{ get; private set; }

		private InputManager _input;

		public bool ShowsMouse => false;

		public PlayerMouseInput(Level level)
		{
			Level = level;
		}

		public List<string> GetDebugLines()
		{
			List<string> res = new List<string>();

			res.Add("Mouse Pos: " + (int)MousePos.X + " " + (int)MousePos.Y);

			res.Add("Buttons Down:");
			for (MouseButton mb = MouseButton.Left; mb <= MouseButton.Extended2; mb++)
			{
				if (IsButtonDown(mb))
				{
					res.Add("  " + mb);
				}
			}

			return res;
		}

		public bool IsButtonPressed(MouseButton mb)
		{
			return _input?.IsMouseButtonPressed(mb) ?? false;
		}

		public bool IsButtonDown(MouseButton mb)
		{
			return _input?.IsMouseButtonDown(mb) ?? false;
		}

		public bool IsButtonReleased(MouseButton mb)
		{
			return _input?.IsMouseButtonReleased(mb) ?? false;
		}

		public void Update(GameTime gt, InputManager input)
		{
			MousePos = input.GetMousePosPx(Level.Main.WindowSize);

			if (_input == null)
			{
				_input = input;
			}
		}

		public void Reset()
		{ }
	}
}
using System.Collections.Generic;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone.AI
{
	public interface IPlayerSource
	{
		string SourceName
		{ get; }

		bool ShowsMouse
		{ get; }

		Vector2 MousePos
		{ get; }

		List<string> GetDebugLines();

		bool IsButtonPressed(MouseButton mb);
		bool IsButtonDown(MouseButton mb);
		bool IsButtonReleased(MouseButton mb);

		void Update(GameTime gt, InputManager input);

		void Reset();
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone.ONAF2
{
	public sealed class OnafMain : IRetroComponent
	{
		public const int STATIC_OVERLAY_ALPHA = 4;

		public CloneGame MainGame
		{ get; private set; }

		public Vector2 WindowSize => MainGame.WindowSize;
		public Vector2 WindowCenter => WindowSize / 2.0f;

		public Random Rand
		{ get; private set; }

		public UIScreen UI
		{ get; private set; }

		public SavedGame Save
		{ get; private set; }

		public Level Level
		{ get; private set; }

		public float Volume
		{
			get
			{
				return Save.Volume;
			}
			set
			{
				Save.Volume = value;
				Level.VolumeController.SetVolume(value);
				Save.Save();
			}
		}

		public bool HasWon
		{
			get
			{
				return Save.HasWon;
			}
			set
			{
				Save.HasWon = value;
				Save.Save();
			}
		}

		public OnafMain(CloneGame mainGame)
		{
			MainGame = mainGame;

			Rand = new Random();

			Level = new Level(this);
			UI = new UIScreen(this);

			Save = new SavedGame(); // will be loaded in a few tenths of a second
		}

		public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
		{
			if (UI.State == UIState.Office || UI.State == UIState.Laptop)
			{
				Level.Draw(gameTime, spriteBatch);
			}

			UI.Draw(gameTime, spriteBatch);
		}

		public void LoadContent(ContentManager content)
		{
			Save = SavedGame.Load();

			UI.LoadContent(content);
			Level.LoadContent(content);

			Level.VolumeController.SetVolume(Volume);
		}

		public void Update(GameTime gameTime, InputManager input)
		{
			UI.Update(gameTime, input);

			if (UI.State == UIState.Office || UI.State == UIState.Laptop)
			{
				Level.Update(gameTime, input);
			}

			if (input.IsKeyPressed(Keys.Delete))
			{
				System.Diagnostics.Debugger.Break();
			}
		}
	}
}

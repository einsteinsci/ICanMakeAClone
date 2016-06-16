using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Xenko.Audio;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone.ONAF2
{
	public enum UIState
	{
		MainMenu = 0,
		Survive,
		Office,
		Laptop,
		Static,
		YouDied,
		SixAM,
		TheEggnd,
		Newspaper,
		RareStartup,
	}

	public class UIScreen : IRetroComponent
	{
		public const int RECORD_STICKER_CENTER = 292;
		public const int STATIC_MAX_OFFSET = 728;
		public const int RARE_SCREEN_RARITY = 25;
		public const int RARE_SCREEN_COUNT = 3;
		public const int THE_EGGND_FRAMES_APART = 19;
		public const int THE_EGGND_FRAME_COUNT = 4;
		public const int STATIC_SCREEN_LENGTH = 4;
		public const int STATIC_SCREEN_COUNT = 10;

		public const float DEBUG_OFFSET = 50;
		public const float DEBUG_LINE_SPACING = 13;
		public const float SURVIVE_HARDBOILED_OFFSET = 45.0f;

		public const float RARE_STARTUP_TIME = 7.0f;
		public const float DEATH_STATIC_TIME = 5.0f;
		public const float SPAM_TIME = 6.5f;
		public const float SPAM_DELAY = 0.5f;
		public const float THE_EGGND_TIME = 25.0f;
		public const float SURVIVE_DELAY = 2.0f;
		public const float SURVIVE_FADETIME = 2.0f;
		public const float NEWSPAPER_FADEIN_TIME = 1.0f;
		public const float NEWSPAPER_FADEOUT_TIME = 3.0f;

		public const float SPAM_SPEED = 15.0f;

		public const float VOLUME_MIN_THRESHOLD = 0.02f;
		public const float VOLUME_MAX_THRESHOLD = 0.98f;


		public static readonly Vector2 MENU_OFFSET = new Vector2(40, 16);
		public static readonly Vector2 RECORD_PLAYER_OFFSET = new Vector2(576, 25);
		public static readonly Vector2 RECORD_STICKER_OFFSET = new Vector2(912, 371);
		public static readonly Vector2 RECORD_TIP_OFFSET = new Vector2(474, -44);
		public static readonly Vector2 VOLUME_OFFSET = new Vector2(273, 473);
		public static readonly Vector2 STAR_OFFSET = new Vector2(50, 115);
		public static readonly Vector2 SPAM_FINAL_OFFSET = new Vector2(587, 320);

		public static readonly Vector2 VOLUME_SIZE = new Vector2(224, 40);
		public static readonly Vector2 SPAM_SIZE = new Vector2(45, 75);

		public static readonly RectangleF VOLUME_INPUTBOX = new RectangleF(270, 475, 223, 35);
		public static readonly RectangleF STARTGAME_INPUTBOX = new RectangleF(50, 220, 270, 55);
		public static readonly RectangleF HARDBOILED_INPUTBOX = new RectangleF(50, 305, 460, 50);
		public static readonly RectangleF FULLSCREEN_INPUTBOX = new RectangleF(50, 385, 450, 55);

		public OnafMain Main
		{ get; private set; }

		public Random Rand => Main.Rand;

		public Vector2 WindowSize => Main.WindowSize;

		public UIState State
		{ get; private set; }

		public int RareStartupIndex
		{ get; private set; }

		public readonly List<string> DebugLines = new List<string>();
		public readonly List<string> DebugLinesAI = new List<string>();

		public bool ShowDebug
		{ get; set; }

		internal SpriteSheet miscScreens;
		internal SpriteSheet menuSprites;
		internal SpriteSheet staticBars;
		
		internal SpriteFont debugFont;

		internal SoundMusic menuMusic;
		internal SoundMusic theEggndMusic;
		internal SoundMusic newspaperMusic;

		internal SoundEffect soundStatic;
		
		internal readonly List<InputRegion> inputRegions = new List<InputRegion>();
		
		internal HelperTimer timer;

		private float _recordSpin;

		private UIState? _nextState;

		private float _spamOffset;
		private bool _doingSpam;

		private int _theEggndFrame;

		private int _chillbarOffset;

		private int _staticScreensLeft;
		private int _currentStaticScreen;

		private int _staticOffset;

		public UIScreen(OnafMain main)
		{
			Main = main;

			ShowDebug = true;

			_recordSpin = (float)Rand.NextDouble() * 100;

			SetStateNextFrame(UIState.MainMenu);

			_chillbarOffset = Rand.Next(0, (int)WindowSize.Y);
		}

		private void _volume_MouseDown(InputManager input)
		{
			float localClick = input.GetMousePosPx(WindowSize).X - VOLUME_INPUTBOX.X;
			float volume = localClick / VOLUME_INPUTBOX.Width;

			if (volume > VOLUME_MAX_THRESHOLD)
			{
				volume = 1.0f;
			}
			else if (volume < VOLUME_MIN_THRESHOLD)
			{
				volume = 0;
			}

			Main.Volume = volume;
		}

		public void SetState(UIState state)
		{
			UIState prev = State;
			State = state;

			if (State == UIState.MainMenu)
			{
				inputRegions.Clear();

				inputRegions.Add(new InputRegion(HARDBOILED_INPUTBOX, true, (i) => {
					if (!Main.HasWon)
					{
						return;
					}

					if (Rand.Next(RARE_SCREEN_RARITY) == 0)
					{
						SetStateNextFrame(UIState.RareStartup);
					}
					else
					{
						SetStateNextFrame(UIState.Survive);
					}

					Main.Level.IsHardBoiled = true;
				}));

				inputRegions.Add(new InputRegion(STARTGAME_INPUTBOX, true, (i) => {
					if (Rand.Next(RARE_SCREEN_RARITY) == 0)
					{
						SetStateNextFrame(UIState.RareStartup);
					}
					else
					{
						SetStateNextFrame(UIState.Survive);
					}

					Main.Level.IsHardBoiled = false;
				}));

				inputRegions.Add(new InputRegion(FULLSCREEN_INPUTBOX, true, (i) => {
					Main.MainGame.ToggleFullscreen();
				}));

				InputRegion volumeInput = new InputRegion(VOLUME_INPUTBOX);
				volumeInput.MouseDown += _volume_MouseDown;
				inputRegions.Add(volumeInput);

				menuMusic.Play();
			}
			else
			{
				menuMusic.Stop();
			}

			if (State == UIState.RareStartup)
			{
				inputRegions.Clear();

				RareStartupIndex = Rand.Next(0, RARE_SCREEN_COUNT) + 1;
				timer = new HelperTimer(TimeSpan.FromSeconds(RARE_STARTUP_TIME), "RareStartupEnd", () => { SetStateNextFrame(UIState.Survive); });
			}

			if (State == UIState.Survive)
			{
				inputRegions.Clear();

				_staticScreensLeft = STATIC_SCREEN_LENGTH;

				timer = new HelperTimer(TimeSpan.FromSeconds(SURVIVE_DELAY), "SurviveDelay", () => {
					timer = new HelperTimer(TimeSpan.FromSeconds(SURVIVE_FADETIME), "SurviveFade", () => { SetStateNextFrame(UIState.Office); });
				});

				Main.Level.Laptop.soundCameraChange.Play();
			}

			if (State == UIState.YouDied)
			{
				inputRegions.Clear();
				inputRegions.Add(new InputRegion(Vector2.Zero, WindowSize, true, (i) => { SetStateNextFrame(UIState.MainMenu); }));
			}

			if (State == UIState.Static)
			{
				inputRegions.Clear();

				timer = new HelperTimer(TimeSpan.FromSeconds(DEATH_STATIC_TIME), "DeathStaticEnd", () => { SetState(UIState.YouDied); });
				soundStatic.Play();
				Main.Level.Office.musicClassicalish.Stop();
			}
			else
			{
				soundStatic.Stop();
			}

			if (State == UIState.SixAM)
			{
				inputRegions.Clear();

				_spamOffset = SPAM_SIZE.Y;

				timer = new HelperTimer(TimeSpan.FromSeconds(SPAM_DELAY), "SpamBegin", () => {
					_doingSpam = true;
					timer = new HelperTimer(TimeSpan.FromSeconds(SPAM_TIME), "SpamEnd", () => {
						_doingSpam = false;
						if (Main.Level.IsHardBoiled)
						{
							SetState(UIState.Newspaper);
						}
						else
						{
							SetState(UIState.TheEggnd);
						}
					});
				});

				Main.Level.Office.musicClassicalish.Stop();
			}

			if (State == UIState.TheEggnd)
			{
				inputRegions.Clear();

				timer = new HelperTimer(TimeSpan.FromSeconds(THE_EGGND_TIME), "TheEggndTime", () => { SetState(UIState.MainMenu); });

				Main.Level.spamMusic.Stop();
				theEggndMusic.Play();
			}
			else
			{
				theEggndMusic.Stop();
			}

			if (State == UIState.Newspaper)
			{
				inputRegions.Clear();

				timer = new HelperTimer(TimeSpan.FromSeconds(NEWSPAPER_FADEIN_TIME), "NewspaperIn", () => {
					inputRegions.Add(new InputRegion(Vector2.Zero, WindowSize, true, (n) => {
						timer = new HelperTimer(TimeSpan.FromSeconds(NEWSPAPER_FADEOUT_TIME), "NewspaperOut", () => {
							SetStateNextFrame(UIState.MainMenu);
							newspaperMusic.Stop();
						});
					}));
				});

				newspaperMusic.Play();
			}

			if (State == UIState.Office)
			{
				inputRegions.Clear();

				if (prev != UIState.Laptop)
				{
					Main.Level.Reset();
				}
				else
				{
					Main.Level.Laptop.Hide();
					Main.Level.Office.Show();
				}
			}
			else
			{
				Main.Level.Hide();
			}

			if (State == UIState.Laptop)
			{
				inputRegions.Clear();

				if (prev != UIState.Office)
				{
					Main.Level.Reset(); // should never happen
				}
				else
				{
					Main.Level.Office.Hide();
					Main.Level.Laptop.Show();
				}
			}
		}

		public void SetStateNextFrame(UIState state)
		{
			_nextState = state;
		}

		#region drawscreens

		public void DrawRareStartup(SpriteBatch spriteBatch)
		{
			miscScreens["RareStartup" + RareStartupIndex].Draw(spriteBatch, Vector2.Zero);
		}

		public void DrawMainMenu(SpriteBatch spriteBatch)
		{
			// draw record player
			menuSprites["Record"].Draw(spriteBatch, RECORD_PLAYER_OFFSET);
			menuSprites["RecordSticker"].Draw(spriteBatch, RECORD_STICKER_OFFSET, _recordSpin);
			menuSprites["RecordTip"].Draw(spriteBatch, RECORD_TIP_OFFSET);

			menuSprites["MenuBase"].Draw(spriteBatch, MENU_OFFSET);
			RectangleF sourceRect = new RectangleF(0, 0, Main.Volume * VOLUME_SIZE.X, VOLUME_SIZE.Y);
			menuSprites["Volume"].Draw(spriteBatch, VOLUME_OFFSET, sourceRect, Color.White);
			if (Main.HasWon)
			{
				menuSprites["HardBoiledMode"].Draw(spriteBatch, HARDBOILED_INPUTBOX.TopLeft);
			}

			if (Main.HasWonHardboiled)
			{
				menuSprites["Star"].Draw(spriteBatch, STAR_OFFSET);
			}

			miscScreens["ChillBarSmall"].Draw(spriteBatch, new Vector2(0, _chillbarOffset));

			DrawNoise(spriteBatch, OnafMain.STATIC_OVERLAY_ALPHA);
		}

		public void DrawNoise(SpriteBatch spriteBatch, byte alpha, byte volume = 255)
		{
			Sprite noise = miscScreens["Static"];
			noise.Draw(spriteBatch, new Vector2(0, -_staticOffset), new Color(volume, volume, volume, alpha), Vector2.One);
		}

		public void DrawYouDied(SpriteBatch sb)
		{
			miscScreens["YouDied"].Draw(sb, Vector2.Zero);
			DrawNoise(sb, OnafMain.STATIC_OVERLAY_ALPHA);
		}

		public void DrawSixAM(SpriteBatch sb)
		{
			miscScreens["SixAM"].Draw(sb, Vector2.Zero);

			RectangleF sourceRect = new RectangleF(0, _spamOffset, SPAM_SIZE.X, SPAM_SIZE.Y - _spamOffset);
			miscScreens["Spam"].Draw(sb, new Vector2(SPAM_FINAL_OFFSET.X, SPAM_FINAL_OFFSET.Y + _spamOffset), sourceRect, Color.White);
		}

		public void DrawTheEggnd(SpriteBatch sb)
		{
			miscScreens["TheEggnd" + _theEggndFrame].Draw(sb, Vector2.Zero);
		}

		public void DrawNewspaper(SpriteBatch sb)
		{
			float opacity = (float)(timer.TimeLeft.TotalSeconds / timer.InitialTime.TotalSeconds);
			if (timer.Label == "NewspaperIn")
			{
				opacity = 1.0f - opacity;
			}

			miscScreens["Newspaper"].Draw(sb, Vector2.Zero, Util.MakeTransparency(opacity), Vector2.One);
		}

		public void DrawSurvive(SpriteBatch sb)
		{
			string text = "Survive until 6 AM";
			float x = Main.WindowCenter.X - Main.Level.hourFont.MeasureString(text).X / 2.0f;
			float y = Main.WindowCenter.Y - Main.Level.hourFont.MeasureString(text).Y / 2.0f;
			float alpha = 1.0f;
			if (timer != null && timer.Label == "SurviveFade")
			{
				alpha = (float)(timer.TimeLeft.TotalSeconds / timer.InitialTime.TotalSeconds);
			}
			sb.DrawString(Main.Level.hourFont, text, new Vector2(x, y), Util.MakeTransparency(alpha));

			text = "(Hard Boiled Mode)";
			y += SURVIVE_HARDBOILED_OFFSET;
			x = Main.WindowCenter.X - Main.Level.hourFont.MeasureString(text).X / 2.0f;
			sb.DrawString(Main.Level.hourFont, text, new Vector2(x, y), Util.MakeTransparency(alpha));
		}

		#endregion

		#region IRetroComponent

		public void Draw(GameTime gameTime, SpriteBatch sb)
		{
			switch (State)
			{
			case UIState.MainMenu:
				DrawMainMenu(sb);
				break;
			case UIState.Survive:
				DrawSurvive(sb);
				break;
			case UIState.YouDied:
				DrawYouDied(sb);
				break;
			case UIState.Static:
				DrawNoise(sb, 255);
				break;
			case UIState.SixAM:
				DrawSixAM(sb);
				break;
			case UIState.TheEggnd:
				DrawTheEggnd(sb);
				break;
			case UIState.Newspaper:
				DrawNewspaper(sb);
				break;
			case UIState.RareStartup:
				DrawRareStartup(sb);
				break;
			}

			if (_staticScreensLeft > 0)
			{
				staticBars[_currentStaticScreen.ToString()].Draw(sb, Vector2.Zero);
			}

			if (ShowDebug && debugFont != null)
			{
				float offset = DEBUG_OFFSET;
				foreach (string line in DebugLines)
				{
					float x = WindowSize.X - debugFont.MeasureString(line).Length() - DEBUG_LINE_SPACING;
					sb.DrawString(debugFont, line, new Vector2(x, offset), Color.White, TextAlignment.Right);
					offset += DEBUG_LINE_SPACING;
				}

				if (State.IsInGame())
				{
					offset = DEBUG_OFFSET;
					foreach (string line in DebugLinesAI)
					{
						sb.DrawString(debugFont, line, new Vector2(DEBUG_LINE_SPACING, offset), Color.White);
						offset += DEBUG_LINE_SPACING;
					}
				}
			}
		}

		public void LoadContent(ContentManager content)
		{
			debugFont = content.Load<SpriteFont>("DebugFont");

			miscScreens = content.Load<SpriteSheet>("ONAF2/MiscScreens");
			menuSprites = content.Load<SpriteSheet>("ONAF2/Menu");
			staticBars = content.Load<SpriteSheet>("ONAF2/StaticBars");

			soundStatic = content.LoadSoundEffect(Main.Level, "ONAF2/Sounds/Static", 1.0f, true);

			menuMusic = content.LoadMusic(Main.Level, "ONAF2/Music/MainMenu");
			theEggndMusic = content.LoadMusic(Main.Level, "ONAF2/Music/Credits");
			newspaperMusic = content.LoadMusic(Main.Level, "ONAF2/Music/Credits2");

			menuSprites["RecordSticker"].Center = new Vector2(RECORD_STICKER_CENTER);
		}

		public void Update(GameTime gameTime, InputManager input)
		{
			// Next frame state changes
			if (_nextState.HasValue)
			{
				SetState(_nextState.Value);
				_nextState = null;
			}

			if (gameTime.FrameCount % 2 == 0)
			{
				_staticOffset = Rand.Next(STATIC_MAX_OFFSET);
			}

			// Chill Bar
			if (_chillbarOffset > WindowSize.Y || Rand.Next(0, 800) == 0)
			{
				_chillbarOffset = Rand.Next(0, (int)WindowSize.Y);
			}
			else if (gameTime.FrameCount % 3 == 0)
			{
				_chillbarOffset++;
			}

			if (_staticScreensLeft > 0 && gameTime.FrameCount % 2 == 0)
			{
				_staticScreensLeft--;
				_currentStaticScreen = Rand.Next(STATIC_SCREEN_COUNT);
			}

			// Record Spin
			_recordSpin = (_recordSpin + 0.01f) % 100;

			// Spam
			if (_doingSpam && _spamOffset > 0)
			{
				_spamOffset = Math.Max(_spamOffset - SPAM_SPEED * (float)gameTime.Elapsed.TotalSeconds, 0);
			}

			// The Eggnd
			if (gameTime.FrameCount % THE_EGGND_FRAMES_APART == 0)
			{
				_theEggndFrame = (_theEggndFrame + 1) % THE_EGGND_FRAME_COUNT;
			}

			// Debug
			if (input.IsKeyPressed(Keys.RightShift))
			{
				ShowDebug = !ShowDebug;
			}

			DebugLines.Clear();

			Vector2 mousePos = input.GetMousePosPx(Main.WindowSize);
			DebugLines.Add("Mouse Pos: " + (int)mousePos.X + " " + (int)mousePos.Y);
			DebugLines.Add("");

			if (State == UIState.Office)
			{
				DebugLines.AddRange(Main.Level.Office.GetDebugLines());
			}
			else if (State == UIState.Laptop)
			{
				DebugLines.AddRange(Main.Level.Laptop.GetDebugLines());
			}
			else
			{
				DebugLines.Add("UI State: " + State);
				DebugLines.Add("UI Timer: " + (timer?.ToString() ?? "NULL"));
				DebugLines.Add("Volume: " + Main.Volume.ToString("F3"));
			}

			if (State.IsInGame())
			{
				DebugLines.AddRange(Main.Level.GetDebugLines());
			}

			DebugLinesAI.Clear();
			DebugLinesAI.Add("Type: " + Main.Level.Bot.SourceName);
			DebugLinesAI.AddRange(Main.Level.Bot.GetDebugLines());

			// Small stuff
			if (timer != null)
			{
				timer.Update(gameTime);
			}
			
			foreach (InputRegion r in inputRegions)
			{
				r.Update(input, WindowSize);
			}

			// Screen Changes
			if (!State.IsInGame())
			{
				if (input.IsKeyPressed(Keys.K))
				{
					SetState(UIState.Static);
				}
				if (input.IsKeyPressed(Keys.M))
				{
					SetState(UIState.MainMenu);
				}
				if (input.IsKeyPressed(Keys.U))
				{
					SetState(UIState.RareStartup);
				}
				if (input.IsKeyPressed(Keys.I))
				{
					SetState(UIState.YouDied);
				}
				if (input.IsKeyPressed(Keys.D6))
				{
					SetState(UIState.SixAM);
				}
				if (input.IsKeyPressed(Keys.N))
				{
					SetState(UIState.Newspaper);
				}
			}
		}

		#endregion
	}
}

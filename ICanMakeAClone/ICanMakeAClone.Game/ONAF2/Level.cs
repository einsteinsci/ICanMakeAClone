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
	public class Level : IRetroComponent
	{
		public const int EXPOSURE_SHAKE_MAX = 5;
		public const int FLIPUP_ALPHA = 100;

		public const float LAPTOP_BATTERY_TIME = 7.0f;
		public const float SECONDS_PER_HOUR = 90.0f;
		public const float SHAKE_SPEED = 90.0f;
		public const float FLIPUP_THRESHOLD = 620.0f;
		public const float SPAM_FADE_TIME = 1.0f;
		public const float HARDBOILED_EXPOSURE_MULTIPLIER = 2.0f;

		public const string BATTERY_DEFAULT_TEXT = "LAPTOP BATTERY";
		public const string BATTERY_CHARGE_TEXT = "CHARGING...";
		public const string BATTERY_CHARGE_OFF_TEXT = "CHARGE OFF";
		public const string EXPOSURE_TEXT = "EXPOSURE";

		public static readonly float VICTORY_TIME = SECONDS_PER_HOUR * 6;
		public static readonly double JUMPSCARE_SHAKE_RANGE = 60;
		public static readonly double JUMPSCARE_SHAKE_MIN = JUMPSCARE_SHAKE_RANGE / -2.0;

		public static readonly Vector2 BATTERY_OFFSET = new Vector2(20, 660);
		public static readonly Vector2 BATTERY_TEXT_OFFSET = new Vector2(20, 635);
		public static readonly Vector2 BATTERY_BAR_OFFSET = new Vector2(26, 666);
		public static readonly Vector2 BATTERY_REDMAN_OFFSET = new Vector2(90, 666);

		public static readonly Vector2 EXPOSURE_OFFSET = new Vector2(1088, 660);
		public static readonly Vector2 EXPOSURE_TEXT_OFFSET = new Vector2(1165, 635);
		public static readonly Vector2 EXPOSURE_BAR_OFFSET = new Vector2(1094, 666);

		public static readonly Vector2 BAR_SIZE = new Vector2(160, 24);

		public static readonly Vector2 FLIPUP_OFFSET = new Vector2(391, 657);
		public static readonly Vector2 REDMAN_OFFICE_WARNING_OFFSET = new Vector2(900, 650);

		public static readonly Color BATTERY_CHARGED_COLOR = new Color(80, 240, 94);
		public static readonly Color BATTERY_OUT_COLOR = new Color(180, 180, 180);
		public static readonly Color BATTERY_CHARGE_OFF_COLOR = new Color(255, 96, 96);
		public static readonly Color EXPOSED_COLOR = new Color(255, 230, 0);

		public OnafMain Main
		{ get; private set; }

		public UIScreen UI => Main.UI;

		public Random Rand => Main.Rand;

		public Laptop Laptop
		{ get; private set; }

		public Office Office
		{ get; private set; }

		public float LaptopBattery
		{ get; set; }

		public float Exposure
		{ get; set; }

		public float Time
		{ get; private set; }

		public bool HasWon
		{ get; private set; }

		public SoundVolumeController VolumeController
		{ get; private set; }

		public MonsterManager Monsters
		{ get; private set; }

		public bool IsJumpscaring
		{ get; set; }

		public bool IsHardBoiled
		{ get; set; }

		public string TimeShown
		{
			get
			{
				if (Time < SECONDS_PER_HOUR)
				{
					return "12 AM";
				}

				int hour = (int)(Time / SECONDS_PER_HOUR);
				return hour + " AM";
			}
		}

		public DateTime FakeTime
		{
			get
			{
				DateTime januaryFirst = new DateTime(2016, 1, 1);
				return januaryFirst.Add(TimeSinceMidnight);
			}
		}

		public TimeSpan TimeSinceMidnight
		{
			get
			{
				float hoursSinceMidnight = Time / SECONDS_PER_HOUR;
				return TimeSpan.FromHours(hoursSinceMidnight);
			}
		}

		internal bool CHEAT_InfiniteExposure
		{ get; private set; }

		internal bool CHEAT_InfiniteBattery
		{ get; private set; }

		internal bool CHEAT_MapDebug
		{ get; private set; }

		internal bool CHEAT_MonstersStayPut
		{ get; private set; }

		internal bool CHEAT_OwlInvincibility
		{ get; private set; }

		internal SpriteFont hourFont;
		internal SpriteFont uiFont;
		internal SpriteFont patienceFont;

		internal SpriteSheet gameUISprites;

		internal SoundEffect exposureUpSound;

		internal SoundMusic spamMusic;
		
		internal Vector2 jumpscareShakeOffset;

		private int _exposureShakeOffset;
		private bool _flipUpEnabled;

		private bool _isMouseLingering;

		private float _spamFadeTime;

		public Level(OnafMain main)
		{
			Main = main;

			Laptop = new Laptop(this);
			Office = new Office(this);

			Monsters = new MonsterManager(this);

			LaptopBattery = 1.0f;

			VolumeController = new SoundVolumeController();

			CHEAT_InfiniteExposure = true;
			CHEAT_InfiniteBattery = true;
			CHEAT_MapDebug = true;
			CHEAT_MonstersStayPut = false;
			CHEAT_OwlInvincibility = true;
		}
		
		public void Reset()
		{
			Office.Reset();
			Laptop.Reset();

			Monsters.Reset();

			Time = 0;
			LaptopBattery = 1.0f;
			Exposure = 0.0f;

			_flipUpEnabled = true;

			HasWon = false;
			_spamFadeTime = 0;

			IsJumpscaring = false;
			jumpscareShakeOffset = Vector2.Zero;
		}

		public void Hide()
		{
			Office.Hide();
			Laptop.Hide();
			_flipUpEnabled = false;
		}

		public List<string> GetDebugLines()
		{
			List<string> res = new List<string>();

			res.Add("Time: " + Main.Level.FakeTime.ToString("hh:mm:ss") + " (" +
					Main.Level.Time.ToString("F0") + ")");
			if (Monsters.Clown.IsActive)
			{
				res.Add("Patience: " + Monsters.Clown.Patience);
			}

			res.Add("Next Static in: " + Laptop.timeUntilNextStatic.ToString("F3"));

			string active = "Active: ";
			foreach(MonsterBase monster in Monsters.Monsters)
			{
				if (monster.IsActive)
				{
					active += monster.Name + " ";
				}
			}
			res.Add(active.Trim());

			if (CHEAT_InfiniteExposure || CHEAT_InfiniteBattery || CHEAT_MapDebug || CHEAT_MonstersStayPut || CHEAT_OwlInvincibility)
			{
				res.Add("");
				res.Add("CHEATS:");
			}

			if (CHEAT_InfiniteExposure)
			{
				res.Add("Infinite Exposure (F7)");
			}
			if (CHEAT_InfiniteBattery)
			{
				res.Add("Infinite Battery (F8)");
			}
			if (CHEAT_MapDebug)
			{
				res.Add("Map Debug (F9)");
			}
			if (CHEAT_MonstersStayPut)
			{
				res.Add("Monsters Stay Put (F10)");
			}
			if (CHEAT_OwlInvincibility)
			{
				res.Add("Owl Invincibility (F11)");
			}
			
			return res;
		}

		public void LoadContent(ContentManager content)
		{
			Office.LoadContent(content);
			Laptop.LoadContent(content);

			Monsters.LoadContent(content);

			hourFont = content.Load<SpriteFont>("GameFontLarge");
			uiFont = content.Load<SpriteFont>("GameFontMed");
			patienceFont = content.Load<SpriteFont>("GameFontXL");

			exposureUpSound = content.LoadSoundEffect(this, "ONAF2/Sounds/AwarenessIncrease", 0.7f, true);

			spamMusic = content.LoadMusic(this, "ONAF2/Music/SPAM");

			gameUISprites = content.Load<SpriteSheet>("ONAF2/GameUI");
		}

		public void Update(GameTime gt, InputManager input)
		{
			if (HasWon)
			{
				_spamFadeTime += (float)gt.Elapsed.TotalSeconds;

				if (_spamFadeTime >= SPAM_FADE_TIME)
				{
					Main.UI.SetState(UIState.SixAM);
				}
				return;
			}

			Office.Update(gt, input);
			Laptop.Update(gt, input);

			Monsters.Update(gt, input);

			// Cheats
			if (input.IsKeyPressed(Keys.D6))
			{
				Time = VICTORY_TIME;
			}

			if (input.IsKeyPressed(Keys.F7))
			{
				CHEAT_InfiniteExposure = !CHEAT_InfiniteExposure;
			}

			if (input.IsKeyPressed(Keys.F8))
			{
				CHEAT_InfiniteBattery = !CHEAT_InfiniteBattery;
			}

			if (input.IsKeyPressed(Keys.F9))
			{
				CHEAT_MapDebug = !CHEAT_MapDebug;
			}

			if (input.IsKeyPressed(Keys.F10))
			{
				CHEAT_MonstersStayPut = !CHEAT_MonstersStayPut;
			}

			if (input.IsKeyPressed(Keys.F11))
			{
				CHEAT_OwlInvincibility = !CHEAT_OwlInvincibility;
			}

			if (_flipUpEnabled && input.GetMousePosPx(Main.WindowSize).Y >= FLIPUP_THRESHOLD && !_isMouseLingering && !IsJumpscaring)
			{
				Laptop.ToggleLaptop();
				_isMouseLingering = true;
			}
			
			if (input.GetMousePosPx(Main.WindowSize).Y < FLIPUP_THRESHOLD && _isMouseLingering && !IsJumpscaring)
			{
				_isMouseLingering = false;
			}

			if (!IsJumpscaring)
			{
				Time += (float)gt.Elapsed.TotalSeconds;
			}
			else
			{
				if (gt.FrameCount % 2 == 0)
				{
					jumpscareShakeOffset = new Vector2((float)((Rand.NextDouble() * JUMPSCARE_SHAKE_RANGE) + JUMPSCARE_SHAKE_MIN), 
						(float)((Rand.NextDouble() * JUMPSCARE_SHAKE_RANGE) + JUMPSCARE_SHAKE_MIN));
				}

				if (Main.UI.State == UIState.Laptop)
				{
					Laptop.ToggleLaptop();
				}
			}
			
			if (Monsters.IsExposed)
			{
				_exposureShakeOffset = (int)(Math.Sin(gt.Total.TotalSeconds * SHAKE_SPEED) * EXPOSURE_SHAKE_MAX);
			}

			if (Exposure >= 0.95f && CHEAT_InfiniteExposure)
			{
				Exposure = 0; // Reset exposure at 95%
			}

			if (Monsters.IsExposed && Exposure >= 1.0f && !CHEAT_InfiniteExposure)
			{
				IsJumpscaring = true;
				Monsters.StartJumpscareFromExposure();
				exposureUpSound.Stop();
			}

			if (Time >= VICTORY_TIME && !IsJumpscaring)
			{
				HasWon = true;
				spamMusic.Play();

				Main.HasWon = true;

				if (IsHardBoiled)
				{
					Main.HasWonHardboiled = true;
				}
			}

			_flipUpEnabled = false;
			if (!Laptop.IsLaptopSwitching)
			{
				if (Main.UI.State == UIState.Laptop || Office.IsLightOn)
				{
					_flipUpEnabled = true;
				}
			}

			if (Main.UI.State == UIState.Office && Office.IsLightOn)
			{
				const float CHARGE_SPEED = 1.0f / LAPTOP_BATTERY_TIME;
				LaptopBattery = Math.Min(LaptopBattery + CHARGE_SPEED * (float)gt.Elapsed.TotalSeconds, 1.0f);
			}
		}

		public void Draw(GameTime gameTime, SpriteBatch sb)
		{
			Office.Draw(gameTime, sb);

			if (IsJumpscaring)
			{
				Monsters.DrawJumpscares(sb);
			}

			Laptop.Draw(gameTime, sb);

			float x = Main.WindowSize.X - hourFont.MeasureString(TimeShown).Length() - 10;
			sb.DrawString(hourFont, TimeShown, new Vector2(x, 10), Color.White);

			string batteryText = BATTERY_DEFAULT_TEXT;
			Color batteryColor = Color.White;

			if (Monsters.Redman.IsRedScreenOfDeathUp)
			{
				batteryColor = BATTERY_CHARGE_OFF_COLOR;
			}
			else if (LaptopBattery >= 1.0f)
			{
				batteryColor = BATTERY_CHARGED_COLOR;
			}
			else if (LaptopBattery <= 0.0f || (Laptop.IsRebooting && Main.UI.State == UIState.Laptop))
			{
				batteryColor = BATTERY_OUT_COLOR;
			}
			else
			{
				if (!Office.IsLightOn)
				{
					batteryColor = BATTERY_CHARGE_OFF_COLOR;
					batteryText = BATTERY_CHARGE_OFF_TEXT;
				}
				else if (Main.UI.State == UIState.Office)
				{
					batteryText = BATTERY_CHARGE_TEXT;
				}
			}

			bool goldenJumpscare = Monsters.GoldenFlumpty != null && Monsters.GoldenFlumpty.IsJumpscaring;

			if (!goldenJumpscare)
			{
				gameUISprites["Battery"].Draw(sb, BATTERY_OFFSET, batteryColor, Vector2.One);

				RectangleF sourceRect = new RectangleF(0, 0, LaptopBattery * BAR_SIZE.X, BAR_SIZE.Y);

				if (Monsters.Redman.IsRedScreenOfDeathUp)
				{
					gameUISprites["BatteryRedman"].Draw(sb, BATTERY_REDMAN_OFFSET);
				}
				else
				{
					gameUISprites["Bar"].Draw(sb, BATTERY_BAR_OFFSET, sourceRect, batteryColor);
					sb.DrawString(uiFont, batteryText, BATTERY_TEXT_OFFSET, batteryColor);
				}

				Color exposureColor = Color.White;
				if (Monsters.IsExposed)
				{
					exposureColor = EXPOSED_COLOR;
				}

				Vector2 shakeOffset = new Vector2(0, Monsters.IsExposed ? _exposureShakeOffset : 0);

				gameUISprites["Exposure"].Draw(sb, EXPOSURE_OFFSET + shakeOffset, exposureColor, Vector2.One);
				sourceRect = new RectangleF(0, 0, Exposure * BAR_SIZE.X, BAR_SIZE.Y);
				gameUISprites["Bar"].Draw(sb, EXPOSURE_BAR_OFFSET + shakeOffset, sourceRect, exposureColor);
				sb.DrawString(uiFont, EXPOSURE_TEXT, EXPOSURE_TEXT_OFFSET + shakeOffset, exposureColor);

				if (_flipUpEnabled)
				{
					gameUISprites["CamFlipUp"].Draw(sb, FLIPUP_OFFSET, Util.MakeTransparency(FLIPUP_ALPHA), Vector2.One);
				}

				if (Monsters.Redman.IsVirusUp && Monsters.Redman.warningIconVisible && UI.State == UIState.Office)
				{
					gameUISprites["RedmanWarning"].Draw(sb, REDMAN_OFFICE_WARNING_OFFSET);
				}
			}

			if (HasWon)
			{
				Color transparency = Util.MakeTransparency(_spamFadeTime / SPAM_FADE_TIME);
				Laptop.miscScreens["SixAM"].Draw(sb, Vector2.Zero, transparency, Vector2.One);
			}
		}
	}
}

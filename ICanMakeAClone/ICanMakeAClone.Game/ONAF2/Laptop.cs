using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
	public enum CameraIndex
	{
		Cam1 = 1, // Eyesaur home
		Cam2, // Grunklefuss home
		Cam3, // Owl home
		Cam4, // Purple vent
		Cam5, // Orange vent
		Cam6, // Right corridor
		Cam7 // Left corridor
	}

	public class Laptop : IRetroComponent
	{
		public const int LAPTOP_FLIPUP_FRAMES = 6;
		public const int POWERDOWN_FRAMES = 5;

		public const float REBOOT_TIME = 4.0f;
		public const float ROOM_ICON_TIME = 0.9f;
		public const float YOU_ICON_TIME = 0.5f;
		public const float RANDOM_STATIC_TIME_MIN = 4.0f;
		public const float RANDOM_STATIC_TIME_MAX = 8.0f;

		public static readonly Vector2 WINDOW_OFFSET = new Vector2(41, 78);
		public static readonly Vector2 WINDOW_CENTER_OFFSET = new Vector2(640, 378.5f);
		public static readonly Vector2 REBOOT_BAR_OFFSET = new Vector2(482, 452);
		public static readonly Vector2 MAP_OFFSET = new Vector2(845, 285);
		public static readonly Vector2 MAP_TEXT_OFFSET = new Vector2(5, 2);
		public static readonly Vector2 YOU_ICON_OFFSET = new Vector2(1015, 615);

		public static readonly Vector2 REBOOT_BAR_SIZE = new Vector2(316, 26);
		public static readonly Vector2 ROOM_INPUT_SIZE = new Vector2(58, 42);

		public static readonly Dictionary<CameraIndex, Vector2> MAP_CAMERA_OFFSETS = new Dictionary<CameraIndex, Vector2>() {
			{ CameraIndex.Cam1, new Vector2(0, 68) },
			{ CameraIndex.Cam2, new Vector2(324, 68) },
			{ CameraIndex.Cam3, new Vector2(162, 140) },
			{ CameraIndex.Cam4, new Vector2(115, 194) },
			{ CameraIndex.Cam5, new Vector2(209, 194) },
			{ CameraIndex.Cam6, new Vector2(4, 271) },
			{ CameraIndex.Cam7, new Vector2(320, 271) }
		};

		public static readonly CameraIndex[] CAMERAS_WITH_PANNING = new CameraIndex[] {
			CameraIndex.Cam1, CameraIndex.Cam2, CameraIndex.Cam6, CameraIndex.Cam7 };

		public UIScreen UI => Level.Main.UI;

		public Level Level
		{ get; private set; }

		public CameraIndex ActiveCamera
		{ get; set; }

		public float RebootProgress
		{ get; set; }

		public bool IsLaptopSwitching
		{ get; private set; }

		public bool IsRebooting
		{ get; private set; }
		
		internal bool CHEAT_InfiniteBattery
		{ get; private set; }

		internal float timeUntilNextStatic
		{ get; private set; }

		internal Dictionary<CameraIndex, InputRegion> roomInputs = new Dictionary<CameraIndex, InputRegion>();

		internal SpriteSheet miscScreens => Level.Main.UI.miscScreens;
		internal SpriteSheet gameUI => Level.gameUISprites;
		internal SpriteSheet staticBars => Level.Main.UI.staticBars;

		internal SoundEffect soundStatic => Level.Main.UI.soundStatic;

		internal SpriteSheet laptopSprites;
		internal SpriteSheet cameraRoomSprites;
		internal SpriteSheet noiseBars;
		internal SpriteSheet powerDownSprites;

		internal SpriteFont retroFontSmall;

		internal SoundEffect soundLaptopAmbient;
		internal SoundEffect soundLaptopOpen;
		internal SoundEffect soundLaptopClose;
		internal SoundEffect soundCameraChange;
		internal SoundEffect soundLaptopPowerdown;

		internal SpriteAnimation laptopAnim;

		internal readonly Dictionary<CameraIndex, CameraOffsetState> cameraOffsets;

		private bool _updateRoomInputs;

		private int _laptopFrame;
		private UIState _fromState;

		private int _powerDownFrame;

		private bool _needsRebooting;

		private int _chillBarOffset;
		
		private int _staticScreensLeft;
		private int _currentStaticScreen;

		private float _timeUntilRoomIconSwitches;
		private bool _roomIconBright = true;

		private float _timeUntilYouIconSwitches;
		private bool _youIconBright = true;
		private bool _isStaticIntermittent;

		public Laptop(Level owner)
		{
			Level = owner;
			RebootProgress = 1.0f;

			CHEAT_InfiniteBattery = true;

			cameraOffsets = new Dictionary<CameraIndex, CameraOffsetState>();
			for (CameraIndex ci = CameraIndex.Cam1; ci < (CameraIndex)8; ci++)
			{
				// IsEnabled is meaningless here. Instead I will use _updateRoomInputs to handle all at once.
				CameraIndex clone = ci; // Otherwise ci is passed by reference into lambda
				roomInputs.Add(ci, new InputRegion(MAP_OFFSET + MAP_CAMERA_OFFSETS[ci], ROOM_INPUT_SIZE, true, (i) => {
					if (ActiveCamera == clone)
					{
						return;
					}

					ActiveCamera = clone;
					_staticScreensLeft = UIScreen.STATIC_SCREEN_LENGTH;
					_currentStaticScreen = Level.Rand.Next(0, UIScreen.STATIC_SCREEN_COUNT);
					_isStaticIntermittent = false;

					_timeUntilRoomIconSwitches = ROOM_ICON_TIME;
					_roomIconBright = true;

					soundCameraChange.Stop();
					soundCameraChange.Play();
				}));

				cameraOffsets.Add(clone, new CameraOffsetState(Level.Rand));
			}
		}

		private SoundEffect _loadSoundEffect(ContentManager content, string path, float volume = 1.0f, bool looped = false)
		{
			SoundEffect res = content.Load<SoundEffect>(path);
			res.Volume = volume * Level.Main.Volume;
			res.IsLooped = looped;
			Level.VolumeController.Register(res, volume);

			return res;
		}

		public void Reboot()
		{
			RebootProgress = 0.0f;
			IsRebooting = true;
			_needsRebooting = false;
		}

		public void Reset()
		{
			RebootProgress = 1.0f;
			IsRebooting = false;

			ActiveCamera = CameraIndex.Cam3;

			_fromState = UIState.Office;
			_laptopFrame = 0;

			_powerDownFrame = 0;

			_needsRebooting = false;

			_chillBarOffset = Level.Rand.Next(0, (int)Level.Main.WindowSize.Y);
			timeUntilNextStatic = RANDOM_STATIC_TIME_MAX;
		}

		public void Show()
		{
			_updateRoomInputs = true;
		}

		public void Hide()
		{
			_updateRoomInputs = false;
		}

		public void ToggleLaptop()
		{
			IsLaptopSwitching = true;

			if (UI.State == UIState.Laptop)
			{
				Level.Main.UI.SetState(UIState.Office);
				_staticScreensLeft = 0;

				soundLaptopClose.Play();
				soundLaptopAmbient.Stop();

				Level.Office.OnLaptopDown();
			}
			else
			{
				_staticScreensLeft = UIScreen.STATIC_SCREEN_LENGTH;

				_timeUntilRoomIconSwitches = ROOM_ICON_TIME;
				_roomIconBright = true;

				_timeUntilYouIconSwitches = YOU_ICON_TIME;
				_youIconBright = true;
				
				soundLaptopOpen.Play();
				soundLaptopAmbient.Play();
			}

			_chillBarOffset = Level.Rand.Next(0, (int)Level.Main.WindowSize.Y);
		}

		public List<string> GetDebugLines()
		{
			List<string> res = new List<string>();

			res.Add("Chill Bar Pos: " + _chillBarOffset);
			res.Add("Current Room: " + ActiveCamera);
			res.Add("Battery Left: " + Level.LaptopBattery.ToString("F2"));
			res.Add("");

			return res;
		}

		public void LoadContent(ContentManager content)
		{
			retroFontSmall = content.Load<SpriteFont>("ONAF2/RetroFontSmall");

			cameraRoomSprites = content.Load<SpriteSheet>("ONAF2/Cameras");
			noiseBars = content.Load<SpriteSheet>("ONAF2/StaticBars");
			powerDownSprites = content.Load<SpriteSheet>("ONAF2/LaptopPowerDown");

			laptopSprites = content.Load<SpriteSheet>("ONAF2/Laptop");

			soundLaptopAmbient = _loadSoundEffect(content, "ONAF2/Sounds/LaptopAmbient", 0.4f, true);
			soundLaptopOpen = _loadSoundEffect(content, "ONAF2/Sounds/LaptopOpen", 0.4f);
			soundLaptopClose = _loadSoundEffect(content, "ONAF2/Sounds/LaptopClose", 0.3f);
			soundCameraChange = _loadSoundEffect(content, "ONAF2/Sounds/LaptopSwitchCam", 0.5f);
			soundLaptopPowerdown = _loadSoundEffect(content, "ONAF2/Sounds/LaptopShutdown");

			laptopAnim = new SpriteAnimation(laptopSprites, new Vector2(0, 624),
				new Vector2(0, 558),
				new Vector2(0, 282),
				Vector2.Zero);
		}

		public void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.F8))
			{
				CHEAT_InfiniteBattery = !CHEAT_InfiniteBattery;
			}

			if (_updateRoomInputs && !Level.IsJumpscaring)
			{
				foreach (var kvp in roomInputs)
				{
					kvp.Value.Update(input, Level.Main.WindowSize);
				}
			}

			float elapsed = (float)gt.Elapsed.TotalSeconds;

			if (gt.FrameCount % 2 == 0)
			{
				_chillBarOffset++;

				if (_chillBarOffset >= Level.Main.WindowSize.Y || Level.Rand.Next(0, 800) == 0)
				{
					_chillBarOffset = Level.Rand.Next(0, (int)Level.Main.WindowSize.Y);
				}

				if (_staticScreensLeft > 0 && !Level.IsJumpscaring)
				{
					_currentStaticScreen = Level.Rand.Next(0, UIScreen.STATIC_SCREEN_COUNT);
					_staticScreensLeft--;
				}
				else
				{
					soundStatic.Stop();
				}
			}

			_timeUntilYouIconSwitches -= elapsed;
			if (_timeUntilYouIconSwitches <= 0)
			{
				_timeUntilYouIconSwitches = YOU_ICON_TIME;
				_youIconBright = !_youIconBright;
			}

			_timeUntilRoomIconSwitches -= elapsed;
			if (_timeUntilRoomIconSwitches <= 0)
			{
				_timeUntilRoomIconSwitches = ROOM_ICON_TIME;
				_roomIconBright = !_roomIconBright;
			}

			timeUntilNextStatic -= elapsed;
			if (timeUntilNextStatic <= 0 && !Level.IsJumpscaring)
			{
				float width = RANDOM_STATIC_TIME_MAX - RANDOM_STATIC_TIME_MIN;
				timeUntilNextStatic = (float)Level.Rand.NextDouble() * width + RANDOM_STATIC_TIME_MIN;

				_currentStaticScreen = Level.Rand.Next(0, UIScreen.STATIC_SCREEN_COUNT);
				_staticScreensLeft = UIScreen.STATIC_SCREEN_LENGTH;
				_isStaticIntermittent = true;

				Level.Monsters.OnStatic();

				if (UI.State == UIState.Laptop)
				{
					soundStatic.Play();
				}
			}
			
			foreach (KeyValuePair<CameraIndex, CameraOffsetState> kvp in cameraOffsets)
			{
				kvp.Value.Update(gt);
			}

			if (Level.LaptopBattery <= 0.0f && _powerDownFrame < POWERDOWN_FRAMES && gt.FrameCount % 2 == 0)
			{
				if (_powerDownFrame == 0)
				{
					_needsRebooting = true;
					soundLaptopPowerdown.Play();
				}

				_powerDownFrame++;
			}
			
			if (Level.LaptopBattery > 0.0f)
			{
				_powerDownFrame = 0;
			}

			if (UI.State == UIState.Laptop && !IsRebooting && !CHEAT_InfiniteBattery)
			{
				float drainSpeed = 1.0f / Level.LAPTOP_BATTERY_TIME;
				Level.LaptopBattery = Math.Max(Level.LaptopBattery - drainSpeed * elapsed, 0.0f);
			}

			if (IsRebooting)
			{
				float rebootSpeed = 1.0f / REBOOT_TIME;
				RebootProgress = Math.Min(RebootProgress + rebootSpeed * elapsed, 1.0f);

				if (RebootProgress >= 1.0f)
				{
					IsRebooting = false;
				}
			}

			if (IsLaptopSwitching && gt.FrameCount % 2 == 0)
			{
				if (_fromState == UIState.Laptop)
				{
					_laptopFrame--;
				}
				else
				{
					_laptopFrame++;
				}

				if (_laptopFrame == 0)
				{
					_fromState = UIState.Office;
					IsLaptopSwitching = false;
				}
				else if (_laptopFrame == LAPTOP_FLIPUP_FRAMES - 1)
				{
					_fromState = UIState.Laptop;
					IsLaptopSwitching = false;
					Level.Main.UI.SetStateNextFrame(UIState.Laptop);

					if (_needsRebooting)
					{
						Reboot();
					}

					Level.Monsters.GoldenFlumpty.Shoo();
				}
			}
		}

		public void Draw(GameTime gt, SpriteBatch sb)
		{
			if (_laptopFrame == 0)
			{
				return;
			}

			if (_laptopFrame < LAPTOP_FLIPUP_FRAMES - 1)
			{
				laptopAnim.Draw(sb, _laptopFrame - 1, Vector2.Zero);
			}

			if (UI.State != UIState.Laptop)
			{
				return;
			}

			if (Level.LaptopBattery <= 0.0f)
			{
				if (_powerDownFrame < POWERDOWN_FRAMES)
				{
					powerDownSprites[_powerDownFrame].Draw(sb, Vector2.Zero);
				}
			}
			else if (Level.Monsters.Redman.IsRedScreenOfDeathUp)
			{
				miscScreens["RedScreenOfDeath"].Draw(sb, Vector2.Zero);
			}
			else if (IsRebooting)
			{
				miscScreens["EggOS"].Draw(sb, Vector2.Zero);

				RectangleF sourceRect = new RectangleF(0, 0, REBOOT_BAR_SIZE.X * RebootProgress, REBOOT_BAR_SIZE.Y);
				miscScreens["RebootBar"].Draw(sb, REBOOT_BAR_OFFSET, sourceRect, Color.White);

				miscScreens["LaptopVignette"].Draw(sb, Vector2.Zero);
			}
			else
			{
				if (_isStaticIntermittent && _staticScreensLeft > 0)
				{
					UI.DrawNoise(sb, 255);
				}
				else
				{
					Vector2 offset = WINDOW_OFFSET;
					if (CAMERAS_WITH_PANNING.Contains(ActiveCamera))
					{
						offset += Vector2.UnitX * cameraOffsets[ActiveCamera].CameraOffset;
					}

					cameraRoomSprites[ActiveCamera.ToString()].Draw(sb, offset);

					Level.Monsters.DrawOnCamera(sb, offset, ActiveCamera);
				}

				UI.DrawNoise(sb, OnafMain.STATIC_OVERLAY_ALPHA);
				miscScreens["ChillBar"].Draw(sb, new Vector2(0, _chillBarOffset));

				if (_staticScreensLeft > 0)
				{
					staticBars[_currentStaticScreen].Draw(sb, Vector2.Zero);
				}

				miscScreens["Camera"].Draw(sb, Vector2.Zero);
				miscScreens["LaptopVignette"].Draw(sb, Vector2.Zero);

				gameUI["Map"].Draw(sb, MAP_OFFSET);

				string icon = _roomIconBright ? "CamSelectedLight" : "CamSelectedDark";
				gameUI[icon].Draw(sb, MAP_OFFSET + MAP_CAMERA_OFFSETS[ActiveCamera]);

				icon = _youIconBright ? "YouSmall" : "YouLarge";
				gameUI[icon].Draw(sb, YOU_ICON_OFFSET);

				foreach (KeyValuePair<CameraIndex, Vector2> kvp in MAP_CAMERA_OFFSETS)
				{
					string str = "CAM\n" + ((int)kvp.Key);
					sb.DrawString(retroFontSmall, str, kvp.Value + MAP_OFFSET + MAP_TEXT_OFFSET, Color.White);
				}

				Level.Monsters.DrawOnLaptop(sb);
			}
		}
	}
}

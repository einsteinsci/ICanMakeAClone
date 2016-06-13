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
	public enum VentState
	{
		Left,
		Right
	}

	public class Office : IRetroComponent
	{
		public const int RECORD_FRAMES = 58;
		public const int CREEPY_PIGLET_RARITY = 30;

		public const float MAX_CAMERA_OFFSET = -220.0f;
		public const float SIDE_PAN_WIDTH = 425.0f;
		public const float SIDE_PAN_SPEED = 15f;
		public const float JUMPSCARE_PAN_SPEED = 15f;

		public static readonly Vector2 ENTRY_OFFSET = new Vector2(604, 86);
		public static readonly Vector2 VENT_OFFSET = new Vector2(262, 12);
		public static readonly Vector2 BUTTON_LEFT_OFFSET = new Vector2(18, 303);
		public static readonly Vector2 BUTTON_RIGHT_OFFSET = new Vector2(1427, 303);
		public static readonly Vector2 BUTTON_DARK_LEFT_OFFSET = new Vector2(0, 251);
		public static readonly Vector2 BUTTON_DARK_RIGHT_OFFSET = new Vector2(1370, 251);
		public static readonly Vector2 WORMHOLE_OFFSET = new Vector2(179, 217);
		public static readonly Vector2 CREEPY_PIGLET_OFFSET = new Vector2(1197, 230);
		public static readonly Vector2 RECORD_OFFSET = new Vector2(270, 510);
		public static readonly Vector2 RAWR_HITBOX_OFFSET = new Vector2(1355, 85);

		public static readonly Vector2 BUTTON_SIZE = new Vector2(59, 99);
		public static readonly Vector2 RAWR_HITBOX_SIZE = new Vector2(100, 100);

		public static readonly RectangleF LIGHTSWITCH_INPUTBOX = new RectangleF(690, 475, 125, 150);
		public static readonly RectangleF MUTE_INTRO_INPUTBOX = new RectangleF(20, 20, 90, 55);
		
		public Level Level
		{ get; private set; }

		public UIState State => Level.Main.UI.State;

		public Vector2 WindowSize => Level.Main.WindowSize;

		public Vector2 CameraOffset
		{ get; private set; }

		public bool IsLightOn
		{ get; set; }

		public bool IsWormholeOpen => Level.Monsters.Clown.IsActive;

		public bool IsPigletCreepier
		{ get; set; }

		public VentState Vent
		{ get; set; }

		internal bool showMuteButton
		{
			get
			{
				return _showMuteButton;
			}
			set
			{
				_showMuteButton = value;
				if (Level.UI.State == UIState.Office || !value)
				{
					muteIntroInput.IsEnabled = value;
				}
			}
		}
		private bool _showMuteButton;

		internal SpriteSheet officeSprites;
		internal SpriteSheet officeVentDoorSprites;
		internal SpriteSheet recordSprites;

		internal SoundEffect soundVent;
		internal SoundEffect soundLightOn;
		internal SoundEffect soundLightOff;
		internal SoundEffect soundWormhole;
		internal SoundEffect soundRawr;

		internal SoundMusic musicIntro;
		internal SoundMusic musicClassicalish;
		internal SoundMusic musicToreador;
		
		internal SpriteAnimation officeVentAnim;

		internal InputRegion lightswitchInput;
		internal InputRegion buttonLeftInput;
		internal InputRegion buttonRightInput;
		internal InputRegion rawrInput;
		internal InputRegion muteIntroInput;

		internal HelperTimer muteButtonTimer;

		private int _ventAnimProgress;
		private bool _ventMoving;
		private VentState _ventAnimState = VentState.Left;

		private int _recordAnimProgress;

		public Office(Level level)
		{
			Level = level;

			CameraOffset = new Vector2(MAX_CAMERA_OFFSET / 2.0f, 0);

			lightswitchInput = new InputRegion(LIGHTSWITCH_INPUTBOX, false, (i) => {
				IsLightOn = !IsLightOn;

				if (IsLightOn)
				{
					soundLightOn.Play();

					if (!Level.HardBoiled)
					{
						musicClassicalish.Play();
					}
				}
				else
				{
					soundLightOff.Play();
					Level.Monsters.GoldenFlumpty.Shoo();

					if (!Level.HardBoiled) // Don't pause music in hard boiled mode
					{
						musicClassicalish.Pause();
					}
				}
			});

			buttonLeftInput = new InputRegion(BUTTON_LEFT_OFFSET + CameraOffset, BUTTON_SIZE, true, (i) => {
				if (Vent != VentState.Left)
				{
					Vent = VentState.Left;
					_ventMoving = true;
					soundVent.Stop();
					soundVent.Play();
				}
			});

			buttonRightInput = new InputRegion(BUTTON_RIGHT_OFFSET + CameraOffset, BUTTON_SIZE, false, (i) => {
				if (Vent != VentState.Right)
				{
					Vent = VentState.Right;
					_ventMoving = true;
					soundVent.Stop();
					soundVent.Play();
				}
			});

			rawrInput = new InputRegion(RAWR_HITBOX_OFFSET + CameraOffset, RAWR_HITBOX_SIZE, true, (i) => {
				soundRawr.Stop();
				soundRawr.Play(); // spammable
			});

			muteIntroInput = new InputRegion(MUTE_INTRO_INPUTBOX, false, (i) => {
				musicIntro.Stop();
				showMuteButton = false;
				muteButtonTimer.Stop();

				if (Level.HardBoiled)
				{
					musicToreador.Play();
				}
				else
				{
					musicClassicalish.Play();
				}	
			});

			Vent = VentState.Left;
		}

		public List<string> GetDebugLines()
		{
			return new List<string> {
				"Vent: " + Vent + " (#" + _ventAnimProgress + ")",
				"Vent Anim State: " + _ventAnimState,
				"Camera Offset: " + (-CameraOffset.X).ToString("F0"),
				""
			};
		}

		public void OnLaptopDown()
		{
			Level.Monsters.GoldenFlumpty.OnLaptopDown();

			IsPigletCreepier = Level.Rand.Next(CREEPY_PIGLET_RARITY) == 0;
		}

		public void Reset()
		{
			Show();

			IsLightOn = true;

			muteButtonTimer = new HelperTimer(TimeSpan.FromSeconds(1.0), true, "MuteButtonEnabled", () => {
				showMuteButton = true;
				muteButtonTimer = new HelperTimer(TimeSpan.FromSeconds(15.0), true, "MuteSong", () => {
					showMuteButton = false;
					if (Level.HardBoiled)
					{
						musicToreador.PlayIfNotPlaying();
					}
					else
					{
						musicClassicalish.PlayIfNotPlaying();
					}
				});
			});

			showMuteButton = false;
			musicIntro.Play();
		}

		public void Show()
		{
			lightswitchInput.IsEnabled = true;
			buttonLeftInput.IsEnabled = true;
			buttonRightInput.IsEnabled = true;
			rawrInput.IsEnabled = true;
			muteIntroInput.IsEnabled = showMuteButton;
		}

		public void Hide()
		{
			lightswitchInput.IsEnabled = false;
			buttonLeftInput.IsEnabled = false;
			buttonRightInput.IsEnabled = false;
			rawrInput.IsEnabled = false;
			muteIntroInput.IsEnabled = false;
		}

		public void LoadContent(ContentManager content)
		{
			officeSprites = content.Load<SpriteSheet>("ONAF2/Office");
			officeVentDoorSprites = content.Load<SpriteSheet>("ONAF2/OfficeVentDoors");
			recordSprites = content.Load<SpriteSheet>("ONAF2/OfficeRecord");

			soundVent = content.LoadSoundEffect(Level, "ONAF2/Sounds/Swap_Vents");
			soundLightOn = content.LoadSoundEffect(Level, "ONAF2/Sounds/LightOn");
			soundLightOff = content.LoadSoundEffect(Level, "ONAF2/Sounds/LightOff");
			soundWormhole = content.LoadSoundEffect(Level, "ONAF2/Sounds/ClownHoleAppear");
			soundRawr = content.LoadSoundEffect(Level, "ONAF2/Sounds/Rawr", 5.0f);

			musicIntro = content.LoadMusic(Level, "ONAF2/Music/Welcome", 0.8f, false);
			musicClassicalish = content.LoadMusic(Level, "ONAF2/Music/Classical-ish", 0.7f);

			officeVentAnim = new SpriteAnimation(officeVentDoorSprites,
				Vector2.Zero,
				new Vector2(38, 0),
				new Vector2(115, 0),
				new Vector2(155, 0),
				new Vector2(158, 0),
				new Vector2(163, 0));
		}

		public void Update(GameTime gameTime, InputManager input)
		{
			if (!Level.IsJumpscaring)
			{
				lightswitchInput.Update(input, WindowSize);
				buttonLeftInput.Update(input, WindowSize);
				buttonRightInput.Update(input, WindowSize);
				rawrInput.Update(input, WindowSize);
				muteIntroInput.Update(input, WindowSize);

				muteButtonTimer.Update(gameTime);
			}

			Vector2 mousePos = input.GetMousePosPx(WindowSize);
			if (mousePos.X >= 0 && mousePos.X < SIDE_PAN_WIDTH && !Level.IsJumpscaring)
			{
				CameraOffset = new Vector2(Math.Min(CameraOffset.X + SIDE_PAN_SPEED, 0), 0);
			}
			else if (mousePos.X <= WindowSize.X && mousePos.X > WindowSize.X - SIDE_PAN_WIDTH && !Level.IsJumpscaring)
			{
				CameraOffset = new Vector2(Math.Max(CameraOffset.X - SIDE_PAN_SPEED, MAX_CAMERA_OFFSET), 0);
			}

			if (CameraOffset.X < MAX_CAMERA_OFFSET / 2.0f && Level.IsJumpscaring)
			{
				CameraOffset = new Vector2(Math.Min(CameraOffset.X + JUMPSCARE_PAN_SPEED, MAX_CAMERA_OFFSET / 2.0f), 0);
			}
			else if (CameraOffset.X > MAX_CAMERA_OFFSET / 2.0f && Level.IsJumpscaring)
			{
				CameraOffset = new Vector2(Math.Max(CameraOffset.X - JUMPSCARE_PAN_SPEED, MAX_CAMERA_OFFSET / 2.0f), 0);
			}

			if (input.IsKeyPressed(Keys.P))
			{
				IsPigletCreepier = !IsPigletCreepier;
			}

			buttonLeftInput.SetXY(BUTTON_LEFT_OFFSET + CameraOffset);
			buttonRightInput.SetXY(BUTTON_RIGHT_OFFSET + CameraOffset);
			lightswitchInput.SetXY(LIGHTSWITCH_INPUTBOX.TopLeft + CameraOffset);
			rawrInput.SetXY(RAWR_HITBOX_OFFSET + CameraOffset);

			if (_ventMoving)
			{
				if (_ventAnimState == VentState.Left)
				{
					_ventAnimProgress++;
				}
				else
				{
					_ventAnimProgress--;
				}

				if (_ventAnimProgress == 0)
				{
					_ventAnimState = VentState.Left;
					_ventMoving = false;
				}
				else if (_ventAnimProgress == 5)
				{
					_ventAnimState = VentState.Right;
					_ventMoving = false;
				}
			}

			if (gameTime.FrameCount % 1 == 0)
			{
				_recordAnimProgress++;
				if (_recordAnimProgress >= RECORD_FRAMES)
				{
					_recordAnimProgress = 0;
				}
			}
		}

		public void Draw(GameTime gameTime, SpriteBatch sb)
		{
			if (State != UIState.Office)
			{
				return;
			}

			Vector2 shake = Vector2.Zero;
			if (Level.IsJumpscaring && Level.Monsters.currentJumpscarer.ShakesOnJumpscare)
			{
				shake = Level.jumpscareShakeOffset;
			}

			officeSprites["Entry"].Draw(sb, CameraOffset + ENTRY_OFFSET + shake);

			Level.Monsters.BBB.DrawSpook(sb, CameraOffset + shake);
			Level.Monsters.Flumpty.DrawSpook(sb, CameraOffset + shake);
			Level.Monsters.Eyesaur.DrawSpook(sb, CameraOffset + shake);

			if (IsLightOn)
			{
				// Only light on office is drawn from center
				Vector2 mainOffset = new Vector2(-MAX_CAMERA_OFFSET / 2.0f, 0);
				officeSprites["Main"].Draw(sb, Level.Main.WindowCenter + mainOffset + CameraOffset + shake);
				officeVentAnim.Draw(sb, _ventAnimProgress, VENT_OFFSET + CameraOffset + shake);

				if (Vent == VentState.Left)
				{
					officeSprites["VentOnL"].Draw(sb, BUTTON_LEFT_OFFSET + CameraOffset + shake);
				}
				else
				{
					officeSprites["VentOnR"].Draw(sb, BUTTON_RIGHT_OFFSET + CameraOffset + shake);
				}

				recordSprites[_recordAnimProgress.ToString()].Draw(sb, RECORD_OFFSET + CameraOffset + shake);

				if (IsWormholeOpen)
				{
					officeSprites["Wormhole"].Draw(sb, WORMHOLE_OFFSET + CameraOffset + shake);
				}

				if (IsPigletCreepier)
				{
					officeSprites["CreepyPiglet"].Draw(sb, CREEPY_PIGLET_OFFSET + CameraOffset + shake);
				}
			}
			else
			{
				officeSprites["Dark"].Draw(sb, CameraOffset + shake);

				if (Vent == VentState.Left)
				{
					officeSprites["DarkVentOnL"].Draw(sb, BUTTON_DARK_LEFT_OFFSET + CameraOffset + shake);
				}
				else
				{
					officeSprites["DarkVentOnR"].Draw(sb, BUTTON_DARK_RIGHT_OFFSET + CameraOffset + shake);
				}
			}

			Level.Monsters.Clown.DrawSpook(sb, CameraOffset + shake);
			Level.Monsters.GoldenFlumpty.DrawInOffice(sb, CameraOffset + shake);

			if (muteIntroInput.IsEnabled)
			{
				Level.gameUISprites["MuteSongButton"].Draw(sb, MUTE_INTRO_INPUTBOX.TopLeft);
			}

			if (Level.CHEAT_MapDebug && !Level.IsJumpscaring)
			{
				Level.gameUISprites["Map"].Draw(sb, Laptop.MAP_OFFSET, Util.MakeTransparency(120), Vector2.One);
				Level.Monsters.DrawOnLaptop(sb);
			}
		}
	}
}

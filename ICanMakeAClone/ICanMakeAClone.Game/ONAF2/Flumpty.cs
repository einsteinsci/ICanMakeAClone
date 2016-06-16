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
	public class Flumpty : MonsterBase
	{
		public enum Position
		{
			Cam1 = 0,
			Cam2,
			Cam3_Start,
			Cam3_UpCloseAndPersonal,
			Cam6,
			Cam7,
			OfficeEntry
		}

		public const int SPOOK_LINGER_FRAME = 22;
		public const int SPOOK_FRAME_COUNT = 26;
		public const int SPOOK_EXPOSURE_ACTIVE = 20;
		public const int SPOOK_EXPOSURE_RETREAT = 23;
		public const int SPOOK_ANIM_SPEED = 3;
		public const int MOVEMENT_RARITY = 2;
		public const int JUMPSCARE_FRAME_COUNT = 13;

		public const float SPOOK_LINGER_TIME = 2.0f;
		public const float EXPOSURE_RATE = 0.15f; // about 7 secs
		public const float TIME_BEFORE_SPOOK = 3.0f;
		public const float SCREAM_DELAY = 0.3f;

		public static readonly Vector2 SPOOK_MAIN_OFFSET = new Vector2(608, 463);

		public static readonly TimeSpan ACTIVATE_TIME = TimeSpan.FromMinutes(30);
		public static readonly TimeSpan ACTIVATE_TIME_HARDBOILED = TimeSpan.FromMinutes(10);

		#region SPOOK_OFFSETS
		public static readonly Vector2[] SPOOK_OFFSETS = new Vector2[] {
			new Vector2(0, -273),
			new Vector2(0, -311),
			new Vector2(0, -313),
			new Vector2(0, -313),
			new Vector2(0, -311),
			new Vector2(0, -311),
			new Vector2(0, -313),
			new Vector2(0, -313),
			new Vector2(0, -313),
			new Vector2(0, -313),
			new Vector2(0, -313),
			new Vector2(0, -313),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -311),
			new Vector2(0, -311),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -312),
			new Vector2(0, -307),
		};
		#endregion SPOOK_OFFSETS

		public static readonly Vector2[] JUMPSCARE_OFFSETS = new Vector2[] {
			new Vector2(0, 252),
			Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero,
			Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero,
			Vector2.Zero, Vector2.Zero
		};

		public static readonly Dictionary<Position, Vector2> ROOM_OFFSETS = new Dictionary<Position, Vector2>() {
			{ Position.Cam1, new Vector2(900, 0) },
			{ Position.Cam2, new Vector2(485, 205) },
			{ Position.Cam3_Start, new Vector2(384, 210) },
			{ Position.Cam3_UpCloseAndPersonal, new Vector2(269, 82) },
			{ Position.Cam6, new Vector2(977, 297) },
			{ Position.Cam7, new Vector2(430, 140) }
		};

		public static readonly Dictionary<Position, Vector2> MAP_ICON_OFFSETS = new Dictionary<Position, Vector2>() {
			{ Position.Cam1, new Vector2(85, 50) },
			{ Position.Cam2, new Vector2(310, 60) },
			{ Position.Cam3_Start, new Vector2(190, 100) },
			{ Position.Cam3_UpCloseAndPersonal, new Vector2(190, 125) },
			{ Position.Cam6, new Vector2(70, 285) },
			{ Position.Cam7, new Vector2(313, 245) },
			{ Position.OfficeEntry, new Vector2(150, 270) }
		};

		public bool IsExposing
		{ get; private set; }

		public Position Pos
		{ get; private set; }

		public override string Name => "Flumpty";

		public bool AboutToSpook => _showSpook && spookFrame <= SPOOK_EXPOSURE_ACTIVE;

		internal SpriteSheet spookSprites;
		internal SpriteSheet roomSprites;

		internal SpriteAnimation spookAnim;

		internal int spookFrame
		{ get; private set; }

		internal float timeUntilSpook
		{ get; private set; }

		private TimeSpan _activateTime => Level.IsHardBoiled ? ACTIVATE_TIME_HARDBOILED : ACTIVATE_TIME;

		private bool _showSpook;

		private float _lingerTime = SPOOK_LINGER_TIME;

		public Flumpty(Level level) : base(level)
		{ }

		public void StartSpook()
		{
			_showSpook = true;
			spookFrame = 0;
			_lingerTime = SPOOK_LINGER_TIME;
		}

		public override void BeginJumpscare()
		{
			base.BeginJumpscare();

			_showSpook = false;
			spookFrame = 0;
		}

		public override void Reset()
		{
			base.Reset();

			IsExposing = false;
			Pos = Position.Cam3_Start;
			IsActive = false;

			spookFrame = 0;
			timeUntilSpook = TIME_BEFORE_SPOOK;
			_showSpook = false;
			_lingerTime = SPOOK_LINGER_TIME;

			screamDelay = new HelperTimer(TimeSpan.FromSeconds(SCREAM_DELAY), false, "ScreamDelay", () => {
				Manager.soundScreamGeneric.Play();
			});
		}

		public void Advance()
		{
			Position res = Pos;

			// Keep generating until he would actually be moving.
			while (res == Pos || res > Position.OfficeEntry || !Manager.IsPositionLegal(res))
			{
				res = (Position)Rand.Next((int)Position.OfficeEntry + 1);
			}

			Pos = res;
			timeUntilSpook = TIME_BEFORE_SPOOK;
		}

		public CameraIndex? GetCameraVisible()
		{
			switch (Pos)
			{
			case Position.Cam1:
				return CameraIndex.Cam1;
			case Position.Cam2:
				return CameraIndex.Cam2;
			case Position.Cam3_Start:
			case Position.Cam3_UpCloseAndPersonal:
				return CameraIndex.Cam3;
			case Position.Cam6:
				return CameraIndex.Cam6;
			case Position.Cam7:
				return CameraIndex.Cam7;
			default:
				return null;
			}
		}

		public override void OnStatic()
		{
			if (Rand.Next(MOVEMENT_RARITY) == 0 && IsActive)
			{
				if (!Level.CHEAT_MonstersStayPut && Pos != Position.OfficeEntry)
				{
					Advance();
				}
			}
		}

		public override void LoadContent(ContentManager content)
		{
			spookSprites = content.Load<SpriteSheet>("ONAF2/SpookFlumpty");
			roomSprites = content.Load<SpriteSheet>("ONAF2/RoomFlumpty");
			jumpscareSprites = content.Load<SpriteSheet>("ONAF2/JumpscareFlumpty");

			spookAnim = new SpriteAnimation(spookSprites, SPOOK_OFFSETS);
		}

		public override void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.F) && Pos != Position.OfficeEntry)
			{
				Advance();
				IsActive = true;
			}

			if (Pos == Position.OfficeEntry && !_showSpook)
			{
				timeUntilSpook -= (float)gt.Elapsed.TotalSeconds;

				if (timeUntilSpook <= 0)
				{
					StartSpook();
				}
			}

			if (IsExposing && Level.Office.IsLightOn && !Manager.Eyesaur.IsExposing && 
				!Manager.Clown.IsExposing && !Manager.BBB.IsExposing)
			{
				Level.Exposure += EXPOSURE_RATE * (float)gt.Elapsed.TotalSeconds * ExposureMultiplier;
			}

			if (Level.IsJumpscaring && Manager.currentJumpscarer == this && gt.FrameCount % 5 == 0)
			{
				if (jumpscareFrame < JUMPSCARE_FRAME_COUNT - 1)
				{
					jumpscareFrame++;
				}
				else
				{
					Level.UI.SetStateNextFrame(UIState.Static);
					Manager.soundAttackMusic.Stop();
					Manager.soundScreamGeneric.Stop();
				}
			}

			screamDelay.Update(gt);

			if (Level.TimeSinceMidnight > _activateTime && !IsActive)
			{
				IsActive = true;
			}

			if (_showSpook && !Level.IsJumpscaring)
			{
				if (gt.FrameCount % SPOOK_ANIM_SPEED == 0)
				{
					if (spookFrame == SPOOK_EXPOSURE_ACTIVE)
					{
						IsExposing = true;
						PlaySpookSound();
					}
					else if (spookFrame == SPOOK_EXPOSURE_RETREAT)
					{
						IsExposing = false;
					}
				}

				if (spookFrame == SPOOK_LINGER_FRAME)
				{
					if (!Level.Office.IsLightOn)
					{
						_lingerTime -= (float)gt.Elapsed.TotalSeconds;

						if (_lingerTime <= 0)
						{
							spookFrame++;
						}
					}
				}
				else if (spookFrame < SPOOK_FRAME_COUNT - 1)
				{
					if (gt.FrameCount % 3 == 0)
					{
						spookFrame++;
					}
				}
				else
				{
					spookFrame = 0;
					_lingerTime = SPOOK_LINGER_TIME;
					_showSpook = false;
					Advance();
				}
			}
		}

		public void DrawSpook(SpriteBatch sb, Vector2 cameraOffset)
		{
			if (_showSpook)
			{
				spookAnim.Draw(sb, spookFrame, SPOOK_MAIN_OFFSET + cameraOffset);
			}
		}

		public override void DrawOnCamera(SpriteBatch sb, Vector2 offset, CameraIndex cam)
		{
			CameraIndex? visible = GetCameraVisible();
			if (visible == null)
			{
				return; // in case of office entry
			}

			if (visible.Value == cam)
			{
				roomSprites[Pos.ToString()].Draw(sb, offset + ROOM_OFFSETS[Pos]);
			}
		}

		public override void DrawOnLaptop(SpriteBatch sb)
		{
			if (Level.CHEAT_MapDebug)
			{
				mapIcons["Flumpty"].Draw(sb, Laptop.MAP_OFFSET + MAP_ICON_OFFSETS[Pos]);
			}
		}

		public override void DrawJumpscare(SpriteBatch sb)
		{
			if (IsJumpscaring)
			{
				jumpscareSprites[jumpscareFrame].Draw(sb, Level.Main.WindowCenter + 
					JUMPSCARE_OFFSETS[jumpscareFrame] + Level.jumpscareShakeOffset);
			}
		}
	}
}

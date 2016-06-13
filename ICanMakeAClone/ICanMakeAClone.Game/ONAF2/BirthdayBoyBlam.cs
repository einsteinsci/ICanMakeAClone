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
	public class BirthdayBoyBlam : MonsterBase
	{
		public enum Position
		{
			Cam6_Thinking = 0,
			Cam6_WavyArms,
			Cam3,
			Cam7_Chat,
			Cam7_KevinJr,
			OfficeEntry,
			Cam6_Kazotsky, // https://wiki.teamfortress.com/wiki/Kazotsky_Kick
		}

	
		public const int SPOOK_LINGER_FRAME = 3;
		public const int SPOOK_FRAME_COUNT = 28;
		public const int SPOOK_EXPOSURE_ACTIVE = 0;
		public const int SPOOK_EXPOSURE_RETREAT = 7;
		public const int SPOOK_ANIM_SPEED = 3;
		public const int MOVEMENT_RARITY = 4;
		public const int MOVEMENT_RARITY_LATE = 2;
		public const int JUMPSCARE_FRAME_COUNT = 11;

		public const float SPOOK_LINGER_TIME = 3.0f;
		public const float EXPOSURE_RATE = 0.17f; // about 6 secs
		public const float TIME_BEFORE_SPOOK = 5.0f;
		public const float SCREAM_DELAY = 0.3f;

		public static readonly Vector2 SPOOK_MAIN_OFFSET = new Vector2(608, 440);

		public static readonly TimeSpan ACTIVATE_TIME = TimeSpan.FromHours(1.33);
		public static readonly TimeSpan LATE_NIGHT = TimeSpan.FromHours(3.75);

		#region SPOOK_OFFSETS
		public static readonly Vector2[] SPOOK_OFFSETS = new Vector2[] {
			new Vector2(229, -245),
			new Vector2(185, -235),
			new Vector2(166, -228),
			new Vector2(160, -224),
			new Vector2(158, -223),
			new Vector2(165, -225),
			new Vector2(160, -234),
			new Vector2(168, -240),
			new Vector2(168, -242),
			new Vector2(165, -243),
			new Vector2(153, -242),
			new Vector2(141, -245),
			new Vector2(135, -246),
			new Vector2(126, -247),
			new Vector2(120, -248),
			new Vector2(105, -247),
			new Vector2(93, -246),
			new Vector2(80, -244),
			new Vector2(70, -244),
			new Vector2(60, -243),
			new Vector2(45, -243),
			new Vector2(32, -243),
			new Vector2(15, -243),
			new Vector2(0, -244),
			new Vector2(0, -242),
			new Vector2(0, -243),
			new Vector2(0, -218),
			new Vector2(-10, -218),
		};
		#endregion SPOOK_OFFSETS

		public static readonly Dictionary<Position, Vector2> ROOM_OFFSETS = new Dictionary<Position, Vector2>() {
			{ Position.Cam6_Thinking, new Vector2(565, 305) },
			{ Position.Cam6_WavyArms, new Vector2(270, 215) },
			{ Position.Cam3, new Vector2(810, 310) },
			{ Position.Cam7_Chat, new Vector2(1000, 225) },
			{ Position.Cam7_KevinJr, new Vector2(405, 265) },
			{ Position.Cam6_Kazotsky, new Vector2(850, 295) }
		};

		public static readonly Dictionary<Position, Vector2> MAP_ICON_OFFSETS = new Dictionary<Position, Vector2>() {
			{ Position.Cam6_Thinking, new Vector2(60, 270) },
			{ Position.Cam6_WavyArms, new Vector2(62, 230) },
			{ Position.Cam3, new Vector2(212, 125) },
			{ Position.Cam7_Chat, new Vector2(340, 255) },
			{ Position.Cam7_KevinJr, new Vector2(320, 275) },
			{ Position.OfficeEntry, new Vector2(240, 275) },
			{ Position.Cam6_Kazotsky, new Vector2(77, 270) }
		};

		public bool IsExposing
		{ get; private set; }

		public bool IsActive
		{ get; private set; }

		public Position Pos
		{ get; private set; }

		internal SpriteSheet spookSprites;
		internal SpriteSheet roomSprites;

		internal SpriteAnimation spookAnim;

		internal int spookFrame
		{ get; private set; }

		internal float timeUntilSpook
		{ get; private set; }

		// Is twice as active after 3:45 AM
		internal int movementRarity => Level.TimeSinceMidnight > LATE_NIGHT ? MOVEMENT_RARITY_LATE : MOVEMENT_RARITY;

		private bool _showSpook;

		private float _lingerTime = SPOOK_LINGER_FRAME;

		public BirthdayBoyBlam(Level level) : base(level)
		{ }

		public void Advance()
		{
			Position next = GetNextPos(Pos);

			if (Manager.IsPositionLegal(next))
			{
				Pos = next;

				if (Pos == Position.OfficeEntry)
				{
					timeUntilSpook = TIME_BEFORE_SPOOK;
				}
			}
		}

		public static Position GetNextPos(Position pos)
		{
			switch (pos)
			{
			case Position.Cam6_Thinking:
				return Position.Cam6_WavyArms;
			case Position.Cam6_WavyArms:
				return Position.Cam3;
			case Position.Cam3:
				return Position.Cam7_Chat;
			case Position.Cam7_Chat:
				return Position.Cam7_KevinJr;
			case Position.Cam7_KevinJr:
				return Position.OfficeEntry;
			case Position.OfficeEntry:
				return Position.Cam6_Kazotsky;
			case Position.Cam6_Kazotsky:
				return Position.Cam6_Thinking;
			default:
				return Position.Cam6_Thinking;
			}
		}

		public CameraIndex? GetCameraVisible()
		{
			switch (Pos)
			{
			case Position.Cam6_Kazotsky:
			case Position.Cam6_Thinking:
			case Position.Cam6_WavyArms:
				return CameraIndex.Cam6;
			case Position.Cam3:
				return CameraIndex.Cam3;
			case Position.Cam7_Chat:
			case Position.Cam7_KevinJr:
				return CameraIndex.Cam7;
			default:
				return null;
			}
		}

		public void StartSpook()
		{
			_showSpook = true;
			spookFrame = 0;
			_lingerTime = SPOOK_LINGER_TIME;
			PlaySpookSound();
		}

		public override void BeginJumpscare()
		{
			base.BeginJumpscare();

			_showSpook = false;
			spookFrame = 0;
		}

		public override void Reset()
		{
			IsExposing = false;
			Pos = Position.Cam6_Thinking;
			IsActive = false;

			timeUntilSpook = TIME_BEFORE_SPOOK;
			spookFrame = 0;
			_showSpook = false;
			_lingerTime = SPOOK_LINGER_TIME;

			screamDelay = new HelperTimer(TimeSpan.FromSeconds(SCREAM_DELAY), false, "ScreamDelay", () => {
				Manager.soundScreamGeneric.Play();
			});
		}

		public override void LoadContent(ContentManager content)
		{
			spookSprites = content.Load<SpriteSheet>("ONAF2/SpookBBB");
			roomSprites = content.Load<SpriteSheet>("ONAF2/RoomBBB");
			jumpscareSprites = content.Load<SpriteSheet>("ONAF2/JumpscareBBB");

			spookAnim = new SpriteAnimation(spookSprites, SPOOK_OFFSETS);
		}

		public override void OnStatic()
		{
			if (Pos == Position.Cam6_Kazotsky || Rand.Next(movementRarity) == 0)
			{
				if (!Level.CHEAT_MonstersStayPut && IsActive && Pos != Position.OfficeEntry)
				{
					Advance();
				}
			}
		}

		public override void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.B) && Pos != Position.OfficeEntry)
			{
				IsActive = true;
				Advance();
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
				!Manager.Clown.IsExposing)
			{
				Level.Exposure += EXPOSURE_RATE * (float)gt.Elapsed.TotalSeconds;
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

			if (Level.TimeSinceMidnight >= ACTIVATE_TIME && !IsActive)
			{
				IsActive = true;
			}

			if (_showSpook)
			{
				if (gt.FrameCount % SPOOK_ANIM_SPEED == 0)
				{
					if (spookFrame == SPOOK_EXPOSURE_ACTIVE)
					{
						IsExposing = true;
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
					if (gt.FrameCount % SPOOK_ANIM_SPEED == 0)
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
				mapIcons["BBB"].Draw(sb, Laptop.MAP_OFFSET + MAP_ICON_OFFSETS[Pos]);
			}
		}

		public override void DrawJumpscare(SpriteBatch sb)
		{
			if (IsJumpscaring)
			{
				jumpscareSprites[jumpscareFrame].Draw(sb, Level.Main.WindowCenter + Level.jumpscareShakeOffset);
			}
		}
	}
}

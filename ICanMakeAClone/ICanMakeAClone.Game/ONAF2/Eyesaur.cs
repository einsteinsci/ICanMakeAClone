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
	public class Eyesaur : MonsterBase
	{
		public enum Position
		{
			Cam1_Waiting = 0,
			Cam1_Emerging,
			Cam3,
			Cam6,
			OfficeEntry
		}

		public const int SPOOK_LINGER_FRAME = 8;
		public const int SPOOK_FRAME_COUNT = 12;
		public const int SPOOK_EXPOSURE_ACTIVE = 3;
		public const int SPOOK_EXPOSURE_RETREAT = 9;
		public const int SPOOK_ANIM_SPEED = 3;
		public const int MOVEMENT_RARITY = 2;
		public const int EMERGE_RARITY = 12;
		public const int EMERGE_RARITY_HARDBOILED = 5;
		public const int JUMPSCARE_FRAME_COUNT = 14;

		public const float SPOOK_LINGER_TIME = 3.0f;
		public const float EXPOSURE_RATE = 0.33f; // about 3 secs
		public const float TIME_BEFORE_SPOOK = 5.0f;
		public const float RETIREMENT_TIME = 30.0f;
		public const float SCREAM_DELAY = 0.1f;

		public static readonly Vector2 SPOOK_MAIN_OFFSET = new Vector2(608, 92);

		public static readonly TimeSpan ACTIVATE_TIME = TimeSpan.FromHours(4.0);
		public static readonly TimeSpan ACTIVATE_TIME_HARDBOILED = TimeSpan.FromHours(1.0);

		public static readonly Vector2[] SPOOK_OFFSETS = new Vector2[] {
			new Vector2(0, 75),
			new Vector2(0, 60),
			new Vector2(0, 45),
			new Vector2(0, 50),
			new Vector2(0, 50),
			new Vector2(0, 50),
			new Vector2(0, 50),
			new Vector2(0, 50),
			new Vector2(0, 50),
			new Vector2(0, 55),
			new Vector2(0, 70),
			new Vector2(0, 85)
		};

		public static readonly Dictionary<Position, Vector2> ROOM_OFFSETS = new Dictionary<Position, Vector2>() {
			{ Position.Cam1_Waiting, new Vector2(460, 180) },
			{ Position.Cam1_Emerging, new Vector2(460, 71) },
			{ Position.Cam3, new Vector2(402, 164) },
			{ Position.Cam6, new Vector2(59, 227) },
		};

		public static readonly Dictionary<Position, Vector2> MAP_ICON_OFFSETS = new Dictionary<Position, Vector2>() {
			{ Position.Cam1_Emerging, new Vector2(60, 35) },
			{ Position.Cam3, new Vector2(175, 75) },
			{ Position.Cam6, new Vector2(70, 245) },
			{ Position.OfficeEntry, new Vector2(135, 280) }
		};

		public bool IsExposing
		{ get; private set; }

		public Position Pos
		{ get; private set; }

		public bool IsActive
		{ get; private set; }

		public bool IsEmerged => Pos != Position.Cam1_Waiting;

		public override string Name => "Eyesaur";

		internal SpriteSheet spookSprites;
		internal SpriteSheet roomSprites;

		internal SpriteAnimation spookAnim;

		internal int spookFrame
		{ get; private set; }

		internal float timeUntilSpook
		{ get; private set; }

		internal float retirementTimeLeft
		{ get; private set; }

		private TimeSpan _activateTime => Level.IsHardBoiled ? ACTIVATE_TIME_HARDBOILED : ACTIVATE_TIME;

		private int _emergeRarity => Level.IsHardBoiled ? EMERGE_RARITY_HARDBOILED : EMERGE_RARITY;

		private bool _showSpook;

		private float _lingerTime = SPOOK_LINGER_TIME;

		public Eyesaur(Level level) : base(level)
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
				else if (Pos == Position.Cam1_Waiting)
				{
					retirementTimeLeft = RETIREMENT_TIME;
				}
			}
		}

		public static Position GetNextPos(Position pos)
		{
			switch (pos)
			{
			case Position.Cam1_Waiting:
			case Position.Cam1_Emerging:
			case Position.Cam3:
			case Position.Cam6:
				return pos + 1;
			default:
				return Position.Cam1_Waiting;
			}
		}

		public CameraIndex? GetCameraVisible()
		{
			switch (Pos)
			{
			case Position.Cam1_Waiting:
			case Position.Cam1_Emerging:
				return CameraIndex.Cam1;
			case Position.Cam3:
				return CameraIndex.Cam3;
			case Position.Cam6:
				return CameraIndex.Cam6;
			default:
				return null;
			}
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
			spookFrame = 0;
			timeUntilSpook = TIME_BEFORE_SPOOK;
			retirementTimeLeft = 0; // Retirement time only occurs after spooking the player

			_showSpook = false;
			_lingerTime = SPOOK_LINGER_TIME;

			screamDelay = new HelperTimer(TimeSpan.FromSeconds(SCREAM_DELAY), false, "ScreamDelay", () => {
				Manager.soundScreamEyesaur.Play();
			});
		}

		public void StartSpook()
		{
			spookFrame = 0;
			_showSpook = true;
			_lingerTime = SPOOK_LINGER_TIME;
			PlaySpookSound();
		}

		public override void LoadContent(ContentManager content)
		{
			spookSprites = content.Load<SpriteSheet>("ONAF2/SpookEyesaur");
			roomSprites = content.Load<SpriteSheet>("ONAF2/RoomEyesaur");
			jumpscareSprites = content.Load<SpriteSheet>("ONAF2/JumpscareEyesaur");

			spookAnim = new SpriteAnimation(spookSprites, SPOOK_OFFSETS);
		}

		public override void OnStatic()
		{
			if (Pos == Position.OfficeEntry)
			{
				return;
			}

			if (IsActive)
			{
				int rarity = IsEmerged ? MOVEMENT_RARITY : _emergeRarity;

				if (Rand.Next(rarity) == 0 && retirementTimeLeft <= 0)
				{
					Advance();
				}
			}
		}

		public override void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.E) && Pos != Position.OfficeEntry)
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

			if (Level.TimeSinceMidnight >= _activateTime && !IsActive)
			{
				IsActive = true;
			}

			if (IsExposing && Level.Office.IsLightOn)
			{
				if (Level.IsHardBoiled && Level.Exposure > 0.25f)
				{
					Level.Exposure = 1.0f; // DIE
				}
				else
				{
					Level.Exposure += EXPOSURE_RATE * (float)gt.Elapsed.TotalSeconds * ExposureMultiplier;
				}
			}

			if (Level.IsJumpscaring && Manager.currentJumpscarer == this && gt.FrameCount % 4 == 0)
			{
				if (jumpscareFrame < JUMPSCARE_FRAME_COUNT - 1)
				{
					jumpscareFrame++;
				}
				else
				{
					Level.UI.SetStateNextFrame(UIState.Static);
					Manager.soundAttackMusic.Stop();
					Manager.soundScreamEyesaur.Stop();
				}
			}

			screamDelay.Update(gt);

			if (retirementTimeLeft > 0 && IsActive)
			{
				retirementTimeLeft = Math.Max(retirementTimeLeft - (float)gt.Elapsed.TotalSeconds, 0);
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

		public override void DrawJumpscare(SpriteBatch sb)
		{
			if (IsJumpscaring)
			{
				jumpscareSprites[jumpscareFrame].Draw(sb, Level.Main.WindowCenter + Level.jumpscareShakeOffset);
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

			if (cam == CameraIndex.Cam1 && Pos != Position.Cam1_Waiting && Pos != Position.Cam1_Emerging)
			{
				roomSprites["Cam1_Missing"].Draw(sb, offset + ROOM_OFFSETS[Position.Cam1_Waiting]);
			}
		}

		public override void DrawOnLaptop(SpriteBatch sb)
		{
			if (Level.CHEAT_MapDebug && Pos != Position.Cam1_Waiting)
			{
				mapIcons["Eyesaur"].Draw(sb, Laptop.MAP_OFFSET + MAP_ICON_OFFSETS[Pos]);
			}
		}
	}
}

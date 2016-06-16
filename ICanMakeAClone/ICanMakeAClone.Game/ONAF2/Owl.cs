using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICanMakeAClone.AI;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Xenko.Audio;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Input;
using UltimateUtil;

namespace ICanMakeAClone.ONAF2
{
	public class Owl : MonsterBase
	{
		public enum Position
		{
			Perch,
			VentEntryW,
			VentEntryE,
			VentMidW,
			VentMidE,
			VentBendW,
			VentBendE,
			VentExitW,
			VentExitE
		}

		public const int PERCH_LEAVE_RARITY = 8;
		public const int PERCH_LEAVE_RARITY_HARDBOILED = 4;
		public const int VENT_FRAME_COUNT = 20;
		public const int JUMPSCARE_FRAME_COUNT = 20;
		public const int JUMPSCARE_SPIN_END_FRAME = 12;

		public const float SCREAM_DELAY = 0.6f;
		public const float START_RETIREMENT_TIME = 10.0f;

		public const double VENT_ENTRY_TIME = 5.0;
		public const double VENT_MID_TIME = 5.0;
		public const double VENT_BEND_TIME = 2.5;

		public static readonly Vector2 PERCH_OFFSET = new Vector2(563, 150);
		public static readonly Vector2 VENT_EARLY_EXTRA_OFFSET = new Vector2(5, 0);

		public static readonly TimeSpan ACTIVATE_TIME = TimeSpan.FromMinutes(30);

		public static readonly Vector2[] VENT_OFFSETS = new Vector2[] {
			new Vector2(628, 315),
			new Vector2(628, 315),
			new Vector2(628, 320),
			new Vector2(628, 325),
			new Vector2(628, 330),
			new Vector2(628, 335),
			new Vector2(628, 350),
			new Vector2(628, 355),
			new Vector2(628, 355),
			new Vector2(625, 365),
			new Vector2(625, 380),
			new Vector2(625, 385),
			new Vector2(615, 390),
			new Vector2(600, 410),
			new Vector2(590, 410),
			Laptop.WINDOW_OFFSET,
			Laptop.WINDOW_OFFSET,
			Laptop.WINDOW_OFFSET,
			Laptop.WINDOW_OFFSET,
			Laptop.WINDOW_OFFSET
		};

		public static readonly Vector2[] JUMPSCARE_OFFSETS = new Vector2[] {
			new Vector2(-315, -225),
			new Vector2(-375, -240),
			new Vector2(-315, -215),
			new Vector2(-250, -210),
			new Vector2(-205, -190),
			new Vector2(-165, -165),
			new Vector2(-135, -175),
			new Vector2(-110, -165),
			new Vector2(-90, -150),
			new Vector2(-80, -135),
			new Vector2(-60, -145),
			new Vector2(-50, -110),
			new Vector2(0, -60),
			new Vector2(0, -10),
			Vector2.Zero,
			Vector2.Zero,
			Vector2.Zero,
			Vector2.Zero,
			Vector2.Zero,
			Vector2.Zero,
		};

		public static readonly Dictionary<Position, Vector2> MAP_ICON_OFFSETS = new Dictionary<Position, Vector2> {
			{ Position.Perch, new Vector2(190, 30) },
			{ Position.VentEntryW, new Vector2(90, 115) },
			{ Position.VentEntryE, new Vector2(290, 115) },
			{ Position.VentMidW, new Vector2(100, 190) },
			{ Position.VentMidE, new Vector2(280, 190) },
			{ Position.VentBendW, new Vector2(105, 243) },
			{ Position.VentBendE, new Vector2(275, 243) },
			{ Position.VentExitW, new Vector2(138, 300) },
			{ Position.VentExitE, new Vector2(245, 300) }
		};

		public override bool ShakesOnJumpscare => false;

		public Position Pos
		{ get; private set; }

		public override string Name => "Owl";

		internal SpriteSheet perchSprites;
		internal SpriteSheet ventSprites;

		internal HelperTimer advanceTimer;

		private int _ventCurrentFrame;
		private bool _ventAnimPlaying;

		private float _retirementTimeLeft;

		public Owl(Level level) : base(level)
		{ }

		public void Advance()
		{
			Position next = GetNextPos();

			if (Manager.IsPositionLegal(next))
			{
				Pos = next;
			}

			#region advanceTimer
			switch (Pos)
			{
			case Position.VentEntryW:
			case Position.VentEntryE:
				advanceTimer = new HelperTimer(TimeSpan.FromSeconds(VENT_ENTRY_TIME), true, "VentEntry", () => {
					if (Pos == Position.VentEntryW || Pos == Position.VentEntryE)
					{
						Advance();
					}
				});
				break;
			case Position.VentMidW:
			case Position.VentMidE:
				advanceTimer = new HelperTimer(TimeSpan.FromSeconds(VENT_MID_TIME), true, "VentMid", () => {
					if (Pos == Position.VentMidW || Pos == Position.VentMidE)
					{
						Advance();
					}
				});
				break;
			case Position.VentBendW:
			case Position.VentBendE:
				advanceTimer = new HelperTimer(TimeSpan.FromSeconds(VENT_BEND_TIME), true, "VentBend", () => {
					if (Pos == Position.VentBendW || Pos == Position.VentBendE)
					{
						Advance();
					}
				});
				break;
			case Position.VentExitW:
				if (Level.Office.Vent == VentState.Right && !Level.CHEAT_OwlInvincibility)
				{
					BeginJumpscare();
				}
				else
				{
					DoThunk(Manager.soundThunk1);
					(Level.Bot as SmartAI)?.OnOwlThunk();
				}
				break;
			case Position.VentExitE:
				if (Level.Office.Vent == VentState.Left && !Level.CHEAT_OwlInvincibility)
				{
					BeginJumpscare();
				}
				else
				{
					DoThunk(Manager.soundThunk2);
					(Level.Bot as SmartAI)?.OnOwlThunk();
				}
				break;
			}
			#endregion advanceTimer
		}

		public void DoThunk(SoundEffect thunk)
		{
			thunk?.Play();
			Pos = Position.Perch;
			_retirementTimeLeft = START_RETIREMENT_TIME;
			Level.Laptop.StartStatic();
		}

		public override void Reset()
		{
			Pos = Position.Perch;
			IsActive = false;

			_ventCurrentFrame = 0;
			_retirementTimeLeft = 0;

			screamDelay = new HelperTimer(TimeSpan.FromSeconds(SCREAM_DELAY), false, "ScreamDelay", Manager.soundScreamOwl.Play);
		}

		public Position GetNextPos()
		{
			if (Pos == Position.Perch)
			{
				return Rand.NextBool() ? Position.VentEntryW : Position.VentEntryE;
			}

			switch (Pos)
			{
			case Position.VentEntryW:
				return Position.VentMidW;
			case Position.VentEntryE:
				return Position.VentMidE;
			case Position.VentMidW:
				return Position.VentBendW;
			case Position.VentMidE:
				return Position.VentBendE;
			case Position.VentBendW:
				return Position.VentExitW;
			case Position.VentBendE:
				return Position.VentExitE;
			default:
				return Position.Perch;
			}
		}

		public override void LoadContent(ContentManager content)
		{
			perchSprites = content.Load<SpriteSheet>("ONAF2/OwlPerch");
			ventSprites = content.Load<SpriteSheet>("ONAF2/OwlVent");
			jumpscareSprites = content.Load<SpriteSheet>("ONAF2/JumpscareOwl");
		}

		public override void OnStatic()
		{
			if (Pos == Position.Perch && _retirementTimeLeft <= 0 && !Level.CHEAT_MonstersStayPut)
			{
				int rarity = Level.IsHardBoiled ? PERCH_LEAVE_RARITY_HARDBOILED : PERCH_LEAVE_RARITY;
				if (Rand.Next(rarity) == 0)
				{
					Advance();
				}
			}
		}

		public override void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.W) && !Level.IsJumpscaring)
			{
				if (!IsActive)
				{
					IsActive = true;
				}
				else
				{
					Advance();
				}
			}

			if (Level.IsJumpscaring && Manager.currentJumpscarer == this && gt.FrameCount % 3 == 0)
			{
				if (jumpscareFrame < JUMPSCARE_FRAME_COUNT - 1)
				{
					jumpscareFrame++;
				}
				else
				{
					Level.UI.SetStateNextFrame(UIState.Static);
					Manager.soundAttackMusic.Stop();
					Manager.soundScreamOwl.Stop();
				}
			}

			screamDelay.Update(gt);

			if (Level.TimeSinceMidnight >= ACTIVATE_TIME && !IsActive)
			{
				IsActive = true;
				Level.Laptop.StartStatic();
			}

			if (_retirementTimeLeft > 0 && IsActive)
			{
				_retirementTimeLeft = Math.Max(_retirementTimeLeft - (float)gt.Elapsed.TotalSeconds, 0);
			}

			if (Level.UI.State == UIState.Laptop)
			{
				if ((Pos == Position.VentEntryW && Level.Laptop.ActiveCamera == CameraIndex.Cam4) ||
					(Pos == Position.VentEntryE && Level.Laptop.ActiveCamera == CameraIndex.Cam5))
				{
					Advance();
					_ventAnimPlaying = true;
				}

				if ((Pos == Position.VentMidW && Level.Laptop.ActiveCamera == CameraIndex.Cam4) ||
					(Pos == Position.VentMidE && Level.Laptop.ActiveCamera == CameraIndex.Cam5))
				{
					_ventAnimPlaying = true;
				}
			}

			if ((Pos == Position.VentMidE || Pos == Position.VentMidW) && _ventAnimPlaying)
			{
				if (_ventCurrentFrame < VENT_FRAME_COUNT - 1)
				{
					if (gt.FrameCount % 3 == 0)
					{
						_ventCurrentFrame++;
					}
				}
				else
				{
					_ventCurrentFrame = 0;
					Advance();
					_ventAnimPlaying = false;
				}
			}

			advanceTimer?.Update(gt);
		}

		public override void DrawOnCamera(SpriteBatch sb, Vector2 offset, CameraIndex cam)
		{
			if (cam == CameraIndex.Cam3 && Pos == Position.Perch)
			{
				string sprite = IsActive ? "Glaring" : "Sleeping";
				perchSprites[sprite].Draw(sb, PERCH_OFFSET + offset);
			}

			if (_ventAnimPlaying)
			{
				if ((Pos == Position.VentMidW && cam == CameraIndex.Cam4) ||
				(Pos == Position.VentMidE && cam == CameraIndex.Cam5))
				{
					Vector2 v = VENT_OFFSETS[_ventCurrentFrame];
					if (_ventCurrentFrame < 4)
					{
						if (Pos == Position.VentMidW)
						{
							v -= VENT_EARLY_EXTRA_OFFSET * (4 - _ventCurrentFrame);
						}
						else if (Pos == Position.VentMidE)
						{
							v += VENT_EARLY_EXTRA_OFFSET * (4 - _ventCurrentFrame);
						}
					}

					ventSprites[_ventCurrentFrame].Draw(sb, v);
				}
			}
		}

		public override void DrawOnLaptop(SpriteBatch sb)
		{
			if (Level.CHEAT_MapDebug)
			{
				mapIcons["Owl"].Draw(sb, Laptop.MAP_OFFSET + MAP_ICON_OFFSETS[Pos]);
			}
		}

		public override void DrawJumpscare(SpriteBatch sb)
		{
			if (!IsJumpscaring)
			{
				return;
			}

			Vector2 camOffset = jumpscareFrame > JUMPSCARE_SPIN_END_FRAME ? Vector2.Zero : Level.Office.CameraOffset;
			if (Pos == Position.VentExitW)
			{
				jumpscareSprites[jumpscareFrame].Draw(sb, JUMPSCARE_OFFSETS[jumpscareFrame] +
					camOffset + Level.Main.WindowCenter);
			}
			else if (Pos == Position.VentExitE)
			{
				jumpscareSprites[jumpscareFrame].Draw(sb, (JUMPSCARE_OFFSETS[jumpscareFrame] * new Vector2(-1, 1)) + 
					camOffset + Level.Main.WindowCenter);
			}
		}
	}
}

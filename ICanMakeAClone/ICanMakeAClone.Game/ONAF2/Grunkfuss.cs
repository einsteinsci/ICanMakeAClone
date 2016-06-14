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
	public class Grunkfuss : MonsterBase
	{
		public const int SPOOK_LINGER_FRAME = 4;
		public const int SPOOK_FRAME_COUNT = 11;
		public const int SPOOK_EXPOSURE_ACTIVE = 0;
		public const int SPOOK_EXPOSURE_RETREAT = 5;
		public const int SPOOK_ANIM_SPEED = 3;
		public const int START_PATIENCE = 2000;
		public const int START_PATIENCE_HARDBOILED = 800;
		public const int JUMPSCARE_FRAME_COUNT = 11;

		public const float SPOOK_LINGER_TIME = 3.0f;
		public const float EXPOSURE_RATE = 0.25f; // 4 secs
		public const float SCREAM_DELAY = 0.15f;

		public static readonly TimeSpan ACTIVATE_TIME = TimeSpan.FromHours(2.1);
		public static readonly TimeSpan ACTIVATE_TIME_HARDBOILED = TimeSpan.FromHours(1.0);

		public static readonly Vector2 SPOOK_MAIN_OFFSET = new Vector2(0, -5);
		public static readonly Vector2 ACTIVE_OFFSET = new Vector2(980, 17);
		public static readonly Vector2 WAITING_OFFSET = new Vector2(980, 40);
		public static readonly Vector2 PATIENCE_LINE1_OFFSET = new Vector2(1220, 95);
		public static readonly Vector2 PATIENCE_LINE2_OFFSET = new Vector2(1220, 125);

		public static readonly Vector2 MAP_ICON_OFFSET = new Vector2(1195, 322);
		public static readonly Vector2 MAP_ICON_OFFICE_OFFSET = new Vector2(990, 605);

		public static readonly Color PATIENCE_COLOR = new Color(210, 197, 224);

		public static readonly Vector2[] SPOOK_OFFSETS = new Vector2[] {
			new Vector2(200, 280),
			new Vector2(173, 270),
			new Vector2(173, 250),
			new Vector2(173, 235),
			new Vector2(173, 230),
			new Vector2(175, 233),
			new Vector2(180, 243),
			new Vector2(185, 255),
			new Vector2(195, 265),
			new Vector2(207, 280),
			new Vector2(240, 285),
		};

		public static readonly Vector2[] JUMPSCARE_OFFSETS = new Vector2[] {
			new Vector2(0, 96),
			new Vector2(0, 15),
			new Vector2(0, -29),
			new Vector2(0, -60),
			new Vector2(0, -60),
			new Vector2(0, -60),
			new Vector2(0, -60),
			new Vector2(0, -60),
			new Vector2(0, -60),
			new Vector2(0, -60),
			new Vector2(0, -60)
		};

		public bool IsExposing
		{ get; private set; }

		public int Patience
		{ get; private set; }

		public override string Name => "Grunkfuss";

		internal int spookFrame
		{ get; private set; }

		internal SpriteSheet spookSprites;
		internal SpriteSheet roomSprites;

		internal SpriteAnimation spookAnim;

		private TimeSpan _activateTime => Level.IsHardBoiled ? ACTIVATE_TIME_HARDBOILED : ACTIVATE_TIME;

		private bool _showSpook;

		private float _lingerTime = SPOOK_LINGER_FRAME;

		private Vector2 _patienceShakeOffset;

		public Grunkfuss(Level level) : base(level)
		{ }

		public void Activate()
		{
			IsActive = true;
			Level.Office.soundWormhole.Play();
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
			IsActive = false;
			Patience = Level.IsHardBoiled ? START_PATIENCE_HARDBOILED : START_PATIENCE;

			spookFrame = 0;

			_showSpook = false;
			_lingerTime = SPOOK_LINGER_TIME;

			screamDelay = new HelperTimer(TimeSpan.FromSeconds(SCREAM_DELAY), false, "ScreamDelay", () => {
				Manager.soundScreamClown.Play();
			});
		}

		public override void LoadContent(ContentManager content)
		{
			spookSprites = content.Load<SpriteSheet>("ONAF2/SpookGrunkfuss");
			roomSprites = content.Load<SpriteSheet>("ONAF2/RoomGrunkfuss");
			jumpscareSprites = content.Load<SpriteSheet>("ONAF2/JumpscareGrunkfuss");

			spookAnim = new SpriteAnimation(spookSprites, SPOOK_OFFSETS);
		}

		public override void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.C))
			{
				//_showSpook = !_showSpook;
				//
				//if (_showSpook)
				//{
				//	StartSpook();
				//}

				IsActive = !IsActive;

				if (IsActive)
				{
					Level.Office.soundWormhole.Play();
				}
				else
				{
					IsExposing = false;
				}
			}

			if (gt.FrameCount % 2 == 0)
			{
				float x = (float)Rand.NextDouble() * 4.0f - 2.0f;
				float y = (float)Rand.NextDouble() * 4.0f - 2.0f;
				_patienceShakeOffset = new Vector2(x, y);
			}

			if (Level.TimeSinceMidnight >= _activateTime && !IsActive)
			{
				Activate();
			}

			if (IsExposing && Level.Office.IsLightOn && !Manager.Eyesaur.IsExposing)
			{
				Level.Exposure += EXPOSURE_RATE * (float)gt.Elapsed.TotalSeconds * ExposureMultiplier;
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
					Manager.soundScreamClown.Stop();
				}
			}

			screamDelay.Update(gt);

			if (IsActive && gt.FrameCount % 2 == 0 && !_showSpook)
			{
				Patience--;

				if (Patience == 0)
				{
					StartSpook();
					Patience = Level.IsHardBoiled ? START_PATIENCE_HARDBOILED : START_PATIENCE;
				}
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
					//Patience = Level.IsHardBoiled ? START_PATIENCE_HARDBOILED : START_PATIENCE;
				}
			}
		}

		private void StartSpook()
		{
			_showSpook = true;
			spookFrame = 0;
			_lingerTime = SPOOK_LINGER_TIME;
			PlaySpookSound();
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
			if (cam == CameraIndex.Cam2 && !IsExposing)
			{
				if (IsActive)
				{
					roomSprites["Active"].Draw(sb, ACTIVE_OFFSET + offset);
				}
				else
				{
					roomSprites["Waiting"].Draw(sb, WAITING_OFFSET + offset);
				}
			}
		}

		public override void DrawOnLaptop(SpriteBatch sb)
		{
			if (IsActive)
			{
				if (Level.CHEAT_MapDebug)
				{
					if (IsExposing)
					{
						mapIcons["Grunkfuss"].Draw(sb, MAP_ICON_OFFICE_OFFSET);
					}
					else
					{
						mapIcons["Grunkfuss"].Draw(sb, MAP_ICON_OFFSET);
					}
				}

				if (Level.Main.UI.State == UIState.Laptop && Level.Laptop.ActiveCamera == CameraIndex.Cam2 && !IsExposing)
				{
					float px = sb.MeasureString(Level.hourFont, "PATIENCE:").X;
					Vector2 offset = PATIENCE_LINE1_OFFSET - new Vector2(px, 0) + _patienceShakeOffset;
					sb.DrawString(Level.hourFont, "PATIENCE:", offset, PATIENCE_COLOR);

					px = sb.MeasureString(Level.patienceFont, Patience.ToString()).X;
					offset = PATIENCE_LINE2_OFFSET - new Vector2(px, 0) + _patienceShakeOffset;
					sb.DrawString(Level.patienceFont, Patience.ToString(), offset, PATIENCE_COLOR);
				}
			}
		}

		public override void DrawJumpscare(SpriteBatch sb)
		{
			if (IsJumpscaring)
			{
				jumpscareSprites[jumpscareFrame].Draw(sb, JUMPSCARE_OFFSETS[jumpscareFrame] + Level.jumpscareShakeOffset * Vector2.UnitY);
			}
		}
	}
}

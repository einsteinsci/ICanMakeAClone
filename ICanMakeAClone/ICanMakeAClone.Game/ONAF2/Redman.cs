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
	public class Redman : MonsterBase
	{
		public const int JUMPSCARE_FRAME_COUNT = 16;
		public const int REDMAN_COUNT_DEATH = 10;
		public const int POPUP_RARITY = 2500;
		public const int BAR_PER_OFFSET = 24;

		public const float POPUP_BAR_TIME = 1.0f;
		public const float RETIREMENT_START_TIME = 10.0f;
		public const float WARNING_ICON_FLASH_TIME = 0.3f;

		public const double RED_SCREEN_OF_DEATH_TIME = 10.0;
		public const double SCREAM_DELAY = 0.15f;

		public static readonly Vector2 JUMPSCARE_OFFSET = new Vector2(640, 750); // slightly below bottom middle
		public static readonly Vector2 WINDOW_WARNING_OFFSET = new Vector2(138, 56);
		public static readonly Vector2 MAX_WINDOW_OFFSET = new Vector2(770, 400);
		public static readonly Vector2 MIN_WINDOW_OFFSET = new Vector2(50, 80);
		public static readonly Vector2 CANCEL_BUTTON_HITBOX_OFFSET = new Vector2(193, 175);
		public static readonly Vector2 CANCEL_BUTTON_HITBOX_SIZE = new Vector2(182, 56);
		public static readonly Vector2 WINDOW_BAR_OFFSET = new Vector2(166, 124);

		public bool IsReady => _retirementTime <= 0;

		public bool IsVirusUp
		{ get; private set; }

		public bool IsRedScreenOfDeathUp
		{ get; private set; }

		public int ProgressBarCount
		{ get; private set; }

		public override string Name => "Redman";

		internal SpriteSheet gameUISprites => Level.gameUISprites;

		internal InputRegion cancelButton;

		internal HelperTimer deathTimer;

		private float _timeUntilCountUp;
		private float _retirementTime;

		private float _warningIconFlashTime;
		internal bool warningIconVisible;

		private Vector2 _currentWindowOffset;

		public Redman(Level level) : base(level)
		{
			screamDelay = new HelperTimer(TimeSpan.FromSeconds(SCREAM_DELAY), false, "ScreamDelay", () => {
				Manager.soundScreamRedman.Play();
			});
		}

		public void StartVirus()
		{
			Reset();
			IsVirusUp = true;

			float x = Rand.Next((int)MIN_WINDOW_OFFSET.X, (int)MAX_WINDOW_OFFSET.X);
			float y = Rand.Next((int)MIN_WINDOW_OFFSET.Y, (int)MAX_WINDOW_OFFSET.Y);
			_currentWindowOffset = new Vector2(x, y);

			cancelButton = new InputRegion(_currentWindowOffset + CANCEL_BUTTON_HITBOX_OFFSET,
				CANCEL_BUTTON_HITBOX_SIZE, true, (n) => { Reset(); });

			Manager.soundPopup.Play();
		}

		public override void Reset()
		{
			base.Reset();
			
			IsVirusUp = false;
			IsRedScreenOfDeathUp = false;
			ProgressBarCount = 0;
			_timeUntilCountUp = 0;
			_retirementTime = RETIREMENT_START_TIME;
			cancelButton = null;

			warningIconVisible = false;
		}

		public override void LoadContent(ContentManager content)
		{
			jumpscareSprites = content.Load<SpriteSheet>("ONAF2/JumpscareRedman");
		}

		public override void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.R) && !IsVirusUp && !IsRedScreenOfDeathUp)
			{
				_retirementTime = 0;
				StartVirus();
			}

			if (IsVirusUp && !IsRedScreenOfDeathUp && Level.UI.State == UIState.Laptop)
			{
				cancelButton?.Update(input, Level.Main.WindowSize);
			}

			if (IsReady && !IsVirusUp && !IsRedScreenOfDeathUp)
			{
				if (Rand.Next(POPUP_RARITY) == 0)
				{
					StartVirus();
				}
			}

			if (IsVirusUp && !IsRedScreenOfDeathUp)
			{
				if (_timeUntilCountUp > 0)
				{
					_timeUntilCountUp -= (float)gt.Elapsed.TotalSeconds;
				}
				else
				{
					_timeUntilCountUp = POPUP_BAR_TIME;
					ProgressBarCount++;
				}
			}
			else if (_retirementTime > 0)
			{
				_retirementTime -= (float)gt.Elapsed.TotalSeconds;
			}

			if (ProgressBarCount == REDMAN_COUNT_DEATH && !IsRedScreenOfDeathUp)
			{
				IsRedScreenOfDeathUp = true;
				warningIconVisible = false;

				Manager.soundRedScreenOfDeath.Play();
				deathTimer = new HelperTimer(TimeSpan.FromSeconds(RED_SCREEN_OF_DEATH_TIME), true, "DeathTimer", BeginJumpscare);
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
					Manager.soundScreamRedman.Stop();
				}
			}

			screamDelay.Update(gt);

			if (IsRedScreenOfDeathUp)
			{
				deathTimer.Update(gt);
			}

			if (IsVirusUp && !IsRedScreenOfDeathUp)
			{
				_warningIconFlashTime -= (float)gt.Elapsed.TotalSeconds;

				if (_warningIconFlashTime <= 0)
				{
					warningIconVisible = !warningIconVisible;
					_warningIconFlashTime = WARNING_ICON_FLASH_TIME;
				}
			}
		}

		public override void DrawOnLaptop(SpriteBatch sb)
		{
			if (Level.UI.State != UIState.Laptop)
			{
				return;
			}

			if (IsVirusUp)
			{
				gameUISprites["RedmanEXE"].Draw(sb, _currentWindowOffset);

				if (warningIconVisible)
				{
					gameUISprites["RedmanWarning"].Draw(sb, _currentWindowOffset + WINDOW_WARNING_OFFSET);
				}

				int x = (int)WINDOW_BAR_OFFSET.X;
				for (int i = 0; i < ProgressBarCount; i++)
				{
					Vector2 barOffset = new Vector2(x, WINDOW_BAR_OFFSET.Y);
					gameUISprites["BarSegment"].Draw(sb, _currentWindowOffset + barOffset);

					x += BAR_PER_OFFSET;
				}
			}
		}

		public override void DrawJumpscare(SpriteBatch sb)
		{
			if (IsJumpscaring)
			{
				Vector2 shake = new Vector2(Level.jumpscareShakeOffset.X, 0);
				jumpscareSprites[jumpscareFrame].Draw(sb, JUMPSCARE_OFFSET + shake);
			}
		}
	}
}

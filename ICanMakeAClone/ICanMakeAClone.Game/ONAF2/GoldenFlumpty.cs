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
	public class GoldenFlumpty : MonsterBase
	{
		public const int APPEARANCE_RARITY = 15;
		public const int TWITCH_RARITY = 40;

		public const float JUMPSCARE_DELAY = 3.0f;
		public const float JUMPSCARE_TIME = 1.5f;

		public static readonly Vector2 MAP_ICON_OFFSET = new Vector2(225, 320);
		
		public static readonly TimeSpan ACTIVATE_TIME = TimeSpan.FromHours(4.5);

		public static readonly Vector2[] SPOOK_OFFSETS = new Vector2[] {
			new Vector2(855, 147),
			new Vector2(855, 108),
			new Vector2(855, 116)
		};

		public bool IsActive
		{ get; private set; }

		public bool IsInOffice
		{ get; private set; }

		public override bool ShakesOnJumpscare => false;

		internal SoundEffect soundDetect;

		private float _timeBeforeJumpscare;
		private float _jumpscareTimeLeft;

		private int _currentOfficeImage;

		private bool _DEBUG_willBeInOffice;

		public GoldenFlumpty(Level level) : base(level)
		{ }

		public void OnLaptopDown()
		{
			if (IsActive && Rand.Next(APPEARANCE_RARITY) == 0)
			{
				IsInOffice = true;
				_timeBeforeJumpscare = JUMPSCARE_DELAY;
				_DEBUG_willBeInOffice = false;
				_currentOfficeImage = 0;
				soundDetect.Play();
			}
		}

		public void Shoo()
		{
			IsInOffice = false;
			_timeBeforeJumpscare = JUMPSCARE_DELAY;
			_DEBUG_willBeInOffice = false;
			soundDetect.Stop();
		}

		public override void Reset()
		{
			base.Reset();

			IsInOffice = false;
			IsActive = false;
			_timeBeforeJumpscare = JUMPSCARE_DELAY;
			_DEBUG_willBeInOffice = false;
			_currentOfficeImage = 0;
			_jumpscareTimeLeft = JUMPSCARE_TIME;
		}

		public override void BeginJumpscare()
		{
			jumpscareFrame = 0;

			Level.IsJumpscaring = true;
			Manager.currentJumpscarer = this;

			Manager.soundScreamGolden.Play();
		}

		public override void LoadContent(ContentManager content)
		{
			jumpscareSprites = content.Load<SpriteSheet>("ONAF2/GoldenFlumpty");

			soundDetect = content.LoadSoundEffect(Level, "ONAF2/Sounds/GoldenFlumptyDetect");
		}

		public override void Update(GameTime gt, InputManager input)
		{
			if (input.IsKeyPressed(Keys.G) && Level.UI.State == UIState.Laptop)
			{
				_DEBUG_willBeInOffice = true;
				IsActive = true;
			}

			if (_DEBUG_willBeInOffice && Level.UI.State == UIState.Office)
			{
				IsInOffice = true;
				_DEBUG_willBeInOffice = false;
				soundDetect.Play();
			}

			if (Level.TimeSinceMidnight >= ACTIVATE_TIME)
			{
				IsActive = true;
			}
			
			if (IsInOffice && Level.UI.State == UIState.Office)
			{
				_timeBeforeJumpscare -= (float)gt.Elapsed.TotalSeconds;

				if (_timeBeforeJumpscare <= 0.0f)
				{
					BeginJumpscare();
				}

				if (gt.FrameCount % 3 == 0)
				{
					if (_currentOfficeImage == 0 && Rand.Next(TWITCH_RARITY) == 0)
					{
						_currentOfficeImage = Rand.Next(1, 3);
					}
					else
					{
						_currentOfficeImage = 0;
					}
				}
			}

			if (Level.IsJumpscaring && IsJumpscaring)
			{
				_jumpscareTimeLeft -= (float)gt.Elapsed.TotalSeconds;

				if (_jumpscareTimeLeft <= 0)
				{
					Level.UI.SetStateNextFrame(UIState.YouDied);
					Manager.soundAttackMusic.Stop();
					Manager.soundScreamGolden.Stop();
				}
			}

			//screamDelay?.Update(gt);
		}

		public override void DrawJumpscare(SpriteBatch sb)
		{
			if (IsJumpscaring)
			{
				jumpscareSprites["Jumpscare"].Draw(sb, Level.Main.WindowCenter);
			}
		}

		public override void DrawOnLaptop(SpriteBatch sb)
		{
			if (Level.CHEAT_MapDebug && (IsInOffice || _DEBUG_willBeInOffice))
			{
				mapIcons["GoldenFlumpty"].Draw(sb, MAP_ICON_OFFSET + Laptop.MAP_OFFSET);
			}
		}

		public void DrawInOffice(SpriteBatch sb, Vector2 camOffset)
		{
			if (!IsInOffice || IsJumpscaring)
			{
				return;
			}

			jumpscareSprites["Office" + _currentOfficeImage].Draw(sb, SPOOK_OFFSETS[_currentOfficeImage] + camOffset);
		}
	}
}
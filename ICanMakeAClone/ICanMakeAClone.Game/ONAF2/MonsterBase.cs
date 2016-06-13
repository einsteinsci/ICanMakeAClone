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
	public abstract class MonsterBase
	{
		public Level Level
		{ get; private set; }

		public MonsterManager Manager => Level.Monsters;

		public Random Rand => Level.Rand;

		public bool IsJumpscaring => Manager.currentJumpscarer == this;

		public virtual bool ShakesOnJumpscare => true;

		internal SpriteSheet mapIcons => Manager.mapIcons;

		internal List<SoundEffect> spookSounds => Manager.spookSounds;

		internal SpriteSheet jumpscareSprites;

		internal HelperTimer screamDelay;

		protected int jumpscareFrame;

		protected MonsterBase(Level level)
		{
			Level = level;
		}

		public virtual void Reset()
		{
			jumpscareFrame = 0;
		}

		public virtual void BeginJumpscare()
		{
			jumpscareFrame = 0;

			Level.IsJumpscaring = true;
			Manager.currentJumpscarer = this;

			Manager.soundAttackMusic.Play();

			screamDelay.Start();
		}

		public void PlaySpookSound()
		{
			int n = Level.Rand.Next(0, spookSounds.Count);
			spookSounds[n].Stop();
			spookSounds[n].Play();
		}

		public virtual void OnStatic()
		{ }

		public abstract void LoadContent(ContentManager content);

		public abstract void Update(GameTime gt, InputManager input);

		public abstract void DrawJumpscare(SpriteBatch sb);

		public virtual void DrawOnCamera(SpriteBatch sb, Vector2 offset, CameraIndex cam)
		{ }

		public virtual void DrawOnLaptop(SpriteBatch sb)
		{ }
	}
}

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
	public class MonsterManager
	{
		public Level Level
		{ get; private set; }

		public Flumpty Flumpty
		{ get; private set; }

		public BirthdayBoyBlam BBB
		{ get; private set; }

		public Eyesaur Eyesaur
		{ get; private set; }

		public Grunkfuss Clown
		{ get; private set; }

		public Owl Owl
		{ get; private set; }

		public Redman Redman
		{ get; private set; }

		public GoldenFlumpty GoldenFlumpty
		{ get; private set; }

		public List<MonsterBase> Monsters
		{ get; private set; }

		public bool IsSpooked => Flumpty.IsExposing || BBB.IsExposing || Eyesaur.IsExposing || Clown.IsExposing;
		public bool IsExposed => IsSpooked && Level.Office.IsLightOn;

		internal List<SoundEffect> spookSounds
		{ get; private set; }

		internal SoundEffect soundThunk1;
		internal SoundEffect soundThunk2;
		internal SoundEffect soundPopup;
		internal SoundEffect soundRedScreenOfDeath;

		internal SoundEffect soundAttackMusic;

		internal SoundEffect soundScreamGeneric;
		internal SoundEffect soundScreamClown;
		internal SoundEffect soundScreamOwl;
		internal SoundEffect soundScreamEyesaur;
		internal SoundEffect soundScreamRedman;
		internal SoundEffect soundScreamGolden;

		internal SpriteSheet mapIcons;

		internal MonsterBase currentJumpscarer;

		public MonsterManager(Level level)
		{
			Level = level;

			Flumpty = new Flumpty(Level);
			BBB = new BirthdayBoyBlam(Level);
			Eyesaur = new Eyesaur(Level);
			Clown = new Grunkfuss(Level);
			Owl = new Owl(Level);
			Redman = new Redman(Level);
			GoldenFlumpty = new GoldenFlumpty(Level);

			Monsters = new List<MonsterBase> { Flumpty, BBB, Eyesaur, Clown, Owl, Redman, GoldenFlumpty };

			spookSounds = new List<SoundEffect>();
		}

		public bool IsPositionLegal(Flumpty.Position pos)
		{
			if (pos == Flumpty.Position.Cam3_UpCloseAndPersonal && BBB.Pos == BirthdayBoyBlam.Position.Cam3)
			{
				return false;
			}

			if (pos == Flumpty.Position.Cam7 && BBB.Pos == BirthdayBoyBlam.Position.Cam7_KevinJr)
			{
				return false;
			}

			if (pos == Flumpty.Position.Cam3_UpCloseAndPersonal && Eyesaur.Pos == Eyesaur.Position.Cam3)
			{
				return false;
			}

			return true;
		}

		public bool IsPositionLegal(BirthdayBoyBlam.Position pos)
		{
			if (pos == BirthdayBoyBlam.Position.Cam3 && Flumpty.Pos == Flumpty.Position.Cam3_UpCloseAndPersonal)
			{
				return false;
			}

			if (pos == BirthdayBoyBlam.Position.Cam7_KevinJr && Flumpty.Pos == Flumpty.Position.Cam7)
			{
				return false;
			}

			if (pos == BirthdayBoyBlam.Position.Cam6_Thinking && Eyesaur.Pos == Eyesaur.Position.Cam6)
			{
				return false;
			}

			return true;
		}

		public bool IsPositionLegal(Eyesaur.Position pos)
		{
			if (pos == Eyesaur.Position.Cam3 && Flumpty.Pos == Flumpty.Position.Cam3_UpCloseAndPersonal)
			{
				return false;
			}

			if (pos == Eyesaur.Position.Cam6 && BBB.Pos == BirthdayBoyBlam.Position.Cam6_Thinking)
			{
				return false;
			}

			return true;
		}

		public bool IsPositionLegal(Owl.Position pos)
		{
			if (Flumpty.Pos == Flumpty.Position.Cam3_UpCloseAndPersonal)
			{
				if (pos == Owl.Position.VentEntryW || pos == Owl.Position.VentEntryE)
				{
					return false;
				}
			}

			return true;
		}

		public void Reset()
		{
			foreach (MonsterBase m in Monsters)
			{
				m.Reset();
			}

			currentJumpscarer = null;
		}

		public void StartJumpscareFromExposure()
		{
			if (currentJumpscarer != null)
			{
				return;
			}

			if (Eyesaur.IsExposing)
			{
				currentJumpscarer = Eyesaur;
			}
			else if (Clown.IsExposing)
			{
				currentJumpscarer = Clown;
			}
			else if (Flumpty.IsExposing)
			{
				currentJumpscarer = Flumpty;
			}
			else if (BBB.IsExposing)
			{
				currentJumpscarer = BBB;
			}

			currentJumpscarer.BeginJumpscare();
		}

		public void LoadContent(ContentManager content)
		{
			mapIcons = content.Load<SpriteSheet>("ONAF2/MapIcons");

			for (int i = 1; i <= 4; i++)
			{
				string path = "ONAF2/Sounds/Spotted" + i.ToString();
				spookSounds.Add(content.LoadSoundEffect(Level, path));
			}

			soundThunk1 = content.LoadSoundEffect(Level, "ONAF2/Sounds/OwlHitVent1");
			soundThunk2 = content.LoadSoundEffect(Level, "ONAF2/Sounds/OwlHitVent2");
			soundPopup = content.LoadSoundEffect(Level, "ONAF2/Sounds/RedmanPopUp");
			soundRedScreenOfDeath = content.LoadSoundEffect(Level, "ONAF2/Sounds/RedmanCrash");

			soundAttackMusic = content.LoadSoundEffect(Level, "ONAF2/Sounds/AttackMusic");

			soundScreamGeneric = content.LoadSoundEffect(Level, "ONAF2/Sounds/GenericScream");
			soundScreamClown = content.LoadSoundEffect(Level, "ONAF2/Sounds/ClownScream");
			soundScreamOwl = content.LoadSoundEffect(Level, "ONAF2/Sounds/OwlScream");
			soundScreamEyesaur = content.LoadSoundEffect(Level, "ONAF2/Sounds/EyesaurScream");
			soundScreamRedman = content.LoadSoundEffect(Level, "ONAF2/Sounds/RedmanScream");
			soundScreamGolden = content.LoadSoundEffect(Level, "ONAF2/Sounds/GoldenFlumptyScream");

			foreach (MonsterBase m in Monsters)
			{
				m.LoadContent(content);
			}
		}

		public void OnStatic()
		{
			if (Redman.IsRedScreenOfDeathUp)
			{
				return;
			}

			foreach (MonsterBase m in Monsters)
			{
				m.OnStatic();
			}
		}

		public void Update(GameTime gt, InputManager input)
		{
			foreach (MonsterBase m in Monsters)
			{
				m.Update(gt, input);
			}

			if (IsExposed && Level.exposureUpSound.PlayState != SoundPlayState.Playing)
			{
				Level.exposureUpSound.Play();
			}
			
			if (!IsExposed && Level.exposureUpSound.PlayState == SoundPlayState.Playing)
			{
				Level.exposureUpSound.Stop();
			}
		}

		public void DrawJumpscares(SpriteBatch sb)
		{
			currentJumpscarer?.DrawJumpscare(sb); // do nothing if null
		}

		public void DrawOnCamera(SpriteBatch sb, Vector2 offset, CameraIndex cam)
		{
			Owl.DrawOnCamera(sb, offset, cam);

			if (cam != CameraIndex.Cam6)
			{
				Eyesaur.DrawOnCamera(sb, offset, cam);
			}

			Clown.DrawOnCamera(sb, offset, cam);
			Flumpty.DrawOnCamera(sb, offset, cam);
			BBB.DrawOnCamera(sb, offset, cam);

			if (cam == CameraIndex.Cam6)
			{
				Eyesaur.DrawOnCamera(sb, offset, cam);
			}
		}

		public void DrawOnLaptop(SpriteBatch sb)
		{
			Eyesaur.DrawOnLaptop(sb);
			Flumpty.DrawOnLaptop(sb);
			BBB.DrawOnLaptop(sb);
			Clown.DrawOnLaptop(sb);
			Owl.DrawOnLaptop(sb);
			Redman.DrawOnLaptop(sb);
			GoldenFlumpty.DrawOnLaptop(sb);
		}
	}
}

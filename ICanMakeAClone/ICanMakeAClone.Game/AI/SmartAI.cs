using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICanMakeAClone.ONAF2;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Games;

using UltimateUtil.Fluid;

namespace ICanMakeAClone.AI
{
	public enum KnownOwlPos
	{
		Waiting,
		Glaring,
		VentW,
		VentE
	}

	public class SmartAI : AIBase
	{
		public const int TRIGGER_PATIENCE = 120;

		public const float TIME_PER_CAMERA = 0.15f;

		public static readonly Vector2 CAMERA_BUTTON_TARGET = new Vector2(32, 22);

		public override string SourceName => "SmartAI";

		public bool IsFlumptyAboutToSpook => !IsLaptopUp && Level.Monsters.Flumpty.AboutToSpook;

		public bool IsCoastClear => !Level.Monsters.IsSpooked && !IsFlumptyAboutToSpook && !PatienceInDanger &&
			KnownEyesaurPos != Eyesaur.Position.OfficeEntry;

		public bool GoldenFlumptyInOffice => Level.Monsters.GoldenFlumpty.IsInOffice;

		public bool PatienceInDanger => KnownPatience != null && KnownPatience.Value < TRIGGER_PATIENCE;

		public CameraIndex Cam => Level.Laptop.ActiveCamera;

		public int? KnownPatience
		{ get; protected set; }

		public Flumpty.Position KnownFlumptyPos
		{ get; protected set; }

		public BirthdayBoyBlam.Position KnownBBBPos
		{ get; protected set; }

		public Eyesaur.Position KnownEyesaurPos
		{ get; protected set; }

		public KnownOwlPos KnownOwlPos
		{ get; protected set; }

		public Redman Redman => Level.Monsters.Redman;

		private float _timeUntilNextCamera;

		private bool _justFlippedUpCamera;

		private bool _hasFoundFlumpty;
		private bool _hasFoundBBB;
		private bool _hasFoundEyesaur;

		private bool _isFlumptyExposing;
		private bool _isBBBExposing;
		private bool _isEyesaurExposing;

		private bool _isEyesaurClose;

		private bool _laptopUpForGoldenFlumpty;

		public SmartAI(Level level) : base(level)
		{ }

		public Vector2 GetClickPos(CameraIndex cam)
		{
			return Laptop.MAP_OFFSET + Laptop.MAP_CAMERA_OFFSETS[cam] + CAMERA_BUTTON_TARGET;
		}

		public CameraIndex? GetNextCam()
		{
			switch (Cam)
			{
			case CameraIndex.Cam3:
				return CameraIndex.Cam6;
			case CameraIndex.Cam6:
				return CameraIndex.Cam1;
			case CameraIndex.Cam1:
				return CameraIndex.Cam2;
			case CameraIndex.Cam2:
				return CameraIndex.Cam4;
			case CameraIndex.Cam4:
				return CameraIndex.Cam5;
			case CameraIndex.Cam5:
				return CameraIndex.Cam7;
			case CameraIndex.Cam7:
				return null;
			}

			return null;
		}

		public override List<string> GetDebugLines()
		{
			List<string> res = new List<string>();
			
			res.Add("Camera: " + Cam);
			res.Add("");
			res.Add("Flumpty at " + KnownFlumptyPos);
			res.Add("BBB at " + KnownBBBPos);
			res.Add("Eyesaur at " + KnownEyesaurPos);
			res.Add("Owl at " + KnownOwlPos);
			res.Add("");
			res.Add("Known Patience: " + KnownPatience);

			return res;
		}

		public void OnOwlThunk()
		{
			KnownOwlPos = KnownOwlPos.Glaring;
		}

		public override void Reset()
		{
			base.Reset();

			KnownFlumptyPos = Flumpty.Position.Cam3_Start;
			KnownBBBPos = BirthdayBoyBlam.Position.Cam6_Thinking;
			KnownEyesaurPos = Eyesaur.Position.Cam1_Waiting;
			KnownOwlPos = KnownOwlPos.Waiting;
			KnownPatience = null;
		}

		public void AdvanceCamera()
		{
			CameraIndex? cam = GetNextCam();

			if (cam != null)
			{
				ClickOn(GetClickPos(cam.Value));
				return;
			}

			if (_justFlippedUpCamera)
			{
				ClickOn(GetClickPos(CameraIndex.Cam3));
				_justFlippedUpCamera = false;
			}
			else
			{
				DoingLaptopThings = false;

				if (!_hasFoundFlumpty)
				{
					KnownFlumptyPos = Flumpty.Position.OfficeEntry;
					_isFlumptyExposing = true;
				}

				if (!_hasFoundBBB)
				{
					KnownBBBPos = BirthdayBoyBlam.Position.OfficeEntry;
					_isBBBExposing = true;
				}

				if (!_hasFoundEyesaur)
				{
					KnownEyesaurPos = Eyesaur.Position.OfficeEntry;
					_isEyesaurExposing = true;
				}
			}
		}

		public void CheckPostSpook()
		{
			if (_isFlumptyExposing && Level.Monsters.Flumpty.Pos != Flumpty.Position.OfficeEntry)
			{
				_isFlumptyExposing = false;
				KnownFlumptyPos = Flumpty.Position.Cam3_Start; // not necessarily true
			}

			if (_isBBBExposing && Level.Monsters.BBB.Pos != BirthdayBoyBlam.Position.OfficeEntry)
			{
				_isBBBExposing = false;
				KnownBBBPos = BirthdayBoyBlam.Position.Cam6_Kazotsky;
			}

			if (_isEyesaurExposing && Level.Monsters.Eyesaur.Pos != Eyesaur.Position.OfficeEntry)
			{
				_isEyesaurExposing = false;
				KnownEyesaurPos = Eyesaur.Position.Cam1_Waiting;
			}

			if (PatienceInDanger && Level.Monsters.Clown.IsRetreating)
			{
				KnownPatience = Level.IsHardBoiled ? Grunkfuss.START_PATIENCE_HARDBOILED : Grunkfuss.START_PATIENCE;
			}
		}

		public void ExtractCameraInfo()
		{
			CameraIndex? monsterCam = Level.Monsters.Flumpty.GetCameraVisible();
			if (monsterCam != null && monsterCam.Value == Cam)
			{
				KnownFlumptyPos = Level.Monsters.Flumpty.Pos;
				_hasFoundFlumpty = true;
			}

			monsterCam = Level.Monsters.BBB.GetCameraVisible();
			if (monsterCam != null && monsterCam.Value == Cam)
			{
				KnownBBBPos = Level.Monsters.BBB.Pos;
				_hasFoundBBB = true;
			}

			monsterCam = Level.Monsters.Eyesaur.GetCameraVisible();
			if (monsterCam != null && monsterCam.Value == Cam)
			{
				KnownEyesaurPos = Level.Monsters.Eyesaur.Pos;
				_hasFoundEyesaur = true;

				if (KnownEyesaurPos == Eyesaur.Position.Cam6)
				{
					_isEyesaurClose = true;
				}
			}

			if (_isEyesaurClose && Cam == CameraIndex.Cam6 && monsterCam == null)
			{
				KnownEyesaurPos = Eyesaur.Position.OfficeEntry;
				_isEyesaurExposing = true;
				DoingLaptopThings = false;
			}

			Owl.Position owlPos = Level.Monsters.Owl.Pos;

			if (Cam == CameraIndex.Cam3 && owlPos == Owl.Position.Perch)
			{
				KnownOwlPos = Level.Monsters.Owl.IsActive ? KnownOwlPos.Glaring : KnownOwlPos.Waiting;
			}
			if (Cam == CameraIndex.Cam4 && owlPos.IsAnyOf(Owl.Position.VentEntryE, Owl.Position.VentMidE))
			{
				KnownOwlPos = KnownOwlPos.VentE;
			}
			if (Cam == CameraIndex.Cam5 && owlPos.IsAnyOf(Owl.Position.VentEntryW, Owl.Position.VentMidW))
			{
				KnownOwlPos = KnownOwlPos.VentW;
			}

			if (Cam == CameraIndex.Cam2 && Level.Monsters.Clown.IsActive)
			{
				KnownPatience = Level.Monsters.Clown.Patience;
			}
		}

		public override void RunAI(GameTime gt)
		{
			if (IsLaptopUp) // Take in info and kill redman
			{
				if (Redman.IsVirusUp)
				{
					ClickOn(Redman.CurrentWindowOffset + REDMAN_CANCEL_TARGET);
				}

				ExtractCameraInfo();
			}

			if (GoldenFlumptyInOffice && !IsLaptopUp && !Level.Laptop.IsLaptopSwitching)
			{
				ToggleLaptop();
				_laptopUpForGoldenFlumpty = true;
			}

			if (IsLaptopUp && _laptopUpForGoldenFlumpty)
			{
				DoingLaptopThings = false;
				_laptopUpForGoldenFlumpty = false;
			}

			if (IsCoastClear && !IsLightOn)
			{
				TargetLight = true;
				return;
			}

			CheckPostSpook();

			if (KnownEyesaurPos == Eyesaur.Position.OfficeEntry && !Level.Monsters.Eyesaur.IsRetreating)
			{
				if (!IsLaptopUp && IsLightOn)
				{
					TargetLight = true;
				}

				// GO GO GO
				return;
			}

			if (Level.Monsters.IsExposed || IsFlumptyAboutToSpook || 
				KnownBBBPos == BirthdayBoyBlam.Position.OfficeEntry || 
				(KnownEyesaurPos == Eyesaur.Position.OfficeEntry && Level.Monsters.Eyesaur.IsRetreating) || 
				KnownFlumptyPos == Flumpty.Position.OfficeEntry ||
				PatienceInDanger)
			{
				if (Redman.IsVirusUp && Redman.ProgressBarCount > 7 && !IsLaptopUp)
				{
					ToggleLaptop();
					return;
				}

				if (!IsCoastClear)
				{
					if (IsLaptopUp)
					{
						DoingLaptopThings = false;
						return;
					}

					if (IsLightOn)
					{
						TargetLight = true;
						return;
					}
				}
			}

			if (KnownOwlPos == KnownOwlPos.VentE && Level.Office.Vent == VentState.Left)
			{
				if (IsLaptopUp)
				{
					DoingLaptopThings = false;
					return;
				}

				TargetedVent = VentState.Right;
				return;
			}

			if (KnownOwlPos == KnownOwlPos.VentW && Level.Office.Vent == VentState.Right)
			{
				if (IsLaptopUp)
				{
					DoingLaptopThings = false;
					return;
				}

				TargetedVent = VentState.Left;
				return;
			}

			if ((Level.LaptopBattery >= 1.0f && !IsLaptopUp && !Level.Laptop.IsLaptopSwitching) || 
				(Redman.IsVirusUp && Redman.ProgressBarCount > 7))
			{
				ToggleLaptop();
				_justFlippedUpCamera = true;

				_hasFoundFlumpty = false;
				_hasFoundBBB = false;
				_hasFoundEyesaur = false;
			}
			else if (IsLaptopUp)
			{
				_timeUntilNextCamera -= (float)gt.Elapsed.TotalSeconds;

				if (_timeUntilNextCamera <= 0)
				{
					_timeUntilNextCamera = TIME_PER_CAMERA;

					AdvanceCamera();
				}
			}
		}
	}
}
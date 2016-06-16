using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICanMakeAClone.ONAF2;

using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Input;

namespace ICanMakeAClone.AI
{
	// Draws its information directly from the Level object, essentially psychic
	public class PsychicAI : AIBase
	{
		public override string SourceName => "CheatyAI";

		public Flumpty.Position FlumptyPos => Level.Monsters.Flumpty.Pos;
		public BirthdayBoyBlam.Position BBBPos => Level.Monsters.BBB.Pos;
		public Eyesaur.Position EyesaurPos => Level.Monsters.Eyesaur.Pos;
		public Owl.Position OwlPos => Level.Monsters.Owl.Pos;

		public int Patience => Level.Monsters.Clown.Patience;
		public bool ClownSpooking => Level.Monsters.Clown.IsExposing;
		public bool GoldenFlumptyInOffice => Level.Monsters.GoldenFlumpty.IsInOffice;

		public Redman Redman => Level.Monsters.Redman;

		public PsychicAI(Level level) : base(level)
		{ }

		public override List<string> GetDebugLines()
		{
			List<string> res = new List<string>();

			if (TargetedVent != null)
			{
				res.Add("Target Vent: " + TargetedVent.Value);
			}
			
			if (TargetLight)
			{
				res.Add("Targeting Light");
			}

			if (DoingLaptopThings)
			{
				res.Add("Doing Laptop Things");
			}

			return res;
		}

		public override void RunAI(GameTime gt)
		{
			DoingLaptopThings = GoldenFlumptyInOffice || Redman.IsVirusUp;

			if (Redman.IsVirusUp)
			{
				DealWithRedman();
			}

			DealWithSpookers();

			DealWithOwl();

			if (GoldenFlumptyInOffice && !IsLaptopUp && !Level.Laptop.IsLaptopSwitching)
			{
				ToggleLaptop();
			}
		}

		private void DealWithOwl()
		{
			if (OwlPos == Owl.Position.VentBendE && Level.Office.Vent == VentState.Left)
			{
				TargetedVent = VentState.Right;
			}
			else if (OwlPos == Owl.Position.VentBendW && Level.Office.Vent == VentState.Right)
			{
				TargetedVent = VentState.Left;
			}
		}

		private void DealWithSpookers()
		{
			if (FlumptyPos == Flumpty.Position.OfficeEntry ||
				BBBPos == BirthdayBoyBlam.Position.OfficeEntry ||
				EyesaurPos == Eyesaur.Position.OfficeEntry ||
				Patience < 30)
			{
				if (IsLightOn && !Redman.IsVirusUp)
				{
					TargetLight = true;
				}
			}

			if (FlumptyPos != Flumpty.Position.OfficeEntry &&
				BBBPos != BirthdayBoyBlam.Position.OfficeEntry &&
				EyesaurPos != Eyesaur.Position.OfficeEntry &&
				!ClownSpooking && Patience >= 30 && !IsLightOn) // coast clear
			{
				TargetLight = true;
			}
		}

		private void DealWithRedman()
		{
			DoingLaptopThings = true;

			if (IsLaptopUp)
			{
				ClickOn(Redman.CurrentWindowOffset + REDMAN_CANCEL_TARGET);
				DoingLaptopThings = false;
			}
			else
			{
				if (IsLightOn)
				{
					ToggleLaptop();
				}
				else if (Redman.ProgressBarCount >= 8)
				{
					// risk exposure if redman count is close (probably will never happen)
					TargetLight = true;
				}
			}
		}

		public override void Update(GameTime gt, InputManager input)
		{
			base.Update(gt, input);

			if (input.IsKeyPressed(Keys.Space))
			{
				//TargetedVent = TargetedVent == VentState.Right ? VentState.Left : VentState.Right;
				TargetLight = true;
			}
		}
	}
}
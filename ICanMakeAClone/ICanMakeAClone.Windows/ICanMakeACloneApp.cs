using System.Windows.Forms;
using SiliconStudio.Xenko.Engine;
// ReSharper disable ClassNeverInstantiated.Global

namespace ICanMakeAClone
{
	internal class ICanMakeACloneApp
	{
		private static void Main(string[] args)
		{
			using (Game game = new CloneGame())
			{
				game.WindowCreated += (sender, eventArgs) =>
				{
					Form form = (Form)game.Window.NativeWindow.NativeHandle;
					form.ShowIcon = false;
					form.Text = "[Re-creation] One Night at Flumpty's 2";
				};
				game.Run();
			}
		}
	}
}

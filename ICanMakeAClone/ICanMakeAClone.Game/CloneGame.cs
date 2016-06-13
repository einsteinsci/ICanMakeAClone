using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICanMakeAClone.ONAF2;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Games;
using SiliconStudio.Xenko.Graphics;
using SiliconStudio.Xenko.Rendering;
using SiliconStudio.Xenko.Rendering.Composers;

namespace ICanMakeAClone
{
	public class CloneGame : Game
	{
		public Texture MapTexture
		{ get; private set; }

		public Vector2 WindowSize
		{
			get
			{
				Vector3 virtualResolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width,
				GraphicsDevice.Presenter.BackBuffer.Height, 20f);
				return virtualResolution.XY();
			}
		}

		public OnafMain ONAF2Component
		{ get; private set; }

		private SpriteBatch _spriteBatch;
		private SamplerState _sampler;

		private bool _hasLoaded;

		public void LoadRendering()
		{
			Vector3 virtualResolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width,
				GraphicsDevice.Presenter.BackBuffer.Height, 20f);

			ONAF2Component = new OnafMain(this);

			Scene scene = SceneSystem.SceneInstance.Scene;
			var compositor = (SceneGraphicsCompositorLayers)scene.Settings.GraphicsCompositor;
			compositor.Master.Renderers.Insert(1, new SceneDelegateRenderer(Draw));

			_spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = virtualResolution };

			_sampler = SamplerState.New(GraphicsDevice,
				new SamplerStateDescription(TextureFilter.Point, TextureAddressMode.Wrap));

			LoadRealContent();

			_hasLoaded = true;
		}

		public void LoadRealContent()
		{
			MapTexture = Content.Load<Texture>("Map");

			ONAF2Component.LoadContent(Content);
		}

		public void Draw(RenderDrawContext rdc, RenderFrame frame)
		{
			_spriteBatch.Begin(rdc.GraphicsContext, SpriteSortMode.Deferred, BlendStates.NonPremultiplied, _sampler);

			// render me here
			//_spriteBatch.Draw(MapTexture, new Vector2(30, 150));
			ONAF2Component.Draw(rdc.RenderContext.Time, _spriteBatch);

			_spriteBatch.End();
		}

		protected override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (!_hasLoaded)
			{
				LoadRendering();
			}

			// finally some update logic
			ONAF2Component.Update(gameTime, Input);
		}
	}
}

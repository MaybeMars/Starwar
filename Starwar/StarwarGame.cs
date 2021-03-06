﻿

namespace Starwar
{
    using System;
    using Common;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Audio;
    using Microsoft.Xna.Framework.Graphics;
    using Microsoft.Xna.Framework.Input;
    using Starwar.Sprites;

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class StarwarGame : Game
    {
        private const string SpaceshipContentName = "spaceship";
        private const string LaserContentName = "laser";
        private const string MessageFontContentName = "message";
        private const string ScoreFontContentName = "scoreFont";
        private const string Enemy4ContentName = "enemy4";
        private const string ExplosionsContentName = "explosions";
        private const string BackgroundContentName = "background";
        private const string ParallaxStarContentName = "stars";
        // private const string BgmContentName = "bgm";
        private const string ExplosionSoundContentName = "explosionSound";
        private const string LaserSoundContentName = "laserSound";
        
        private readonly GraphicsDeviceManager graphics;
        private readonly FrameCounter frameCounter = new FrameCounter();
        private readonly SpritePool<LaserSprite> laserPool;
        private readonly SpritePool<EnemySprite> enemyPool;
        private readonly SpritePool<AnimatedSprite> explosionPool;
        private readonly SpritePool<ParallaxStarSprite> starPool;
        private readonly TimeSpan laserUpdateThreshold;
        private SpriteGenerator<EnemySprite> enemyGenerator;
        private SpriteGenerator<ParallaxStarSprite> starGenerator;
        
        // texture and content
        private SpriteFont messageFont;
        private SpriteFont scoreFont;
        
        private Texture2D spaceshipTexture;
        private Texture2D laserTexture;
        private Texture2D enemy4Texture;
        private Texture2D explosionTexture;
        private Texture2D backgroundTexture;
        private Texture2D starTexture;
        private SoundEffect bgmEffect;
        private SoundEffect explosionSoundEffect;
        private SoundEffect laserSoundEffect;

        private SoundEffectInstance laserSound;
        private SoundEffectInstance explosionSound;
        private GameOverScene gameOverScene;
        private TimeSpan currentLaserUpdateTimeSpan = TimeSpan.Zero;

        // sprites
        private SpriteBatch spriteBatch;
        private SpaceshipSprite spaceshipSprite;
        private BackgroundSprite backgroundSprite;

        private int score;

        private readonly Settings settings;
        
        public StarwarGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            laserPool = new SpritePool<LaserSprite>(graphics);
            enemyPool = new SpritePool<EnemySprite>(graphics);
            explosionPool = new SpritePool<AnimatedSprite>(graphics);
            starPool = new SpritePool<ParallaxStarSprite>(graphics);
            settings = Settings.ReadSettings();

            laserUpdateThreshold = TimeSpan.FromMilliseconds(1000.0F/settings.NumOfLasersPerSecond);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            graphics.IsFullScreen = settings.FullScreen;
            graphics.PreferredBackBufferWidth = GraphicsDevice.DisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsDevice.DisplayMode.Height;
            //graphics.PreferredBackBufferWidth = 1024;
            //graphics.PreferredBackBufferHeight = 768;
            graphics.ApplyChanges();
            base.Initialize();
            //graphics.ToggleFullScreen();
            Window.AllowUserResizing = true;
            graphics.ApplyChanges();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load texture and contents
            messageFont = this.Content.Load<SpriteFont>(MessageFontContentName);
            scoreFont = this.Content.Load<SpriteFont>(ScoreFontContentName);
            
            laserTexture = this.Content.Load<Texture2D>(LaserContentName);
            spaceshipTexture = this.Content.Load<Texture2D>(SpaceshipContentName);
            enemy4Texture = this.Content.Load<Texture2D>(Enemy4ContentName);
            explosionTexture = this.Content.Load<Texture2D>(ExplosionsContentName);
            backgroundTexture = this.Content.Load<Texture2D>(BackgroundContentName);
            starTexture = this.Content.Load<Texture2D>(ParallaxStarContentName);
            //bgmEffect = this.Content.Load<SoundEffect>(BgmContentName);
            bgmEffect = this.Content.Load<SoundEffect>(this.settings.BgmSoundEffect);
            explosionSoundEffect = this.Content.Load<SoundEffect>(ExplosionSoundContentName);
            explosionSound = explosionSoundEffect.CreateInstance();
            explosionSound.Volume = 1.0F;
            
            laserSoundEffect = this.Content.Load<SoundEffect>(LaserSoundContentName);
            laserSound = laserSoundEffect.CreateInstance();
            laserSound.Volume = 1.0F;

            // create sprites
            spaceshipSprite = new SpaceshipSprite(spaceshipTexture);
            backgroundSprite = new BackgroundSprite(backgroundTexture, graphics);

            // create sprite generators
            enemyGenerator =
                new SpriteGenerator<EnemySprite>(
                    () =>
                        new EnemySprite(enemy4Texture,
                            new Vector2(Utils.GetRandomNumber(1, GraphicsDevice.Viewport.Width - enemy4Texture.Width), 1),
                            Utils.GetRandomNumber(5, 10)), enemyPool, TimeSpan.FromMilliseconds(1000.0F/settings.NumOfEnemiesPerSecond));

            starGenerator =
                new SpriteGenerator<ParallaxStarSprite>(() => new ParallaxStarSprite(starTexture, new Vector2(
                    Utils.GetRandomNumber(1,
                        GraphicsDevice.Viewport.Width - starTexture.Width), 1), Utils.GetRandomNumber(5, 20)), starPool,
                    TimeSpan.FromMilliseconds(100));

            gameOverScene = new GameOverScene(this, () => !spaceshipSprite.IsActive, () =>
            {
                this.enemyPool.Clear();
                this.laserPool.Clear();
                this.explosionPool.Clear();

                this.enemyGenerator.IsActive = false;

                if (explosionSound != null && !explosionSound.IsDisposed)
                {
                    explosionSound.Stop(true);
                    explosionSound.Dispose();
                }
                if (laserSound != null && !laserSound.IsDisposed)
                {
                    laserSound.Stop(true);
                    laserSound.Dispose();
                }
                bgmEffect.Dispose();
            }) {IsActive = !settings.LiveForever};

            var bgm = bgmEffect.CreateInstance();
            bgm.IsLooped = true;
            bgm.Play();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            this.backgroundSprite.Update(gameTime);

            if (spaceshipSprite.IsActive)
            {
                var mouseState = Mouse.GetState();

                spaceshipSprite.X = mouseState.X - spaceshipSprite.Width/2;
                spaceshipSprite.Y = mouseState.Y - spaceshipSprite.Height/2;

                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    this.currentLaserUpdateTimeSpan += gameTime.ElapsedGameTime;
                    if (this.currentLaserUpdateTimeSpan >= laserUpdateThreshold)
                    {
                        var laserSprite = new LaserSprite(laserTexture,
                            new Vector2(spaceshipSprite.X + (spaceshipSprite.Width - laserTexture.Width)/2.0F,
                                spaceshipSprite.Y));
                        PlaySound(laserSoundEffect, laserSound);
                        this.laserPool.Add(laserSprite);
                        this.currentLaserUpdateTimeSpan = TimeSpan.Zero;
                    }
                }

                foreach (var enemy in this.enemyPool.Sprites)
                {
                    foreach (var laser in this.laserPool.Sprites)
                    {
                        if (laser.CollidesWith(enemy))
                        {
                            score += 10;
                            PlaySound(explosionSoundEffect, explosionSound);
                            laser.IsActive = false;
                            enemy.IsActive = false;
                            var explosionSprite = new AnimatedSprite(explosionTexture, new Vector2(enemy.X, enemy.Y),
                                new SpriteSheet(64, 64, 16, 0, 0), TimeSpan.FromMilliseconds(5), 1);
                            this.explosionPool.Add(explosionSprite);
                        }
                    }

                    if (gameOverScene.IsActive && enemy.CollidesWith(spaceshipSprite))
                    {
                        PlaySound(explosionSoundEffect, explosionSound);
                        enemy.IsActive = false;
                        spaceshipSprite.IsActive = false;
                        var explosionSprite = new AnimatedSprite(explosionTexture,
                            new Vector2(spaceshipSprite.X, spaceshipSprite.Y),
                            new SpriteSheet(64, 64, 16, 0, 0), TimeSpan.FromMilliseconds(5), 1);
                        this.explosionPool.Add(explosionSprite);
                    }
                }
            }

            this.gameOverScene.Update(gameTime);

            this.enemyGenerator.Update(gameTime);
            this.laserPool.Update(gameTime);
            this.explosionPool.Update(gameTime);

            this.starGenerator.Update(gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            frameCounter.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
            var fps = string.Format("FPS: {0}", frameCounter.AverageFramesPerSecond);

            spriteBatch.Begin();

            //this.explosionSprite.Draw(gameTime, spriteBatch);
            backgroundSprite.Draw(gameTime, spriteBatch);
            starGenerator.Draw(gameTime, spriteBatch);

            enemyGenerator.Draw(gameTime, spriteBatch);

            if (spaceshipSprite.IsActive)
            {
                spaceshipSprite.Draw(gameTime, spriteBatch);
            }
            
            laserPool.Draw(gameTime, spriteBatch);

            explosionPool.Draw(gameTime, spriteBatch);

            this.gameOverScene.Draw(gameTime, spriteBatch);

            var scoreString = string.Format("Score: {0}", this.score.ToString().PadLeft(7, '0'));
            var scoreStringSize = scoreFont.MeasureString(scoreString);
            spriteBatch.DrawString(scoreFont, scoreString, new Vector2(GraphicsDevice.Viewport.Width - scoreStringSize.X - 20, 5), Color.Yellow);

            #region Output Debug Information

            if (settings.ShowDebugInfo)
            {
                // FPS
                spriteBatch.DrawString(messageFont, fps, Vector2.One, Color.Yellow);

                // Spacecraft Information
                var spaceCraftInfo = string.Format("[Spacecraft] X = {0}, Y = {1}", spaceshipSprite.X, spaceshipSprite.Y);
                spriteBatch.DrawString(messageFont, spaceCraftInfo, new Vector2(1, 15), Color.Yellow);

                // Laser Pool Information
                var laserPoolCountStr = string.Format("[Laser Pool] Count = {0}", this.laserPool.Count);
                spriteBatch.DrawString(messageFont, laserPoolCountStr, new Vector2(1, 30), Color.Yellow);

                // Enemy Pool Information
                var enemyPoolCountStr = string.Format("[Enemy Pool] Count = {0}", this.enemyPool.Count);
                spriteBatch.DrawString(messageFont, enemyPoolCountStr, new Vector2(1, 45), Color.Yellow);

                // Explosion Pool Information
                var explPoolCountStr = string.Format("[Expln Pool] Count = {0}", this.explosionPool.Count);
                spriteBatch.DrawString(messageFont, explPoolCountStr, new Vector2(1, 60), Color.Yellow);

                // Viewport Information
                var viewportInfoStr = string.Format("[ Viewport ] Width = {0}, Height = {1}",
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);
                spriteBatch.DrawString(messageFont, viewportInfoStr, new Vector2(1, 75), Color.Yellow);

                // Game time Information
                var gameTimeInfoStr = string.Format("[ GameTime ] Total = {0}", gameTime.TotalGameTime);
                spriteBatch.DrawString(messageFont, gameTimeInfoStr, new Vector2(1, 90), Color.Yellow);
            }


            #endregion

            spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.spaceshipTexture.Dispose();
                this.laserTexture.Dispose();
                this.enemy4Texture.Dispose();
                this.explosionTexture.Dispose();
                this.backgroundTexture.Dispose();
                this.bgmEffect.Dispose();
                this.explosionSoundEffect.Dispose();
                this.laserSoundEffect.Dispose();
            }
            base.Dispose(disposing);
        }

        private void PlaySound(SoundEffect soundEffect, SoundEffectInstance soundEffectInstance, float volume = 1.0F)
        {
            if (soundEffectInstance != null &&
                !soundEffectInstance.IsDisposed)
            {
                soundEffectInstance.Dispose();
            }
            soundEffectInstance = soundEffect.CreateInstance();
            soundEffectInstance.Play();
        }
    }
}

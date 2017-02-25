using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Media;
namespace Coding_Lab_4
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        Song song;
        
        // gameplay mechanics
        Vector2 window = new Vector2(800, 600);
        float initialBallSpeed = 8;
        float aiPaddleSpeed = 10;
        int numBricks = 5;
        int timer = 0;
        int initialPaddleSpeed = 8;
        int slimedPaddleSpeed = 3;

        
        // temporary or constant variables
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;
        Vector2 leftPaddle, ball, rightPaddle;
        Vector2 ballVelocity;
        Vector2 goalArea;
        Vector2 powerupPosition;
        int powerupType = 0;
        double powerupTimer = 0;
        float ballSpeed;
        int[] leftHealth;
        int[] rightHealth;
        string goalText;
        bool goalState, menuState = true;
        double leftScore, rightScore;
        int brickWidth = 50;
        int brickHeight;
        bool frozen = false, slimy = false;
        int lastPaddle; // 1 for left, 2 for right
        int menuSelected = 1;
        int gamemode;

        SpriteFont titleFont;

        public void drawRectangle(int x, int y, int width, int height, Color fill, Color outline)
        {
            // credit to Stack Overflow post
            // http://stackoverflow.com/questions/5751732/draw-rectangle-in-xna-using-spritebatch

            Texture2D outlineTexture = new Texture2D(graphics.GraphicsDevice, width+2, height+2);
            Texture2D fillTexture = new Texture2D(graphics.GraphicsDevice, width, height);

            Color[] outlineData = new Color[(width+2) * (height+2)];
            for (int i = 0; i < outlineData.Length; ++i) outlineData[i] = outline;
            outlineTexture.SetData(outlineData);

            Vector2 outlineCoor = new Vector2(x-1, y-1);

            Color[] fillData = new Color[width * height];
            for (int i = 0; i < fillData.Length; ++i) fillData[i] = fill;
            fillTexture.SetData(fillData);

            Vector2 fillCoor = new Vector2(x, y);

            spriteBatch.Draw(outlineTexture, outlineCoor, outline);
            spriteBatch.Draw(fillTexture, fillCoor, fill);
        }

        public void drawLine(int x, int y, int width, int height, Color color)
        {
            if (width >= height)
                for (int i = x; i < x + width; i++)
                    spriteBatch.Draw(Content.Load<Texture2D>("dot"), new Vector2(i, y + (i - x) * height / width), Color.White);
            else
                for (int i = y; i < y + height; i++)
                    spriteBatch.Draw(Content.Load<Texture2D>("dot"), new Vector2(x + (i - y) * width / height, i), Color.White);
        }

        public bool collide(Vector2 coordinates1, Vector2 coordinates2, int radius)
        {
            Vector2 center1, center2;

            center1 = coordinates1 + new Vector2(radius, radius);
            center2 = coordinates2 + new Vector2(radius, radius);

            if (Math.Sqrt(Math.Pow(center2.X - center1.X, 2) + Math.Pow(center2.Y - center1.Y, 2)) <= 64)
                return true;

            return false;
        }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = (int)window.X;
            graphics.PreferredBackBufferHeight = (int)window.Y;
            goalArea.Y = 100;

            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            ballSpeed = initialBallSpeed;
            powerupPosition = window;

            base.Initialize();

           
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {

            //load music
            song = Content.Load<Song>("Song");
            MediaPlayer.Play(song);
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Content.Load<SpriteFont>("Courier New");
            titleFont = Content.Load<SpriteFont>("Magneto");

            leftHealth = new int[numBricks];
            rightHealth = new int[numBricks];

            leftPaddle = new Vector2(brickWidth + 10, 50f);
            ball = new Vector2(window.X / 2, window.Y / 2);
            rightPaddle = new Vector2(window.X - 24 - (brickWidth + 10), 536f);
            ballVelocity = new Vector2(ballSpeed, ballSpeed);
            goalText = "";
            brickHeight = (int)(window.Y / numBricks);

            for (int i=0; i<numBricks; i++)
            {
                leftHealth[i] = 2;
                rightHealth[i] = 2;
            }

            // TODO: use this.Content to load your game content here
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
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
            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            // TODO: Add your update logic here
            if (goalState)
            {
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    goalState = false;
                    goalText = "";

                    for (int i = 0; i < numBricks; i++)
                    {
                        leftHealth[i] = 2;
                        rightHealth[i] = 2;
                    }
                }
            }
            else if (!menuState)
            {
                #region ball stuff
                // collisions with paddle
                if (ball.X <= leftPaddle.X + 24 && ball.Y + 32 >= leftPaddle.Y && ball.Y <= leftPaddle.Y + 64)
                {
                    ballVelocity = new Vector2(ballSpeed, (ball.Y - (leftPaddle.Y - 32) - 48) / 48 * ballSpeed);
                    Content.Load<SoundEffect>("hit").Play();
                    lastPaddle = 1;
                }
                else if (ball.X + 32 >= rightPaddle.X && ball.Y + 32 >= rightPaddle.Y && ball.Y <= rightPaddle.Y + 64)
                {
                    ballVelocity = new Vector2(-ballSpeed, (ball.Y - (rightPaddle.Y - 32) - 48) / 48 * ballSpeed);
                    Content.Load<SoundEffect>("hit").Play();
                    lastPaddle = 2;
                }
                
                // collisions with top and bottom walls
                if (ball.Y <= 0 || ball.Y >= window.Y - 32) ballVelocity.Y *= -1;

                // goals
                if (ball.X <= -32)
                {
                    ball = new Vector2(window.X / 2, window.Y / 2);
                    ballVelocity = new Vector2(ballSpeed, new Random().Next((int)-ballSpeed, (int)ballSpeed));
                    Content.Load<SoundEffect>("friendlyGoal").Play();
                    goalState = true;
                    goalText = "GOAL!  You have gained one point!\nClick to continue!";
                    goalArea.X = 200;
                    rightScore += 1;
                }
                else if (ball.X >= window.X)
                {
                    ball = new Vector2(300f, 300f);
                    ballVelocity = new Vector2(-ballSpeed, new Random().Next((int)-ballSpeed, (int)ballSpeed));
                    Content.Load<SoundEffect>("enemyGoal").Play();
                    goalState = true;
                    goalText = "GOAL!  Your enemy has gained one point!\nClick to continue!";
                    goalArea.X = 175;
                    leftScore += 1;
                }

                // collisions with bricks
                if (ball.X <= brickWidth && leftHealth[(int)(ball.Y / brickHeight)] != 0)
                {
                    if (leftHealth[(int)(ball.Y / brickHeight)] == 1) rightScore += 1.0 / numBricks;

                    leftHealth[(int)(ball.Y / brickHeight)]--;
                    ballVelocity.X *= -1;
                    Content.Load<SoundEffect>("brick").Play();
                }
                else if (ball.X + 32 >= window.X - brickWidth && rightHealth[(int)(ball.Y / brickHeight)] != 0)
                {
                    if (rightHealth[(int)(ball.Y / brickHeight)] == 1) leftScore += 1.0 / numBricks;

                    rightHealth[(int)(ball.Y / brickHeight)]--;
                    ballVelocity.X *= -1;
                    Content.Load<SoundEffect>("brick").Play();
                }

                // collision with powerups
                if (collide(ball, powerupPosition, 32))
                {
                    powerupPosition = window; // move it off-screen

                    if (powerupType == 1) frozen = true;
                    else if (powerupType == 2) ballSpeed += 5;
                    else if (powerupType == 3) slimy = true;

                    powerupTimer = 3;
                }

                ball += ballVelocity;
                #endregion

                #region ai paddle stuff
                if (gamemode == 1)
                {
                    if (!(frozen && lastPaddle == 1) && !slimy)
                    {
                        KeyboardState ks = Keyboard.GetState();
                        if (ks.IsKeyDown(Keys.S))
                            leftPaddle.Y += initialPaddleSpeed;
                        else if (ks.IsKeyDown(Keys.W))
                            leftPaddle.Y -= initialPaddleSpeed;
                    }
                    else if (slimy)
                    {
                        KeyboardState ks = Keyboard.GetState();
                        if (ks.IsKeyDown(Keys.S))
                            leftPaddle.Y += slimedPaddleSpeed;
                        else if (ks.IsKeyDown(Keys.W))
                            leftPaddle.Y -= slimedPaddleSpeed;
                    }
                }
                else if (gamemode == 2)
                {
                    if (ball.X <= 100 && !(frozen && lastPaddle == 2))
                    {
                        if (ball.Y > leftPaddle.Y) leftPaddle.Y += aiPaddleSpeed;
                        else if (ball.Y < leftPaddle.Y) leftPaddle.Y -= aiPaddleSpeed;
                    }
                }
                #endregion

                #region player paddle stuff
                if (!(frozen && lastPaddle == 1) && !slimy)
                {
                    KeyboardState ks = Keyboard.GetState();
                    if (ks.IsKeyDown(Keys.Down))
                        rightPaddle.Y += initialPaddleSpeed;
                    else if (ks.IsKeyDown(Keys.Up))
                        rightPaddle.Y -= initialPaddleSpeed;
                }
                else if (slimy)
                {
                    KeyboardState ks = Keyboard.GetState();
                    if (ks.IsKeyDown(Keys.Down))
                        rightPaddle.Y += slimedPaddleSpeed;
                    else if (ks.IsKeyDown(Keys.Up))
                        rightPaddle.Y -= slimedPaddleSpeed;
                }

                #endregion

                #region powerup stuff
                if (timer % 300 == 0)
                {    
                    powerupType = new Random().Next(1, 4);

                    powerupPosition.X = new Random().Next(brickWidth + 100, (int)window.X - brickWidth - 100);
                    powerupPosition.Y = new Random().Next(0, (int)window.Y - 32);
                }
                #endregion
            }

            timer++;
            if (powerupTimer > 0) powerupTimer -= 0.01;
            else
            {
                frozen = false;
                ballSpeed = initialBallSpeed;
                slimy = false;
            }

            base.Update(gameTime);
        }
        
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            spriteBatch.Begin();
            
            if (menuState)
            {
                // credit to Stack Overflow post
                // http://stackoverflow.com/questions/6632723/how-to-make-a-texture2d-50-transparent-xna
                drawRectangle(0, 0, (int)window.X, (int)window.Y, new Color(0, 0, 0, 100), Color.Black);

                spriteBatch.DrawString(titleFont, "PONG", new Vector2(200, 50), Color.White);
                spriteBatch.DrawString(titleFont, "Breaker", new Vector2(250, 100), Color.White);

                if (menuSelected == 1)
                {
                    spriteBatch.DrawString(titleFont, "Play in 1 vs. 1 mode", new Vector2(100, 350), Color.Yellow);
                    spriteBatch.DrawString(titleFont, "Play in vs. AI mode", new Vector2(100, 400), Color.White);

                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        menuState = false;
                        gamemode = menuSelected;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Down)) menuSelected = 2;
                }
                else if (menuSelected == 2)
                {
                    spriteBatch.DrawString(titleFont, "Play in 1 vs. 1 mode", new Vector2(100, 350), Color.White);
                    spriteBatch.DrawString(titleFont, "Play in vs. AI mode", new Vector2(100, 400), Color.Yellow);

                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        menuState = false;
                        gamemode = menuSelected;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Up)) menuSelected = 1;
                }
            }
            else
            {
                spriteBatch.Draw(Content.Load<Texture2D>("left_paddle"), leftPaddle, Color.White);
                spriteBatch.Draw(Content.Load<Texture2D>("small_ball"), ball, Color.White);
                spriteBatch.Draw(Content.Load<Texture2D>("right_paddle"), rightPaddle, Color.White);
                spriteBatch.DrawString(spriteFont, goalText, goalArea, Color.Yellow);

                for (int i = 0; i < numBricks; i++)
                {
                    int x = (int)window.X - brickWidth;
                    int y = i * brickHeight;

                    if (leftHealth[i] > 0) drawRectangle(0, y, brickWidth, brickHeight, Color.Red, Color.Black);
                    if (leftHealth[i] == 1)
                    {
                        drawLine(0, y + 50, 20, 5, Color.Black);
                        drawLine(20, y + 55, 5, -10, Color.Black);
                        drawLine(25, y + 45, 10, -5, Color.Black);
                        drawLine(35, y + 40, 15, 10, Color.Black);
                    }

                    if (rightHealth[i] > 0) drawRectangle(x, y, brickWidth, brickHeight, Color.Red, Color.Black);
                    if (rightHealth[i] == 1)
                    {
                        drawLine(x, y + 50, 20, 5, Color.Black);
                        drawLine(x + 20, y + 55, 5, -10, Color.Black);
                        drawLine(x + 25, y + 45, 10, -5, Color.Black);
                        drawLine(x + 35, y + 40, 15, 10, Color.Black);
                    }
                }

                spriteBatch.DrawString(spriteFont, "Computer: " + leftScore, Vector2.Zero, Color.Yellow);
                spriteBatch.DrawString(spriteFont, "You: " + rightScore, new Vector2(window.X - 90, 0), Color.Yellow);

                switch (powerupType)
                {
                    case 1:
                        spriteBatch.Draw(Content.Load<Texture2D>("freeze"), powerupPosition, Color.White);
                        break;
                    case 2:
                        spriteBatch.Draw(Content.Load<Texture2D>("speed"), powerupPosition, Color.White);
                        break;
                    case 3:
                        spriteBatch.Draw(Content.Load<Texture2D>("slime"), powerupPosition, Color.White);
                        break;
                }
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

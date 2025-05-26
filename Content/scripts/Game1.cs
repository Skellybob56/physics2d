using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System;
using System.Threading;
using System.Collections.Generic;
using Physics.Content.scripts;

namespace Physics
{
    public class Game1 : Game
    {
        public static Game1 instance { get; private set; }
        public static Camera camera { get; private set; }

        private GraphicsDeviceManager _graphics;

        public Effect ellipseEffect;

        private Thread fixedUpdateThread;
        private const int FixedUpdateIntervalMSec = 10;
        private const double FixedUpdateInterval = FixedUpdateIntervalMSec / 1000d;
        private const float FixedUpdateIntervalF = (float)FixedUpdateInterval;
        private Stopwatch stopwatchFixedUpdate;
        long currentTick = 0;
        private long currentTimeFixedMSec;
        private int remainingTimeFixedMSec;
        private long nextTickTimeFixedMSec;
        private long tickStartTimeFixedMSec;
        private long tickStartTimeSharedMSec;
        private long tickStartTimeUnfixedMSec;
        private long lastTickStartTimeUnfixedMSec;
        const float tickLengthPredictionEntropy = 0.875f;
        private int tickLengthPredictionMSec = FixedUpdateIntervalMSec;

        private PhysicsManager physicsManager; // for use in the fixed update thread
        private PhyManGraphicsData previousPhysicsDataShared;
        private PhyManGraphicsData currentPhysicsDataShared;
        private PhyManGraphicsData previousPhysicsDataUnfixed;
        private PhyManGraphicsData currentPhysicsDataUnfixed;

        double deltaTime;
        float deltaTimeF;

        KeyboardState keyboardStateFixed;
        KeyboardState keyboardStateUnfixed;

        MouseState mouseStateUnfixed;
        Vector2 mousePos;
        Vector2 mouseWorldPos;
        int oldScrollWheelValue = 0;
        bool dragging;
        Vector2 dragStartCameraPosition;
        Vector2 dragStartMousePosition;

        float interpValue;
        bool interpEnabledFixed;
        bool interpEnabledShared;
        bool interpEnabledUnfixed;
        bool newPhysicsData = true;

        private readonly object sharedDataLock = new();

        public bool firstTick = true;
        public bool firstFrame = true;

        public Game1()
        {
            if (instance != null) { throw new InvalidOperationException("Game1 already initialized."); }
            instance = this;

            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 1000,
                PreferredBackBufferHeight = 1000,
                SynchronizeWithVerticalRetrace = true
            };
            _graphics.ApplyChanges();

            Window.AllowAltF4 = true;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnResize;

            fixedUpdateThread = new Thread(FixedUpdateLoop);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            IsFixedTimeStep = false;

            camera = new Camera();
            new Renderer();

            physicsManager = new PhysicsManager();
            physicsManager.AddObject(0, new StaticObject(
                new Vector2[] { new(-75, 150), new(75, 0), new(-75, -150) },
                new Vector2(-200, 0), PhysicsMaterial.Zero,
                new Color(0xA0, 0xA0, 0xBA)));
            physicsManager.AddObject(1, new StaticObject(
                new Vector2[] { new(-250, 0), new(50, 0), new(150, 40), new(200, 150), new(200, -150)},
                new Vector2(140, 0), PhysicsMaterial.Zero,
                new Color(0x50, 0x50, 0x5A)));
            physicsManager.AddObject(2, new StaticObject(
                new Vector2[] { new(-25, -20), new(0, 20), new(25, -20) },
                new Vector2(0, -100), PhysicsMaterial.Zero,
                new Color(255, 160, 100)));
            physicsManager.AddObject(0, new DynamicObject(
                new Vector2[] { new(-25, -20), new(0, 20), new(25, -20) },
                new Vector2(0, 500), PhysicsMaterial.Zero,
                new Color(100, 160, 255)));
            previousPhysicsDataShared = currentPhysicsDataShared = 
                physicsManager.GetPhyManGraphicsData();

            base.Initialize();

            fixedUpdateThread.Start();
        }


        protected override void LoadContent()
        {
            Renderer.instance.LoadContent();
            ellipseEffect = Content.Load<Effect>("shaders/EllipseShader");
        }

        private void OnResize(object sender, EventArgs eventArgs)
        {
            Renderer.instance.UpdateProjectionMatrix();
        }

        private void FixedUpdateLoop()
        {
            stopwatchFixedUpdate = new Stopwatch();
            stopwatchFixedUpdate.Start();

            while (true)
            {
                tickStartTimeFixedMSec = stopwatchFixedUpdate.ElapsedMilliseconds;
                FixedUpdate();
                ++currentTick;

                currentTimeFixedMSec = stopwatchFixedUpdate.ElapsedMilliseconds;
                nextTickTimeFixedMSec = tickStartTimeFixedMSec + FixedUpdateIntervalMSec;
                remainingTimeFixedMSec  = (int)(nextTickTimeFixedMSec - currentTimeFixedMSec);
                if (remainingTimeFixedMSec > 0)
                {
                    if (remainingTimeFixedMSec > 10)
                    { // 10 msec buffer for inaccuracy
                        Thread.Sleep(remainingTimeFixedMSec - 10);
                    }

                    while (stopwatchFixedUpdate.ElapsedMilliseconds < nextTickTimeFixedMSec) { Thread.Sleep(1); }
                }
            }
        }

        private void FixedUpdate()
        {
            interpEnabledFixed = true;

            keyboardStateFixed = Keyboard.GetState();

            const float speed = 1500f;
            const float gravity = -600f;
            Vector2 force;
            force = new Vector2(0f, gravity);

            if (keyboardStateFixed.IsKeyDown(Keys.D)) { force.X += speed; }
            if (keyboardStateFixed.IsKeyDown(Keys.A)) { force.X -= speed; }

            if (keyboardStateFixed.IsKeyDown(Keys.W)) { force.Y += speed; }
            if (keyboardStateFixed.IsKeyDown(Keys.S)) { force.Y -= speed; }

            physicsManager.dynamicObjects[0].Accelerate(force, FixedUpdateIntervalF);

            if (keyboardStateFixed.IsKeyDown(Keys.R)) { 
                physicsManager.dynamicObjects[0].SetVelocity();
                physicsManager.dynamicObjects[0].SetPosition(new Vector2(0, 20));
                interpEnabledFixed = false;
            }

            physicsManager.TickAllObjects(FixedUpdateIntervalF);

            lock (sharedDataLock)
            {
                interpEnabledShared = interpEnabledFixed;
                tickStartTimeSharedMSec = tickStartTimeFixedMSec;
                previousPhysicsDataShared = currentPhysicsDataShared;
                currentPhysicsDataShared = physicsManager.GetPhyManGraphicsData();
                newPhysicsData = true;
            }
            firstTick = false;
        }

        protected override void Update(GameTime gameTime)
        {
            // runs before Draw() - runs twice with no Draw() on startup (uududud...)
            deltaTime = gameTime.ElapsedGameTime.TotalSeconds;
            deltaTimeF = (float)deltaTime;

            keyboardStateUnfixed = Keyboard.GetState();
            if (keyboardStateUnfixed.IsKeyDown(Keys.Escape)) Exit();

            // camera manipulation
            mouseStateUnfixed = Mouse.GetState();
            mousePos = mouseStateUnfixed.Position.ToVector2();
            mouseWorldPos = Vector2.Transform(mousePos, camera.pixelToWorldMatrix);

            if (mouseStateUnfixed.ScrollWheelValue != oldScrollWheelValue)
            {
                if (mouseStateUnfixed.ScrollWheelValue > oldScrollWheelValue)
                { camera.SetZoomOnPoint(camera.cameraZoom + 0.35f, mouseWorldPos); }
                else { camera.SetZoomOnPoint(camera.cameraZoom - 0.35f, mouseWorldPos); }

                if (dragging)
                {
                    dragStartCameraPosition = camera.cameraPosition;
                    dragStartMousePosition = mousePos;
                }

                Renderer.instance.UpdateProjectionMatrix();
            }

            if (mouseStateUnfixed.MiddleButton == ButtonState.Pressed || keyboardStateUnfixed.IsKeyDown(Keys.Space))
            {
                if (dragging)
                {
                    camera.SetPosition(dragStartCameraPosition -
                        (mousePos - dragStartMousePosition) *
                        new Vector2(camera.pixelToWorldMatrix.M11,
                            camera.pixelToWorldMatrix.M22)
                        );

                    Renderer.instance.UpdateProjectionMatrix();
                }
                else // first dragging frame
                {
                    dragStartCameraPosition = camera.cameraPosition;
                    dragStartMousePosition = mousePos;
                    dragging = true;
                }
                //camera.SetPosition(camera.cameraPosition);
            }
            else { dragging = false; }
            if (keyboardStateUnfixed.IsKeyDown(Keys.Up))
            {
                camera.SetPosition(camera.cameraPosition + new Vector2(1f * deltaTimeF, 0f));
                Renderer.instance.UpdateProjectionMatrix();
            }

            // physics interpolating and data transfer
            if (newPhysicsData)
            {
                lastTickStartTimeUnfixedMSec = tickStartTimeUnfixedMSec;
                lock (sharedDataLock)
                {
                    interpEnabledUnfixed = interpEnabledShared;
                    tickStartTimeUnfixedMSec = tickStartTimeSharedMSec;
                    previousPhysicsDataUnfixed = previousPhysicsDataShared;
                    currentPhysicsDataUnfixed = currentPhysicsDataShared;
                    newPhysicsData = false;
                }

                if (currentTick >= 5)
                {
                    tickLengthPredictionMSec = tickLengthPredictionMSec.LerpInt(
                        (int)(tickStartTimeUnfixedMSec - lastTickStartTimeUnfixedMSec),
                        tickLengthPredictionEntropy);
                }
            }

            if (interpEnabledUnfixed || true)
            { interpValue = ((stopwatchFixedUpdate.ElapsedMilliseconds - tickStartTimeUnfixedMSec)/(float)tickLengthPredictionMSec).Saturate(); }
            else { interpValue = 1f; }

            oldScrollWheelValue = mouseStateUnfixed.ScrollWheelValue;
        }

        protected override void Draw(GameTime gameTime)
        {

            GraphicsDevice.Clear(new Color(27, 28, 30));

            Renderer.instance.Begin();

            DrawInterpolatedPhysics();

            Renderer.instance.End();

            firstFrame = false;
        }

        private void DrawInterpolatedPhysics()
        {
            foreach (KeyValuePair<int, PhyObjGraphicsData> keyValuePair in previousPhysicsDataUnfixed.staticGraphicsData)
            {
                keyValuePair.Value.Draw();
            }
            foreach (KeyValuePair<int, PhyObjGraphicsData> keyValuePair in previousPhysicsDataUnfixed.dynamicGraphicsData)
            {
                if (currentPhysicsDataUnfixed.dynamicGraphicsData.ContainsKey(keyValuePair.Key))
                {
                    if (interpEnabledUnfixed)
                    {
                        keyValuePair.Value.Draw(Util.LerpVector2(
                            keyValuePair.Value.position,
                            currentPhysicsDataUnfixed.dynamicGraphicsData[keyValuePair.Key].position,
                            interpValue));
                    }
                    else
                    {
                        keyValuePair.Value.Draw();
                    }
                }
            }
        }
    }
}

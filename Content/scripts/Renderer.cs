using System;
using System.Diagnostics;
using System.Net.Mime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Physics.Content.scripts;

namespace Physics
{
    public sealed class Renderer
    {
        public static Renderer instance { get; private set; }

        private Effect untexturedEffect;

        private VertexPositionColor[] vertices;
        private int[] indices;

        const int MaxVertexCount = 1024;
        const int MaxIndexCount = MaxVertexCount * 3;

        private int shapeCount;
        private int vertexCount;
        private int indexCount;

        private bool isStarted;

        public Renderer()
        {
            if (instance != null) { throw new InvalidOperationException("Renderer already initialized."); }
            instance = this;

            UpdateProjectionMatrix();

            vertices = new VertexPositionColor[MaxVertexCount];
            indices = new int[MaxIndexCount];

            shapeCount = 0;
            vertexCount = 0;
            indexCount = 0;

            isStarted = false;
        }

        public void UpdateProjectionMatrix()
        {
            Game1.camera.UpdateMatricies(
                Game1.instance.GraphicsDevice.Viewport.Width,
                Game1.instance.GraphicsDevice.Viewport.Height);

            if (untexturedEffect != null)
            { untexturedEffect.Parameters["render_matrix"].SetValue(Game1.camera.renderMatrix); }

        }

        public void LoadContent()
        {
            untexturedEffect = Game1.instance.Content.Load<Effect>("shaders/Untextured");
            UpdateProjectionMatrix();
        }

        public void Begin()
        {
            if (isStarted) throw new Exception("batching has already started.");

            isStarted = true;
        }

        public void End()
        {
            Flush();
            isStarted = false;
        }

        public void Flush()
        {
            EnsureStarted();

            if (this.shapeCount == 0) return; // Nothing to draw

            foreach (EffectPass pass in untexturedEffect.CurrentTechnique.Passes)
            {
                pass.Apply();

                Game1.instance.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
                    PrimitiveType.TriangleList,
                    vertices,
                    0, vertexCount,
                    indices,
                    0, indexCount / 3);
            }

            //Game1.Instance.ellipseEffect.Parameters["resolution"].SetValue();
            Game1.instance.ellipseEffect.Techniques[0].Passes[0].Apply();
            Game1.instance.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionTexture>(
                PrimitiveType.TriangleList,
                new VertexPositionTexture[4]
                {
                    new(new Vector3(10, 10, 0), new Vector2(0, 0)),    // Bottom-left
                    new(new Vector3(10, 100, 0), new Vector2(0, 1)),   // Top-left
                    new(new Vector3(100, 10, 0), new Vector2(1, 0)),   // Bottom-right
                    new(new Vector3(100, 100, 0), new Vector2(1, 1))   // Top-right
                },
                0, 4,
                new int[6] { 2, 1, 0, 1, 2, 3 }, 0,
                2
            );


            shapeCount = 0;
            vertexCount = 0;
            indexCount = 0;
        }

        public void EnsureStarted()
        {
            if (!isStarted) throw new Exception("batching was never started.");
        }

        public void EnsureSpace(int shapeVertexCount, int shapeIndexCount)
        {
            if (shapeVertexCount > MaxVertexCount) throw new Exception("Maximum shape vertex count is: " + MaxVertexCount);
            if (shapeIndexCount > MaxIndexCount) throw new Exception("Maximum shape index count is: " + MaxIndexCount);

            if (vertexCount + shapeVertexCount > MaxVertexCount ||
                indexCount + shapeIndexCount > MaxIndexCount)
            {
                Flush();
            }
        }

        public void DrawFilledTriangle(VertexPositionColor a, VertexPositionColor b, VertexPositionColor c)
        {
            EnsureStarted();
            EnsureSpace(3, 3);

            indices[indexCount++] = 0 + vertexCount;
            indices[indexCount++] = 1 + vertexCount;
            indices[indexCount++] = 2 + vertexCount;

            vertices[vertexCount++] = a;
            vertices[vertexCount++] = b;
            vertices[vertexCount++] = c;

            ++shapeCount;
        }
        
        // all roads lead to rome
        public void DrawFilledPolygon(VertexPositionColor[] shapeVerticies, int[] shapeIndices)
        {
            EnsureStarted();
            EnsureSpace(shapeVerticies.Length, shapeIndices.Length);

            foreach (int index in shapeIndices)
            {
                indices[indexCount++] = index + vertexCount;
            }
            foreach (VertexPositionColor vertexPositionColor in shapeVerticies)
            {
                vertices[vertexCount++] = vertexPositionColor;
            }

            ++shapeCount;
        }
        public void DrawFilledPolygon(VertexPositionColor[] shapeVerticies)
        {
            DrawFilledPolygon(shapeVerticies, PolygonUtils.Triangulate(shapeVerticies));
        }
        public void DrawFilledPolygon(Vector2[] shapePositions, Color color)
        {
            DrawFilledPolygon(PolygonUtils.Vec2ToVertexPositionColor(shapePositions, color), PolygonUtils.Triangulate(shapePositions));
        }

        public void DrawOutlinePolygon(VertexPositionColor[] shapeVerticies, float thickness = 2f)
        {
            for (int i = 0; i < shapeVerticies.Length; ++i)
            {
                DrawLine(shapeVerticies[i], shapeVerticies.GetItem(i + 1), thickness);
            }
        }
        public void DrawOutlinePolygon(Vector2[] shapePositions, float thickness = 2f)
        {
            for (int i = 0; i < shapePositions.Length; ++i)
            {
                DrawLine(shapePositions[i], shapePositions.GetItem(i + 1), thickness);
            }
        }
        public void DrawOutlinePolygon(Vector2[] shapePositions, Color color, float thickness = 2f)
        {
            for (int i = 0; i < shapePositions.Length; ++i)
            {
                DrawLine(shapePositions[i], shapePositions.GetItem(i + 1), color, thickness);
            }
        }

        // all roads lead to rome
        public void DrawLine(Vector2 a, Vector2 b, Color colorA, Color colorB, float thickness = 2f)
        {
            EnsureStarted();
            EnsureSpace(4, 6);

            float halfThickness = thickness / 2f;

            Vector2 offset = (b - a).Perpendicular().Normalized() * halfThickness;

            indices[indexCount++] = 3 + vertexCount;
            indices[indexCount++] = 0 + vertexCount;
            indices[indexCount++] = 1 + vertexCount;
            indices[indexCount++] = 3 + vertexCount;
            indices[indexCount++] = 1 + vertexCount;
            indices[indexCount++] = 2 + vertexCount;

            vertices[vertexCount++] = new VertexPositionColor(new Vector3(a - offset, 0), colorA);
            vertices[vertexCount++] = new VertexPositionColor(new Vector3(a + offset, 0), colorA);
            vertices[vertexCount++] = new VertexPositionColor(new Vector3(b + offset, 0), colorB);
            vertices[vertexCount++] = new VertexPositionColor(new Vector3(b - offset, 0), colorB);

            ++shapeCount;
        }
        public void DrawLine(Vector2 a, Vector2 b, Color color, float thickness = 2f)
        {
            DrawLine(a, b, color, color, thickness);
        }
        public void DrawLine(Vector2 a, Vector2 b, float thickness = 2f)
        {
            DrawLine(a, b, Color.White, Color.White, thickness);
        }
        public void DrawLine(VertexPositionColor a, VertexPositionColor b, float thickness = 2f)
        {
            DrawLine(PolygonUtils.VertexPositionColorToVec2(a), 
                PolygonUtils.VertexPositionColorToVec2(b), a.Color, b.Color, thickness);
        }

        // all roads lead to rome
        public void DrawWireframe(VertexPositionColor[] wireframeVerticies, int[] wireframeIndices, float thickness = 2f)
        {
            // truely horribly slow method - please replace with faster option, very temp code
            Vector2[] wireframePositions = PolygonUtils.VertexPositionColorToVec2(wireframeVerticies);

            for (int i = 0; i < wireframeIndices.Length; i += 3)
            {
                DrawLine(wireframePositions[wireframeIndices[i]], wireframePositions[wireframeIndices[i + 1]],
                    wireframeVerticies[wireframeIndices[i]].Color, wireframeVerticies[wireframeIndices[i + 1]].Color,
                    thickness);
                DrawLine(wireframePositions[wireframeIndices[i + 1]], wireframePositions[wireframeIndices[i + 2]],
                    wireframeVerticies[wireframeIndices[i + 1]].Color, wireframeVerticies[wireframeIndices[i + 2]].Color,
                    thickness);
                DrawLine(wireframePositions[wireframeIndices[i + 2]], wireframePositions[wireframeIndices[i]],
                    wireframeVerticies[wireframeIndices[i + 2]].Color, wireframeVerticies[wireframeIndices[i]].Color,
                    thickness);
            }
        }
        public void DrawWireframe(VertexPositionColor[] polygonVerticies, float thickness = 2f)
        {
            DrawWireframe(polygonVerticies, PolygonUtils.Triangulate(polygonVerticies), thickness);
        }
        public void DrawWireframe(Vector2[] wireframeVerticies, int[] wireframeIndices, Color color, float thickness = 2f)
        {
            DrawWireframe(PolygonUtils.Vec2ToVertexPositionColor(wireframeVerticies, color), wireframeIndices, thickness);
        }
        public void DrawWireframe(Vector2[] wireframeVerticies, int[] wireframeIndices, float thickness = 2f)
        {
            DrawWireframe(wireframeVerticies, wireframeIndices, Color.White, thickness);
        }
        public void DrawWireframe(Vector2[] polygonVerticies, Color color, float thickness = 2f)
        {
            DrawWireframe(PolygonUtils.Vec2ToVertexPositionColor(polygonVerticies, color), thickness);
        }
        public void DrawWireframe(Vector2[] polygonVerticies, float thickness = 2f)
        {
            DrawWireframe(PolygonUtils.Vec2ToVertexPositionColor(polygonVerticies), thickness);
        }
    }
}

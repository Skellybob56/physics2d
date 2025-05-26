using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Schema;

namespace Physics
{
    public static class PolygonUtils
    {
        // https://youtu.be/hTJFcHutls8

        public static int[] Triangulate(Vector2[] vertices)
        {
            // TODO: add input checks

            List<int> indexList = new();
            for (int i = 0; i < vertices.Length; ++i)
            {
                indexList.Add(i);
            }

            int totalTriangleCount = vertices.Length - 2;
            int totalTriangleIndexCount = totalTriangleCount * 3;

            int[] indices = new int[totalTriangleIndexCount];
            int currentTriangleIndex = 0;

            while (indexList.Count > 3)
            {
                for (int i = 0; i < indexList.Count; ++i)
                {
                    int idxA = indexList[i];
                    int idxB = indexList.GetItem(i - 1);
                    int idxC = indexList.GetItem(i + 1);

                    Vector2 vecA = vertices[idxA];
                    Vector2 vecB = vertices[idxB];
                    Vector2 vecC = vertices[idxC];

                    // continue if reflex
                    if (Util.Cross(vecB - vecA, vecC - vecA) <= 0f) { continue; }

                    // check if other point in triangle
                    bool isEar = true;

                    for (int j = 0; j < vertices.Length; ++j)
                    {
                        if (j == idxA ||  j == idxB || j == idxC) continue;

                        if (IsPointInTriangle(vertices[j], vecB, vecA, vecC))
                        {
                            isEar = false;
                            break;
                        }
                    }

                    if (isEar)
                    {
                        indices[currentTriangleIndex++] = idxB;
                        indices[currentTriangleIndex++] = idxA;
                        indices[currentTriangleIndex++] = idxC;

                        indexList.RemoveAt(i);
                        break;
                    }
                }
            }

            // add final triangle
            indices[currentTriangleIndex++] = indexList[0];
            indices[currentTriangleIndex++] = indexList[1];
            indices[currentTriangleIndex++] = indexList[2];

            return indices;
        }
        public static int[] Triangulate(VertexPositionColor[] vertices)
        {
            Vector2[] shapePositions = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                shapePositions[i] = new Vector2(vertices[i].Position.X, vertices[i].Position.Y);
            }

            return Triangulate(shapePositions);
        }

        public static Vector2 VertexPositionColorToVec2(this VertexPositionColor vertex)
        {
            return new Vector2(vertex.Position.X, vertex.Position.Y);
        }
        public static Vector2[] VertexPositionColorToVec2(this VertexPositionColor[] vertices)
        {
            Vector2[] positions = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                positions[i] = VertexPositionColorToVec2(vertices[i]);
            }
            return positions;
        }

        public static VertexPositionColor Vec2ToVertexPositionColor(this Vector2 position)
        {
            return new VertexPositionColor(new Vector3(position.X, position.Y, 0), Color.White);
        }
        public static VertexPositionColor Vec2ToVertexPositionColor(this Vector2 position, Color color)
        {
            return new VertexPositionColor(new Vector3(position.X, position.Y, 0), color);
        }
        public static VertexPositionColor[] Vec2ToVertexPositionColor(this Vector2[] positions)
        {
            VertexPositionColor[] vertices = new VertexPositionColor[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                vertices[i] = Vec2ToVertexPositionColor(positions[i]);
            }
            return vertices;
        }
        public static VertexPositionColor[] Vec2ToVertexPositionColor(this Vector2[] positions, Color color)
        {
            VertexPositionColor[] vertices = new VertexPositionColor[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                vertices[i] = Vec2ToVertexPositionColor(positions[i], color);
            }
            return vertices;
        }

        public static bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return Util.Cross(b - a, p - a) <= 0 && Util.Cross(c - b, p - b) <= 0 && Util.Cross(a - c, p - c) <= 0;
        }
    }
}


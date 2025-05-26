using System;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Physics
{
    class PhysicsObject
    {
        public Vector2[] collider { get; private set; } // wrapped clockwise
        public Vector2 position { get; protected set; }
        public PhysicsMaterial physicsMaterial { get; protected set; }
        public Color color;

        public Vector2[] boundingBox { get; protected set; }
        public int[] convexPoints { get; protected set; }
        public Vector2[] convexPointNormals { get; protected set; }
        public Vector2[] normals { get; protected set; }
        public int[] triangleIndices { get; private set; }

        public PhysicsObject()
        {
            Initialize(new Vector2[] { Vector2.UnitY, Vector2.One, Vector2.UnitX, Vector2.Zero }, Vector2.Zero, PhysicsMaterial.Zero, Color.White);
        }
        public PhysicsObject(Vector2[] collider, Vector2 position = default, PhysicsMaterial physicsMaterial = default, Color color = default)
        {
            Initialize(collider, position, physicsMaterial, color);
        }

        private void Initialize(Vector2[] collider, Vector2 position, PhysicsMaterial physicsMaterial, Color color)
        {
            this.collider = collider;
            this.position = position;
            this.physicsMaterial = physicsMaterial;
            this.color = color;

            InitializeBoundingBox();
            InitializeConvexPoints();
            InitializeNormals();
            InitializeConvexPointNormals();
            triangleIndices = PolygonUtils.Triangulate(this.collider);
        }

        protected virtual void InitializeBoundingBox()
        {
            boundingBox = new Vector2[2];
            boundingBox[0] = collider[0];
            boundingBox[1] = collider[0];
            for (int i = 1; i < collider.Length; ++i)
            {
                Vector2 vertex = collider[i];
                if (vertex.X < boundingBox[0].X) { boundingBox[0].X = vertex.X; }
                if (vertex.Y < boundingBox[0].Y) { boundingBox[0].Y = vertex.Y; }
                if (vertex.X > boundingBox[1].X) { boundingBox[1].X = vertex.X; }
                if (vertex.Y > boundingBox[1].Y) { boundingBox[1].Y = vertex.Y; }
            }
        }

        protected virtual void InitializeConvexPoints()
        {
            int[] tempConvexPoints = new int[collider.Length];
            int length = 0;
            for (int i = 0; i < collider.Length; ++i)
            {
                if (Util.Cross(collider[i] - collider.GetItem(i - 1),
                    collider.GetItem(i + 1) - collider[i]) >= 0f) { continue; }
                tempConvexPoints[length] = i;
                length++;
            }

            convexPoints = new int[length];
            Array.Copy(tempConvexPoints, convexPoints, length);
        }

        protected virtual void InitializeNormals()
        {
            normals = new Vector2[collider.Length];
            for (int i = 0; i < collider.Length; ++i)
            {
                normals[i] = Util.Normalized(collider[i] - collider.GetItem(i + 1)) * Matrix2.Clockwise90Rot;
            }
        }

        protected virtual void InitializeConvexPointNormals()
        {
            convexPointNormals = new Vector2[convexPoints.Length];
            for (int i = 0; i < convexPointNormals.Length; ++i)
            {
                int vertexIndex = convexPoints[i];
                convexPointNormals[i] = (normals[vertexIndex] + normals.GetItem(vertexIndex - 1)).Normalized();
            }
        }
    }

    class DynamicObject : PhysicsObject
    {
        public Vector2 velocity { get; protected set; }
        public float mass { get; protected set; }
        public int maxCollisionDepth { get; protected set; }

        public DynamicObject() : base()
        {
            Initialize(Vector2.Zero, 1f, 4);
        }
        public DynamicObject(Vector2[] collider, Vector2 position = default, PhysicsMaterial physicsMaterial = default, Color color = default,
            Vector2 velocity = default, float mass = 1f, int maxCollisionDepth = 4) : base(collider, position, physicsMaterial, color)
        {
            Initialize(velocity, mass, maxCollisionDepth);
        }

        private void Initialize(Vector2 velocity, float mass, int maxCollisionDepth)
        {
            this.velocity = velocity;
            this.mass = mass;
            this.maxCollisionDepth = maxCollisionDepth;
        }

        public void Tick(SortedDictionary<int, StaticObject> staticObjects, SortedDictionary<int, DynamicObject> dynamicObjects, float deltaTime)
        {
            CollideAndSlide(staticObjects, dynamicObjects, velocity * deltaTime, deltaTime, 1);
        }

        private void CollideAndSlide(SortedDictionary<int, StaticObject> staticObjects, SortedDictionary<int, DynamicObject> dynamicObjects, Vector2 displacement, float deltaTime, int depth)
        {
            if (displacement == Vector2.Zero) { return; } // no movement/no time

            Vector2 maxDisplacement = displacement;

            Vector2 normal = Vector2.Zero;
            foreach (StaticObject staticObject in staticObjects.Values)
            {
                if (CollisionCheck(this, staticObject, ref displacement, ref normal))
                {
                    // Debug.WriteLine($"collided at {this.position + displacement} with normal {normal}");
                }
            }

            // TODO: dynamic collisions
            // Debug.WriteLine(displacement);
            position += displacement;

            if (normal == Vector2.Zero) { return; } // no collision

            float remainingTime = (1 - MathF.Max(MathF.Abs(displacement.X), MathF.Abs(displacement.Y)) /
                MathF.Max(MathF.Abs(maxDisplacement.X), MathF.Abs(maxDisplacement.Y))) * deltaTime;

            velocity = velocity.ProjectOnLine(normal); // projecting velocity to the line
            velocity += normal * (1f / 8f); // fix for sticking bug

            if (depth == maxCollisionDepth || remainingTime == 0) { return; }

            CollideAndSlide(staticObjects, dynamicObjects, (velocity * deltaTime) + (normal * (1f / 16f)), remainingTime, depth + 1);
            return;
        }

        static private bool CollisionCheck(DynamicObject dynamicObject, StaticObject staticObject, ref Vector2 displacement, ref Vector2 normal)
        {
            Vector2[] dynamicBoundingBox = dynamicObject.DynamicBoundingBox(displacement);

            if (!Util.DoBoxesOverlap(staticObject.boundingBox, dynamicBoundingBox)) { return false; } // boxes don't overlap

            bool collision = false;

            for (int convexPointIndex = 0; convexPointIndex < dynamicObject.convexPoints.Length; ++convexPointIndex)
            {
                int vertexIndex = dynamicObject.convexPoints[convexPointIndex];
                Vector2 vertex = dynamicObject.collider[vertexIndex];
                Vector2 convexPointNormal = dynamicObject.convexPointNormals[convexPointIndex];
                Vector2 point = vertex + dynamicObject.position;
                for (int i = 0; i < staticObject.collider.Length; ++i)
                {
                    Vector2[] line = new Vector2[2] { staticObject.collider[i] + staticObject.position,
                        staticObject.collider.GetItem(i + 1) + staticObject.position };
                    
                    if (Util.Dot(staticObject.normals[i], displacement) > 0f)
                    { continue; } // backface culling (displacement)

                    if (Util.Dot(staticObject.normals[i], convexPointNormal) >= 0f)
                    { continue; } // backface culling (normals)

                    if (Util.FiniteLineIntersection(point, point + displacement, line[0], line[1], out Vector2 intersection) < 0)
                    { continue; } // no intersection (or collinear)

                    //Debug.WriteLine($"C1 N{staticObject.normals[i]} CPN{convexPointNormal}");

                    displacement = intersection - point;
                    normal = staticObject.normals[i];
                    collision = true;
                }
            }
            // TODO: simplify duplicate code
            for (int convexPointIndex = 0; convexPointIndex < staticObject.convexPoints.Length; ++convexPointIndex)
            {
                int vertexIndex = staticObject.convexPoints[convexPointIndex];
                Vector2 vertex = staticObject.collider[vertexIndex];
                Vector2 convexPointNormal = staticObject.convexPointNormals[convexPointIndex];
                Vector2 point = vertex + staticObject.position;
                for (int i = 0; i < dynamicObject.collider.Length; ++i)
                {
                    Vector2[] line = new Vector2[2] { dynamicObject.collider[i] + dynamicObject.position,
                        dynamicObject.collider.GetItem(i + 1) + dynamicObject.position };
                    
                    if (Util.Dot(dynamicObject.normals[i], -displacement) > 0f)
                    { continue; } // backface culling (displacement)

                    if (Util.Dot(dynamicObject.normals[i], convexPointNormal) >= 0f)
                    { continue; } // backface culling (normals)

                    if (Util.FiniteLineIntersection(point, point - displacement, line[0], line[1], out Vector2 intersection) < 0)
                    { continue; } // no intersection (or collinear)

                    //Debug.WriteLine($"C2 N{staticObject.normals[i]} CPN{convexPointNormal}");

                    displacement = point - intersection;
                    normal = -dynamicObject.normals[i];
                    collision = true;
                }
            }
            if (collision)
            { }
            return collision;
        }

        public void Accelerate(Vector2 force, float deltaTime)
        {
            velocity += force * deltaTime;
        }

        public void AddForce(Vector2 force)
        {
            velocity += force;
        }

        public void SetVelocity(Vector2 velocity = default)
        {
            this.velocity = velocity;
        }

        public void SetPosition(Vector2 position = default)
        {
            this.position = position;
        }

        private Vector2[] DynamicBoundingBox(Vector2 displacement)
        {
            Vector2[] dynamicBox = (Vector2[])boundingBox.Clone();
            if (displacement.X < 0) { dynamicBox[0].X += displacement.X; }
            else { dynamicBox[1].X += displacement.X; }
            if (displacement.Y < 0) { dynamicBox[0].Y += displacement.Y; }
            else { dynamicBox[1].Y += displacement.Y; }
            dynamicBox[0] += position; dynamicBox[1] += position;
            return dynamicBox;
        }
    }

    class StaticObject : PhysicsObject
    {
        public StaticObject() : base() { }
        public StaticObject(Vector2[] collider, Vector2 position = default, PhysicsMaterial physicsMaterial = default,
            Color color = default) : base(collider, position, physicsMaterial, color) { }
        protected override void InitializeBoundingBox()
        {
            base.InitializeBoundingBox();
            boundingBox = new Vector2[] { boundingBox[0] + position, boundingBox[1] + position };
        }
    }

    struct PhysicsMaterial
    {
        public static readonly PhysicsMaterial Zero = new(0f, 0f);

        public float bounciness;
        public float friction; 

        public PhysicsMaterial(float bounciness = 0, float friction = 0)
        {
            this.bounciness = bounciness;
            this.friction = friction;
        }
    }
}

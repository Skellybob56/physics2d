using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Security.AccessControl;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Physics
{
    class PhysicsManager
    {
        public SortedDictionary<int, StaticObject> staticObjects;
        public SortedDictionary<int, DynamicObject> dynamicObjects;

        public int autoIDStartingPointStatic { get; private set; } = -1000;
        public int autoIDStartingPointDynamic { get; private set; } = -1000;

        public PhysicsManager()
        {
            staticObjects = new SortedDictionary<int, StaticObject>();
            dynamicObjects = new SortedDictionary<int, DynamicObject>();
        }

        public PhyManGraphicsData GetPhyManGraphicsData()
        {
            return new PhyManGraphicsData(staticObjects, dynamicObjects);
        }

        public bool AddObject(DynamicObject dynamicObject)
        {
            int ID = autoIDStartingPointDynamic;
            for (; ID < dynamicObjects.Count; ++ID)
            {
                if (!dynamicObjects.ContainsKey(ID)) { break; }
            }
            autoIDStartingPointDynamic = ID + 1;
            return AddObject(ID, dynamicObject);
        }
        public bool AddObject(StaticObject staticObject)
        {
            int ID = autoIDStartingPointStatic;
            for (; ID < staticObjects.Count; ++ID)
            {
                if (!staticObjects.ContainsKey(ID)) { break; }
            }
            autoIDStartingPointStatic = ID + 1;
            return AddObject(ID, staticObject);
        }
        public bool AddObject(int ID, DynamicObject dynamicObject)
        {
            return dynamicObjects.TryAdd(ID, dynamicObject);
        }
        public bool AddObject(int ID, StaticObject staticObject)
        {
            return staticObjects.TryAdd(ID, staticObject);
        }

        public void TickAllObjects(float deltaTime)
        {
            foreach (DynamicObject dynamicObject in dynamicObjects.Values)
            {
                dynamicObject.Tick(staticObjects, dynamicObjects, deltaTime);
            }
        }
    }

    struct PhyManGraphicsData : IEquatable<PhyManGraphicsData>
    {
        public SortedDictionary<int, PhyObjGraphicsData> staticGraphicsData;
        public SortedDictionary<int, PhyObjGraphicsData> dynamicGraphicsData;

        public PhyManGraphicsData(SortedDictionary<int, PhyObjGraphicsData> staticGraphicsData,
            SortedDictionary<int, PhyObjGraphicsData> dynamicGraphicsData)
        {
            this.staticGraphicsData = new(staticGraphicsData);
            this.dynamicGraphicsData = new(dynamicGraphicsData);
        }
        public PhyManGraphicsData(SortedDictionary<int, StaticObject> staticObjects,
            SortedDictionary<int, DynamicObject> dynamicObjects)
        {
            this.staticGraphicsData = new();
            this.dynamicGraphicsData = new();

            foreach (KeyValuePair<int, StaticObject> keyValuePair in staticObjects)
            {
                staticGraphicsData.Add(keyValuePair.Key, new(keyValuePair.Value));
            }
            foreach (KeyValuePair<int, DynamicObject> keyValuePair in dynamicObjects)
            {
                dynamicGraphicsData.Add(keyValuePair.Key, new(keyValuePair.Value));
            }
        }

        public void DrawPhyManGraphicsData()
        {
            foreach (PhyObjGraphicsData phyObjGraphicsData in staticGraphicsData.Values)
            {
                phyObjGraphicsData.Draw();
            }
            foreach (PhyObjGraphicsData phyObjGraphicsData in dynamicGraphicsData.Values)
            {
                phyObjGraphicsData.Draw();
            }
        }
        public void DrawPhyManGraphicsData(Color color)
        {
            foreach (PhyObjGraphicsData phyObjGraphicsData in staticGraphicsData.Values)
            {
                phyObjGraphicsData.Draw(color);
            }
            foreach (PhyObjGraphicsData phyObjGraphicsData in dynamicGraphicsData.Values)
            {
                phyObjGraphicsData.Draw(color);
            }
        }

        public static bool operator ==(PhyManGraphicsData phyManGraphicsDataA, PhyManGraphicsData phyManGraphicsDataB)
        {
            return phyManGraphicsDataA.Equals(phyManGraphicsDataB);
        }
        public static bool operator !=(PhyManGraphicsData phyManGraphicsDataA, PhyManGraphicsData phyManGraphicsDataB)
        {
            return !phyManGraphicsDataA.Equals(phyManGraphicsDataB);
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is PhyManGraphicsData phyManGraphicsData)
            {
                return Equals(phyManGraphicsData);
            }

            return false;
        }
        public readonly bool Equals(PhyManGraphicsData other)
        {
            if (staticGraphicsData == other.staticGraphicsData)
            {
                return dynamicGraphicsData == other.dynamicGraphicsData;
            }
            return false;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(staticGraphicsData.GetHashCode(), dynamicGraphicsData.GetHashCode());
        }
    }

    struct PhyObjGraphicsData : IEquatable<PhyObjGraphicsData>
    {
        public Vector2 position;
        public Vector2[] collider;
        public int[] triangleIndices;
        public Color color;
        public VertexPositionColor[] vertices;

        public PhyObjGraphicsData(PhysicsObject physicsObject)
        {
            position = physicsObject.position;
            collider = (Vector2[])physicsObject.collider.Clone();
            triangleIndices = (int[])physicsObject.triangleIndices.Clone();
            color = physicsObject.color;
            vertices = new VertexPositionColor[collider.Length];
        }

        public void Draw()
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = PolygonUtils.Vec2ToVertexPositionColor(collider[i] + position, color);
            }

            Renderer.instance.DrawFilledPolygon(vertices, triangleIndices);
        }
        public void Draw(Color color)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = PolygonUtils.Vec2ToVertexPositionColor(collider[i] + position, color);
            }

            Renderer.instance.DrawFilledPolygon(vertices, triangleIndices);
        }
        public void Draw(Vector2 position)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = PolygonUtils.Vec2ToVertexPositionColor(collider[i] + position, color);
            }

            Renderer.instance.DrawFilledPolygon(vertices, triangleIndices);
        }
        public void Draw(Vector2 position, Color color)
        {
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = PolygonUtils.Vec2ToVertexPositionColor(collider[i] + position, color);
            }

            Renderer.instance.DrawFilledPolygon(vertices, triangleIndices);
        }

        public static bool operator ==(PhyObjGraphicsData phyObjGraphicsDataA, PhyObjGraphicsData phyObjGraphicsDataB)
        {
            return phyObjGraphicsDataA.Equals(phyObjGraphicsDataB);
        }
        public static bool operator !=(PhyObjGraphicsData phyObjGraphicsDataA, PhyObjGraphicsData phyObjGraphicsDataB)
        {
            return !phyObjGraphicsDataA.Equals(phyObjGraphicsDataB);
        }

        public override readonly bool Equals(object obj)
        {
            if (obj is PhyObjGraphicsData phyObjGraphicsData)
            {
                return Equals(phyObjGraphicsData);
            }

            return false;
        }
        public readonly bool Equals(PhyObjGraphicsData other)
        {
            return (position == other.position) && (collider == other.collider) && 
                (triangleIndices == other.triangleIndices) && (color == other.color);
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(position.GetHashCode(), collider.GetHashCode(), triangleIndices.GetHashCode(), color.GetHashCode());
        }
    }
}

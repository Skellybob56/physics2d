using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Physics.Content.scripts
{
    public class Camera
    {
        // settables
        public Vector2 cameraPosition { get; private set; }
        public float cameraZoom { get; private set; }

        // properties
        public float aspectRatio { get; private set; }
        public Matrix renderMatrix { get; private set; }
        public Matrix worldToPixelMatrix { get; private set; }
        public Matrix pixelToWorldMatrix { get; private set; }

        // internal storage
        private float zoom;
        private float inverseZoom;
        private Vector2 renderScale;
        private Vector2 inverseRenderScale;

        private float scaleLimitingResolution;
        private float pixelsPerUnit;
        private float unitsPerPixel;

        public Camera(Vector2 cameraPosition = default, float cameraZoom = -9.5f)
        {
            this.cameraPosition = cameraPosition;
            this.cameraZoom = cameraZoom;
        }

        public void SetPosition(Vector2 cameraPosition)
        { this.cameraPosition = cameraPosition; }
        public void SetZoom(float cameraZoom)
        { this.cameraZoom = cameraZoom; }
        public void SetZoomOnPoint(float cameraZoom, Vector2 center)
        {
            float zoomDelta = MathF.Pow(2f, cameraZoom - this.cameraZoom);
            this.cameraZoom = cameraZoom;
            this.cameraPosition = cameraPosition.LerpVector2(center, (zoomDelta - 1f) / zoomDelta);
        }

        public void UpdateMatricies(int viewportWidth, int viewportHeight)
        {
            zoom = MathF.Pow(2f, cameraZoom);
            inverseZoom = 1f / zoom;

            aspectRatio = (float)viewportWidth / (float)viewportHeight;
            if (aspectRatio < 1) // maintains a square centre for all aspect ratios
            { renderScale = new Vector2(zoom, zoom * aspectRatio); }
            else { renderScale = new Vector2(zoom / aspectRatio, zoom); }
            inverseRenderScale = new Vector2(1f / renderScale.X, 1f / renderScale.Y);

            scaleLimitingResolution = MathF.Min(viewportWidth, viewportHeight);
            pixelsPerUnit = (zoom/2f) * scaleLimitingResolution; // /2f because unit space is 2x2 wide
            unitsPerPixel = 1f / pixelsPerUnit;

            UpdateRenderMatrix();
            UpdateWorldToPixelMatrix();
            UpdatePixelToWorldMatrix();
        }

        private void UpdateRenderMatrix()
        {
            renderMatrix = new Matrix(
                renderScale.X, 0f, 0f, 0f,
                0f, renderScale.Y, 0f, 0f,
                0f, 0f, 1f, 0f,
                -cameraPosition.X * renderScale.X, -cameraPosition.Y * renderScale.X, 0f, 1f
                );
        }

        private void UpdateWorldToPixelMatrix()
        {
            worldToPixelMatrix = new Matrix(
                pixelsPerUnit, 0f, 0f, 0f,
                0f, -pixelsPerUnit, 0f, 0f, // flip y
                0f, 0f, 1f, 0f,
                (inverseRenderScale.X - cameraPosition.X) * pixelsPerUnit, (inverseRenderScale.Y + cameraPosition.Y) * pixelsPerUnit, 0f, 1f
                );
        }

        private void UpdatePixelToWorldMatrix()
        {
            pixelToWorldMatrix = new Matrix(
                unitsPerPixel, 0f, 0f, 0f,
                0f, -unitsPerPixel, 0f, 0f, // flip y
                0f, 0f, 1f, 0f,
                cameraPosition.X - inverseRenderScale.X, cameraPosition.Y + inverseRenderScale.Y, 0f, 1f
                );
        }
    }
}

'backface culling (displacement)' is overly agressive
 - specifically `Util.Dot(convexPointNormal, displacement) < 0f`

impaling bug where static geo can impale a dynamic object

Working Space:

vec2 worldSpacePos;
out vec2 pixelCoord;

float pixelsPerUnit = zoom * min(resolution.X, resolution.Y); // min defines the scale

// scales world correctly
pixelCoord.X = worldSpacePos.X * +pixelsPerUnit;
pixelCoord.Y = worldSpacePos.Y * -pixelsPerUnit;

// offsets so the centre is zero
pixelCoord.X += cameraPosition.X * pixelsPerUnit;
pixelCoord.Y += cameraPosition.Y * pixelsPerUnit;

// offsets so the top right corner is zero
pixelCoord.X += resolution.X/2f;
pixelCoord.Y += resolution.Y/2f;

######

vec2 pixelCoord;
out vec2 worldSpacePos;

float unitsPerPixel = 1f/pixelsPerUnit;

// scales world correctly
pixelCoord.X = worldSpacePos.X * +unitsPerPixel;
pixelCoord.Y = worldSpacePos.Y * -unitsPerPixel;

// offsets so the bottom right corner is camera pos
pixelCoord.X -= cameraPosition.X;
pixelCoord.Y -= cameraPosition.Y;

// offsets so the centre is camera pos
pixelCoord.X -= inverseRenderScale.X;
pixelCoord.Y += inverseRenderScale.Y;
# Physics 2D
This is a WIP 2D rigidbody simulator written in C# with MonoGame.<br>
It works on the basis that any colision between two arbitrary polygons will be between one point and one line.
If you trace the subtick motion of a point on a polygon using a straight line and check if this motion intersects with any lines from other colliders, you can consistantly detect colisions.<br>
I devised of this method of collision detection in August 2024 and I wanted to see if I could make rigidbody colision in 2D without doing any research on colision detection and response.
I worked on the project in Python (using Pyglet) for five months on and off, before leaving Python due to scale and performance concerns. 
This project was created on January 13th 2025 as I wanted to try MonoGame as a good, low level but cross platform base for C# applications with OpenGL.

# Current features
**Physics**
 - Solid Dynamic-Static colision and response

**Camera**
 - Fully functional panning and zooming
 - Resolution agnostic 

# Future features
**Physics**
 - Bounciness
 - Friction
 - Dynamic-Dynamic Colision
 - Rotation
 - Primative Colliders

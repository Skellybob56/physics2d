# Physics2D
**Physics2D** is a custom 2D rigidbody physics engine written in C# using MonoGame.<br>
It will support collisions with static (unmoving) and dynamic (moving) physics objects.
Physics objects contain a collider of arbitrary convex or concave ngons, with configurable physics properties

## Collision Detection Method
This engine is based on a novel approach I devised in August 2024:
> Any collision between two arbitrary polygons in 2D can be reduced to a point-line intersection.

Each vertex of a moving polygon is traced linearly across a sub-tick timestep.
These trajectories are tested for intersections against the static edges of other polygons.
This enables consistent and robust detection of point-line collisions without relying on SAT or impulse-based resolution.

## Development History
- **Initial concept:** Developed in Python with Pyglet (Aug 2024–Jan 2025) to test feasibility.
- **C# port:** Started in January 2025 using MonoGame for performance, cross-platform rendering, and better scaling potential.

# Current features
**Physics**
 - Convex & concave polygon support
 - Solid Dynamic-Static colision and material led response
  - View matrix system (pan + zoom)
  - Internal ngon triangulation via ear clipping

# Future features
**Physics**
 - Bounciness
 - Friction
 - Dynamic-Dynamic Colision
 - Rotation
 - Primative Colliders

## Purpose
This project is a technology testbed for a future custom 3D engine. It lays the foundation for robust collision detection, physics response, and custom rendering control—all of which will feed into a larger game engine project.

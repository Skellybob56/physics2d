# Physics Plans
## Bugs

## Future
### Optimization
 - Implement a BVH for more efficient collision checks (at least for static geo)

### Features (in decreasing likelihood)
 - Dynamic-Dynamic collision (use relative velocity)
 	- every collision would create a new subtick for all? Dynamic objects as the linear paradigm would be interrupted
 	  	- parallelize dynamic collisions and apply shortest time to collision. remember previous collision plans and only invalidate those with relation to an applied collision
 - Bounciness
 - Friction
 - Rotation (use collision point as rotation point)
 - Circular Colliders

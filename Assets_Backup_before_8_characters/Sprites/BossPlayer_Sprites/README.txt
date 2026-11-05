BossPlayer sprites, 64x64 per frame, PNG, transparent background.
Each animation = separate horizontal strip (frames left to right).
Character faces RIGHT. For left movement flip via transform.localScale.x = -1 or SpriteRenderer.flipX.

Frames: Idle 8, Run 6, Jump 5, Fall 3, Land 4, DaggerThrow 8, Parry 10, Dash 4, Hurt 4, Death 7

Unity import (each PNG):
  Texture Type: Sprite (2D and UI); Sprite Mode: Multiple
  Pixels Per Unit: 64 (or your project value); Filter Mode: Point (no filter); Compression: None
  Sprite Editor -> Slice -> Grid By Cell Size 64x64, Pivot: Bottom (feet on the ground line, same for all frames)
Collider/Rigidbody2D/scripts are not touched; only SpriteRenderer/Animator sprites are swapped.

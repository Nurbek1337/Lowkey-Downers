ELF ROGUE — UNITY SPRITE SHEETS
================================

Files:
- Walk.png          9 frames
- Dash.png          7 frames
- Jump.png          9 frames
- Hurt.png          5 frames
- Death.png         9 frames
- Dagger_Throw.png  8 frames
- Parry.png         7 frames

Every frame is 64x64 pixels. Each PNG is a single horizontal strip with a transparent background.
The sheets were extracted from the generated sprite-sheet artwork and normalized to a shared pixel scale and bottom pivot.
The artwork itself is AI-generated, so inspect the frame transitions before using them in a production build.

Unity import:
1. Select each PNG in the Project window.
2. Set Texture Type = Sprite (2D and UI).
3. Set Sprite Mode = Multiple.
4. Set Filter Mode = Point (no filter).
5. Set Compression = None.
6. Open Sprite Editor -> Slice -> Grid by Cell Size -> 64 x 64 -> Slice -> Apply.
7. Select the sliced sprites and create an Animation clip for each action.

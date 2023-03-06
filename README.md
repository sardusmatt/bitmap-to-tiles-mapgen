A simple command-line utility, which procedurally generates 2D tiled maps (split into background tiles and map tiles), starting from a base|up|down texture set, a background image and a map bitmap (B/W img will do).
Useful to build levels for 2D games with destructible maps (like Worms and the likes).

The utility expects 5 command line arguments
- background image file
- boolean map file
- tile width
- tile height
- texturing style (either 1: classic, 2: inferno or 3: sci-fi)
 While there are no restriction for dimensions, tile width and height should be 64|128|256|512 for best results
 Background image and bitmap should also have same dimensions, which should be multiples of 1024|2048|4096 x 1024|2048|4096
 The tool is expected to run in the parent folder of a 'textures' directory, containing Base, Up and Down textures for each texturing style
 The tool will output the tiled map into an 'output' folder in the current working directory
 Invocation example: .\BitmapToTiles.exe .\testBackground.png .\testBitmap.png 128 128 1
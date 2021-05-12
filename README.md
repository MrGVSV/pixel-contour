[![openupm](https://img.shields.io/npm/v/com.mrgvsv.pixel-contour?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.mrgvsv.pixel-contour/)

![pixel_contour_thumbnail](/Documentation~/thumbnail.png)

# Pixel Contour

This package provides a simple edge-detection system designed specifically for pixel-art. Use it to create complex shapes and colliders based on pixels from a Sprite or Texture2D.

> **Note**: Edge-detection is done using [Moore-Neighbor Tracing](http://www.imageprocessingplace.com/downloads_V3/root_downloads/tutorials/contour_tracing_Abeer_George_Ghuneim/moore.html), and is only good for detecting the outermost edge of a pixel-art image. It will not identify "holes" or additional "islands" in any given texture.



## Installation

### OpenUPM

Install the package via [openupm-cli](https://github.com/openupm/openupm-cli):

```bash
openupm add com.mrgvsv.pixel-contour
```

### Git URL

The package can also be installed via git URL. This can be done one of two ways:

#### Within Unity

Inside Unity, open the Package Manager Window. Click the "+" button in the top left corner, and select "Add package fromgGit URL...". Then paste in the following:

```bash
https://github.com/MrGVSV/pixel-contour.git
```

> For more details, see Unity's [documentation](https://docs.unity3d.com/Manual/upm-ui-giturl.html)

#### Manually

Alternatively, a package can be installed manually by opening the `manifest.json` file found in the "Packages" folder in your root project directory. Add the following:

```jsonc
{
	// ...
  	"dependencies": {
    		// other dependencies...
    		"com.mrgvsv.pixel-contour": "https://github.com/MrGVSV/pixel-contour.git",
  	}
}
```



## Configuration

Before a Sprite can be used, it must first be configured properly. To do this, open up your Sprite in the Inspector. Make sure the option "**Read/Write Enabled**" under "Advanced" is **checked**.

#### For `SpriteContourVisualizer`

The following is not required for the package to function, but is recommended for use with `SpriteContourVisualizer`.

| Name      | Value         | Details                                                      |
| --------- | ------------- | ------------------------------------------------------------ |
| Mesh Type | `Full Rect`   | This is set so that the contour's pixel coordinate system lines up with the Sprite's coordinates. |
| Pivot     | `Bottom Left` | Again, this makes it so that `(0,0)` corresponds to the Sprite's bottom-left corner |



## Usage

### `PixelContourDetector`

This class is the core of the package. It is used to generate a `Contour` object, which contains all the vertex data for the shape.

```c#
var detector = new PixelContourDetector( mySprite );
```

To then find the contour, run:

```c#
detector.FindContour();
```

Note that this does not return the contour itself. To get that, call:

```c#
var contour = detector.GetContour();
```

> To do both at once, you can also simply call the `GetContour()` method. It will find the contour if `FindContour()` has not already been called.


### `Contour`

This class contains the actual contour data.

#### Properties

| Name        | Type                               | Description                                                  |
| ----------- | ---------------------------------- | ------------------------------------------------------------ |
| Vertices    | `ReadOnlyCollection<ContourVertex>` | A collection of all vertices. A `ContourVertex` is a structure containing the position and "pixel-normal" of a vertex, where "pixel-normal" is simply the normal of a vertex rounded to the best-fit pixel (ex. a normal of `( 0.7f, 0.7f )` will become `( 1f, 1f )`, whereas `( 1f, 0f )` will stay the same) |
| Points      | `IEnumerable<Vector2>`              | A collection of all vertex positions                         |
| VertexCount | `int`                                | The number of vertices in the contour                        |
| Bounds      | `Bounds`                             | The bounds of the contour                                    |

#### Methods

##### `public Contour Expanded(float amount)`

Return an expanded (or shrunk) version of this contour.

This is not guaranteed to work for all contours. Expanding or shrinking can cause edges to overlap if they are too close together (ex. in single-pixel lines or small gaps).

##### `public Contour StepExpanded(int steps)`

Return an expanded (or shrunk) version of this contour, using whole-pixel steps.

Stepping is sometimes better because it allows nearby vertices to collapse into a single vertex, preventing some cases of overlapping edges. 

This is not guaranteed to work for all contours. Expanding or shrinking can cause edges to overlap if they are too close together (ex. in single-pixel lines or small gaps).

##### `public Contour Simplified()`

Return a simplified version of this contour.

This only affects collinear vertices. Corners and diagonals will not be simplified.


### `SpriteContourVisualizer`

This is simply a helper component used to show the vertices of a Sprite. Simply attach this MonoBehaviour to a Sprite object in the scene (be sure to [configure](#for-spritecontourvisualizer) your Sprite correctly) and then set the "Pixels Per Unit" field to match the Sprite.

The "Expansion" slider allows the contour to be expand and shrink according to its vertex normals. This is not guaranteed to work for all textures (see [this section](#`public Contour Expanded(float amount)`) for more details).

The script finds the contour during `OnEnable()` in both Play and Edit modes. Swapping out the Sprite or other operations may require the component to be toggled off and back on to reload.

### Considerations

The process for finding a contour is not exceptionally fast. On the [test](/Runtime/Sprites/pixel-contour-test-sprite.png) Sprite, it took about 1.75 milliseconds to find the contour. Therefore, it is recommended to find the contour either when the scene loads in or when performance won't be an issue, then cache the result.

For example:

```c#
private void LoadContours(Sprite[] sprites)
{
	m_Contours = new Dictionary<string, Contour>();
	
	foreach(Sprite sprite in sprites)
	{
		var detector = new PixelContourDetector( sprite );
		m_Contours.Add( sprite.name, detector.GetContour() );
	}
}
```



## License

MIT License

Copyright © 2021 Gino Valente

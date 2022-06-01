# Point Cloud From Hue Encoded Depth Video

Using a Kinect v1 and encoding its depth into a video file as a hue range, it can later be used to playback point-cloud animations. This is just a test project.

[![ezgif com-gif-maker](https://user-images.githubusercontent.com/9950198/171508651-ef7784cd-d833-4b34-abff-d8520e722299.gif)](https://gfycat.com/grimymediocreilsamochadegu)

## Plugins Used
* [Kinect with MS-SDK](https://assetstore.unity.com/packages/tools/kinect-with-ms-sdk-7747)
* [FFmpegOut](https://github.com/keijiro/jp.keijiro.ffmpeg-out)

## Controls
* `F1` Records to a custom file format that stores a sequence of PNGs
* `F2` Records to a video file that will be located in the root project directory
* `Tab` Switch between sources. Kinect, Custom file, or video.

## Playing
Start by opening the scene `Scenes/DepthMapTest`.
There is a sample video included that can be played by pressing `Tab` and switching to the video provider. The object `Player/Video Provider` must be enabled and active in the scene for it to be seen.

## Recording
For recording a video you need a Kinect V1 camera and the installed drivers. 
> Install the Kinect SDK 1.8 or Runtime 1.8.

Recorded Videos will be located in the root project folder.
The Project uses FFmpeg and in the scene object `Player` you can change the preset or add additional parameters. Check FFmpegOut for more information

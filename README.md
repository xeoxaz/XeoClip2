# XeoClip2 – Windows x64
_A high-performance screen recording tool optimized for Windows 64-bit._
## Features
✅ **Windows x64 Compatibility:** Fully optimized for 64-bit FFmpeg builds
✅ **Efficient Screen Capture:** Uses `screen-capture-recorder` for high-quality desktop recording
✅ **System Audio Capture:** Uses `virtual-audio-capturer` to grab system sounds
✅ **GPU-Accelerated Encoding:** Utilizes `h264_nvenc` for fast, efficient processing
✅ **Customizable Bitrates & Buffers:** Tuned settings for reliable streaming performance
✅ **FLV Format for Streaming:** Ensures compatibility with live platforms
✅ **Real-Time Logging & Debugging:** Detailed FFmpeg logs for troubleshooting
## Installation for Windows x64
### Install FFmpeg (64-bit)
Download and extract **64-bit FFmpeg** from [FFmpeg's official site](https://ffmpeg.org/download.html).
Ensure it's accessible in the system PATH:
ffmpeg -version
### Install Screen Capture & Audio Drivers
- **Download** and **install** [Screen Capture Recorder](https://github.com/rdp/screen-capture-recorder-to-video-windows-free).
- **Install VB-Audio Virtual Cable** (if needed for stable system sound capture).

## Icon Matching for Clips
✅ **Automatic Clip Thumbnail Generation:** Uses FFmpeg to extract preview frames
✅ **Customizable Icons for Clip Categories:** Matches clip type to predefined icons
✅ **Metadata Extraction for Identification:** Displays clip details for easy sorting
## Troubleshooting
📌 **Video Not Capturing?** Run:
ffmpeg -list_devices true -f dshow -i dummy
Ensure `"screen-capture-recorder"` appears in the device list.

📌 **Audio Issues?** Verify system audio source by selecting `"virtual-audio-capturer"` in sound settings.

# XeoClip :: Project
You can follow the overall project and it's plans.
[XeoClip Project](https://github.com/users/xeoxaz/projects/2)

## XeoClip :: Repository
See the archived project.
[First Repsitory](https://github.com/xeoxaz/XeoClip)

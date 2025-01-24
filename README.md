# Unity 360 Video Recorder & Uploader

Record, process, and upload 360° videos directly from Unity to YouTube.

## Features
- One-click 360° video recording
- YouTube upload integration
- Spatial metadata injection for 360° video formatting
- Support for both mono and stereo (left-right) 360° videos

## Requirements
- Unity 2022.3 or newer
- Python installation (standard Python or Anaconda)
- Google Cloud project with YouTube API enabled

## Installation

### Using Git
To install this package via Git, follow these steps:

1. Open Unity and go to **Window > Package Manager**.
2. Click the **+** button in the top-left corner and select **Add package from git URL.
3. Paste the following URL and click **Add**: `https://github.com/TheWiselyBearded/360-unity-capture.git`

## Setup
1. Add YouTube API credentials:
   - Place your OAuth 2.0 credentials in `YouTubeUploader.cs`
   - Set redirect URI to `http://localhost:8080/`

2. Add recorder to scene:
   - Tools → Recorder Setup → Add Recorder

3. Record & Upload:
   - Click Record to capture 360° footage
   - Tools → YouTube Uploader
   - Optionally inject spatial metadata
   - Set title, description, privacy
   - Upload to YouTube

## Dependencies
- Google.Apis.YouTube.v3 client library
- Python spatial-media injection tool (included)

## Notes
- First upload requires OAuth authentication via browser
- Processed videos maintain original quality
- YouTube processing may take additional time for 360° playback

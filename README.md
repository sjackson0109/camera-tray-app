# Camera Tray App

The **Camera Tray App** is a Windows application designed for managing and viewing multiple IP camera feeds. The app resides in the system tray, offering convenient access to configure, monitor, and troubleshoot camera streams.

---

## Features

- **System Tray Integration**: Quickly access camera configurations and feeds. Making this application ideal for small businesses and home users seeking a quick, lightweight IP Camera Viewer.
- **Support for Multiple Cameras**: Dynamically load and display camera feeds in a grid.
- **JSON-based Configuration**: Easy to edit and manage camera settings.
- **Command-line Arguments**: Directly launch specific functionalities like camera feeds display. Ensuring fast-access times to getting a live camera view.
- **Error Handling**: Provides user-friendly messages and debug logging. Issues with misconfigurations or missing dependencies should throw a visual error as well.
---

## System Requirements
- Operating System: Microsoft Windows 10/11+
- **.NET Framework**: Install the appropriate .NET Framework version.
- **`ffplay` Utility**: Place `ffplay.exe` in the `lib` directory of the application folder. This is essential for video stream playback.
- **Camera Configuration File**: The app requires a `camera.config` file with proper JSON formatting (explained below).

---

## Installation

1. **Clone or Download** the repository.
2. Place the required dependencies (`ffplay.exe`) in the `lib` directory.
3. Compile the project using Visual Studio or a compatible IDE.
4. Ensure the `camera.config` file exists in the application's directory with valid configurations.

---

## Configuration Guide
This product supports a `camera.config` file in either of the two paths (in order):
1. `%appdata%\Simon Jackson\CameraTrayApp\camera.config`
2. `C:\Program Files\Simon Jackson\CameraTrayApp\camera.config`
> [!TIPs]
> - When opening the app, path 1 will be attempted to be read. Falling back to path 1
> - When saving config; the application will create file 1 (in most circumstances)


### JSON Configuration File: `camera.config`

This file is critical for loading camera settings. It should adhere to the following structure:

```json
{
  "Cameras": [
    {
      "Name": "Camera1",
      "Url": "rtsp://192.168.1.100:554/stream1",
      "User": "admin",
      "Password": "password",
      "X": 100,
      "Y": 200,
      "Width": 640,
      "Height": 480
    },
    {
      "Name": "Camera2",
      "Url": "http://192.168.1.101:8080/video",
      "User": "",
      "Password": "",
      "X": null,
      "Y": null,
      "Width": null,
      "Height": null
    }
  ],
  "debugLevel": "warning"
}

```
> [!WARNING] Use a `viewer-only` account credentials, to avoid exposing administrative/root access to the IP Camera.

### Explanation of Properties

|Property|Type|Description|
|---|---|---|
|Name|String|A unique name to identify the camera.|
|Url|String|The full URL to access the camera stream. Ports and paths must be explicitly defined.|
|User|String|(Optional) Username for authentication.|
|Password|String|(Optional) Password for authentication.|
|X|Integer|(Optional) Horizontal Position of the video window in pixels, relative to the left of your main desktop.|
|Y|Integer|(Optional) Vertical Position of the video window in pixels, relative to the top of your main desktop.|
|Width|Integer|(Optional) Width of the video feed in pixels.|
|Height|Integer|(Optional) Height of the video feed in pixels.|


## Running the application
### Desktop
We can run the application in one of two ways:
 - Under most circumstances, it's expected users will launch this application from a `desktop shortcut`, supplied by the PS1 Installation script.<br>
 -OR-
 - Simply double on the EXE file.
 
Once the application is launched, interacting with the product is done via the windows system tray. Double click opens 'show cameras'. Right click gives a menu to select from.
### Command Line
The application supports a couple of command line arguments:
 - Display the cameras upon launch of the application:
    ```powershell
    CameraTrayApp.exe --show
    ```
 - Display the configuration screen upon launch of the application:
    ```powershell
    CameraTrayApp.exe --config
    ```

## Troubleshooting
Initally Logging levels are set to minimal, but it is possible to enable debug logging, by updating the cameras.config file, adding a parameter 
```json
{
  "Cameras": [
    // leave everything here as it was before
  ],
  "debugLevel": "error"
}
```
`debugLevel` supports `warning`, `error`, `info`, and `trace`.  Logging files are saved under `%appdata%\Simon Jackson\CameraTrayApp\Logs`.

### Stream Not Loading:
- Verify the Url and ensure it includes the correct port and path for the camera feed.
- Check the camera's user manual or online support for the correct RTSP/HTTP paths.
### Error Loading Configuration:
- Ensure the camera.config file exists and is a correctly formatted JSON file - see [JSON Configuration File](###%20JSON%20Configuration%20File) above.


### Request for feedback
It would be nice to expand ont he capabilities of this application; so anyone with a use-case that doesn't work, or maybe something that is needed that hasn't yet been implemented (reading camera config from a remove NVR comes to mind).. please raise a feature request or bug via github.
# Camera Tray App

The **Camera Tray App** is a Windows application that resides in the system tray and provides a quick way to manage and view multiple camera feeds. The app allows users to configure camera feeds and display them in a grid layout.

## Features

- Tray icon for quick access.
- Configuration of multiple camera feeds.
- Dynamic loading of camera configurations from a file.
- Display camera feeds in a grid format.
- Command-line argument support to directly show cameras.
- JSON-based configuration for easy management.
- Graceful error handling and notifications.

## Requirements

- **.NET Framework**: Ensure you have `.Net Framework v8.0` installed - Available to download separately [here](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- **ffplay**: Has been bundled into this downloaded repository, note  `ffplay.exe` is inside the `lib` directory. This product is available 

## Installation

1. Clone or download the repository.
2. Place the necessary dependencies, such as `ffplay.exe`, in the `lib` directory.
3. Compile the project using Visual Studio or another suitable IDE.
4. Ensure the configuration file (`camera.config`) is in the application directory.

## Configuration

### Camera Configuration File

The `camera.config` file is a JSON file that defines the list of cameras to be displayed. Each camera requires the following properties:

```json
{
  "Cameras": [
    {
      "Name": "Camera1",
      "Url": "http://camera-url",
      "User": "username",
      "Password": "password",
      "X": 100,
      "Y": 200,
      "Width": 640,
      "Height": 480
    }
  ]
}

### Explaination of Properties

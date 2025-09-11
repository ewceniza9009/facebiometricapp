# fbapp

`fbapp` is a cross-platform mobile application built with .NET MAUI for facial recognition-based biometric timekeeping. It provides a seamless way to log time-in, time-out, and breaks, with both online and offline capabilities.

-----

## Features

  * **Facial Recognition:** Utilizes facial recognition for accurate and secure time logging. The app supports "Time In", "Break-Out", "Break-In", and "Time Out" functions.
  * **User Registration:** New users can be registered with their biometric ID, name, and a reference face image.
  * **Offline Mode:** Allows for continued operation even without an internet connection. An administrator can enable offline mode via password authentication.
  * **Spoof Detection:** Incorporates a security measure to detect and prevent spoofing attempts, enhancing the integrity of the biometric system.
  * **HRIS API Integration:** Connects with an HRIS (Human Resource Information System) API to synchronize biometric logs.
  * **Local Data Storage:** Employs a local SQLite database to store user data and time logs, ensuring data persistence in offline scenarios.

-----

## Technologies Used

  * **.NET MAUI:** A modern, multi-platform framework for creating native mobile and desktop apps with C\# and XAML.
  * **SkiaSharp:** A 2D graphics library used for image processing, including resizing and rotation.
  * **SQLite:** A lightweight, serverless, self-contained, transactional SQL database engine for local data storage.
  * **CommunityToolkit.Maui:** A collection of reusable components and helpers for .NET MAUI development.
  * **Microsoft.ML.OnnxRuntime:** A high-performance inference engine for ONNX (Open Neural Network Exchange) models, enabling on-device machine learning.

-----

## Getting Started

### Prerequisites

  * .NET 8 SDK
  * Visual Studio 2022 with the .NET Multi-platform App UI development workload installed.

### Installation

1.  **Clone the repository:**
    ```bash
    git clone https://github.com/ewceniza9009/facebiometricapp.git
    ```
2.  **Open the solution:**
    Open the `fbapp.sln` file in Visual Studio.
3.  **Restore NuGet packages:**
    Build the solution to automatically restore all the required NuGet packages.
4.  **Configuration:**
    Configure the application settings, such as the HRIS API URL and camera settings, within the app's setup page.
5.  **Run the application:**
    Select the target platform (Android or Windows) and run the application.

-----

## Usage

1.  **Time Logging:** Use the main screen to select the desired log type ("Time In", "Break Out", etc.) and capture an image for verification.
2.  **Registration:** Navigate to the registration page to enroll new users by providing their details and capturing a facial image.
3.  **Offline Mode:** In case of network unavailability, an administrator can enable offline mode by authenticating with a password. All logs will be stored locally and can be synchronized later.
4.  **Settings:** Access the setup page to configure various application parameters, including camera selection, image rotation, and API endpoints.

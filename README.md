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

## Important Commands

> Note: See the .csproj modification 11/30/2025

1. dotnet workload restore
2. dotnet build -f net8.0-android34.0
3. dotnet run -f net8.0-android34.0

## Wireless Debugging

1. adb pair 192.168.1.15:35441
2. adb connect 192.168.1.15:43210

## Wireless Debugging Walkthrough

Wireless debugging in .NET MAUI allows you to deploy and debug your app on a physical device without a USB cable. This is incredibly useful if you have faulty cables, loose ports, or simply want the freedom to move the device around.

Here is the step-by-step guide for **Android** (the most common use case) and **iOS**.

-----

### 📱 Android Wireless Debugging

There are two methods depending on your Android version. Both require your PC and Phone to be on the **same Wi-Fi network**.

#### Method 1: Android 11+ (The Modern Way)

Android 11 introduced native support for wireless debugging using a pairing code.

1.  **Enable Wireless Debugging on Phone:**

      * Go to **Settings** \> **Developer Options**.
      * Scroll down to the **Debugging** section.
      * Toggle **Wireless debugging** to **ON**.
      * Tap the text **Wireless debugging** to enter the sub-menu.

2.  **Get Pairing Info:**

      * Tap **Pair device with pairing code**.
      * You will see a **Wi-Fi pairing code** (6 digits) and an **IP address & Port** (e.g., `192.168.1.15:35441`). Keep this screen open.

3.  **Pair via PC Command Line:**

      * Open the **Android Adb Command Prompt** in Visual Studio (**Tools** \> **Android** \> **Android Adb Command Prompt**) or a standard PowerShell/CMD window.
      * Type the following command using the IP and Port from the pairing screen:
        ```bash
        adb pair 192.168.1.15:35441
        ```
      * It will ask for the pairing code. Type the 6-digit code shown on your phone.
      * *Result:* `Successfully paired to 192.168.1.15:35441`

4.  **Connect:**

      * Go back to the main **Wireless debugging** menu on your phone.
      * Look at the **IP address and Port** under the main status (This port is different from the pairing port\!).
      * Run this command on PC:
        ```bash
        adb connect 192.168.1.15:43210
        ```
        *(Replace with the actual IP:Port shown on the main screen)*.

5.  **Debug:**

      * Check your connection: `adb devices`.
      * Your device will now appear in the Visual Studio **Debug Target** dropdown menu.

-----

#### Method 2: Android 10 and Below (Legacy ADB TCP/IP)

This method requires a USB cable for the *initial* setup, but then you can unplug it.

1.  **Initial Connection:**

      * Connect your phone to the PC via **USB**.
      * Make sure USB debugging is allowed.

2.  **Open Port 5555:**

      * Open the terminal/command prompt.
      * Run:
        ```bash
        adb tcpip 5555
        ```
      * *Result:* `restarting in TCP mode port: 5555`

3.  **Connect Wirelessly:**

      * Find your phone's IP address (Settings \> About Phone \> Status or tapping on the Wi-Fi network details).
      * **Disconnect the USB cable.**
      * Run:
        ```bash
        adb connect 192.168.1.15:5555
        ```
        *(Replace with your phone's IP)*.

4.  **Debug:**

      * Visual Studio will now see the device over the network.

-----

### 🍎 iOS Wireless Debugging

To debug an iOS device wirelessly from Visual Studio (Windows), you generally need a Mac build host paired to VS, or you must configure the device via a Mac first.

1.  **Setup on Mac (Required once):**

      * Connect your iPhone to your Mac via USB.
      * Open **Xcode**.
      * Go to **Window** \> **Devices and Simulators**.
      * Select your connected iPhone.
      * Check the box **Connect via network**.
      * Wait for a globe icon to appear next to the phone's name.

2.  **Debug in .NET MAUI:**

      * Disconnect the USB cable.
      * Ensure the Mac and iPhone are on the same Wi-Fi.
      * In Visual Studio (Windows), connecting to the Mac ("Pair to Mac") will now detect the iPhone as a remote wireless device.
      * Select "Remote Device" or the iPhone's name in the debug dropdown.

### ⚠️ Important Troubleshooting Tips

  * **Same Network:** The \#1 reason this fails is that the PC is on Ethernet (LAN) and the phone is on Wi-Fi, and the router isolates them. Ensure they are on the same subnet.
  * **Firewall:** If `adb connect` fails, ensure Windows Firewall isn't blocking `adb.exe`.
  * **Port Changes:** On Android 11+, the Port number changes **every time** you toggle Wireless Debugging off and on. You must check the screen for the new port before connecting.
  * **Keep Phone Awake:** During the initial pairing, keep the phone screen on and the "Pairing" dialog open, or the code may change.

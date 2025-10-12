# Capstone
# capstonephase2

# Project Dependencies Setup

This guide walks through everything you need to install before opening the project in Unity and building for Android or iOS.

## 1. Prerequisites
- Unity Hub (latest stable release)
- Git (for cloning the repository)
- macOS 13+ or Windows 10+
- Optional: Node.js LTS (only required if you plan to use `Assets/server.js` for local testing)

## 2. Install Unity Editor & Modules
1. Open Unity Hub and install **Unity 2022.3.61f1** (LTS) — this is the editor version recorded in `ProjectSettings/ProjectVersion.txt`.
2. While installing, add the following build support modules:
   - Android Build Support (includes Android SDK & NDK, OpenJDK)
   - iOS Build Support (macOS only)
   - Mac Build Support (if you intend to produce macOS desktop builds)
3. Launch Unity Hub once after installation so the Android licensing components finish configuring.

## 3. Allow Unity to Restore Packages
When the project is opened for the first time, Unity Package Manager reads `Packages/manifest.json` and installs all required packages automatically. The key dependencies you should see in the Package Manager UI include:

- `com.unity.render-pipelines.universal` (URP)
- `com.unity.xr.arfoundation`, `com.unity.xr.arcore`, `com.unity.xr.arkit`
- `com.unity.xr.interaction.toolkit`
- `com.unity.inputsystem`
- `com.unity.sentis`
- `com.unity.textmeshpro`
- `com.google.external-dependency-manager` (Google EDM4U)
- `jillejr.newtonsoft.json-for-unity`

> If Unity shows “Download” or “Resolve” buttons for any packages, allow it to complete before attempting to open scenes.

## 4. Mapbox SDK & Access Token
The repository contains the Mapbox Unity SDK under `Assets/Mapbox`. Unity will import these assets automatically.

Before entering Play Mode, configure a Mapbox access token:
1. Create or log into a Mapbox account at https://account.mapbox.com.
2. Copy a default or newly created **Public** token.
3. In Unity, go to `Mapbox > Configure` (or `Mapbox > Setup`) and paste the token into the Access Token field.
4. Save the configuration when prompted. A missing or invalid token will block map loading.

## 5. Firebase Unity SDK
Firebase packages are already included under `Assets/Firebase`. Ensure the following native configuration files exist (they should be version-controlled for your production project):

- `Assets/google-services.json` (Android)
- `Assets/GoogleService-Info.plist` (iOS)

If you are setting up your own Firebase project:
1. Create a new Firebase project in the Firebase Console.
2. Add Android and/or iOS apps, then download the configuration files above and drop them into `Assets/`.
3. (Optional) Update the hard-coded `AppOptions` in `Assets/DBLoader.cs` with your own API key, App ID, and Database URL.
4. In Unity, run `Assets > External Dependency Manager > Android Resolver > Force Resolve` after changing Firebase settings to ensure Android libraries are up to date.

## 6. Install CocoaPods (iOS only)
Unity’s Firebase and AR packages rely on CocoaPods when exporting an iOS/Xcode project. Verify Pods are installed on your machine:

1. Check if CocoaPods is available:
   ```bash
   pod --version
   ```
2. If the command is missing, install via RubyGems (requires Xcode command-line tools):
   ```bash
   sudo gem install cocoapods
   ```
3. After installation, run `pod setup` once to prime the local specs repo. This can take several minutes the first time.
4. When you build the project for iOS, Unity will generate an Xcode workspace. Open it and run `pod install` in the generated `Pods` directory if Unity does not do so automatically.

## 7. Platform SDK Notes
- **Android**: Make sure Android SDK/NDK and OpenJDK ship with the Unity module. If Unity cannot locate them, point Unity Hub to the installed SDK locations.
- **iOS**: Xcode 15+ is required for building and deploying to iOS.
- **AR Features**: Device testing requires ARKit-compatible iOS devices or ARCore-compatible Android devices.

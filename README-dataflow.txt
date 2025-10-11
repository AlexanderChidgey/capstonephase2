Data Flow & Usage Guide
========================

Overview
--------
This project loads substation metadata from Firebase (with local caching), renders those records on a Mapbox map, and allows the user to navigate to detailed information screens. The following scripts coordinate that behaviour:

- `DBLoader.cs` (Assets/DBLoader.cs)
  - Ensures a Firebase app exists, loads cached JSON (or fetches from Firebase), and exposes the list of `Substation` records.
  - Saves the records to `Application.persistentDataPath/DistSubstation.json` for offline use.
  - Triggers `geoConverter.Convert(substations)` if a converter component is assigned.
  - Raises the `OnSubstationsLoaded` event once data is available.

- `InteractiveMap` (Assets/Mapbox/Examples/6_ZoomableMap/Scripts/SpawnOnMap.cs)
  - Waits for both the Mapbox map to initialize and `DBLoader` to report that data is ready.
  - Instantiates a marker prefab for each substation and positions it on the map.
  - When a marker is clicked/tapped, stores the selected `SYSTEM_ID` in `StoreSelectedScan` and loads `MapObjectInfoScene`.

- `MapOverlayController` (Assets/UI/UI Scripts/MapOverlayController.cs)
  - Singleton (`DontDestroyOnLoad`) that clones `ScanDataOverlay.uxml` and keeps the nav buttons wired across scene transitions.
  - Provides a `Show(Substation)` helper used by other systems when an overlay is required.

- `UIController.cs` / `UIControllerMapOverlay.cs` (Assets/UI/UI Scripts)
  - Manage the UI Toolkit overlays in different scenes (main vs map).
  - Wire copy buttons, export actions, and nav buttons.
  - Listen for `DBLoader.OnSubstationsLoaded` to populate labels.
  - Use `StoreSelectedScan` to recall the substation that should be displayed when returning from another scene.

- `HistoryLogController` & `HistoryStoreScan` (Assets/UI/UI Scripts)
  - Collect user scan history and allow navigation back into detail scenes.
  - `HistoryStoreScan` is a simple static cache for the selected record and overlay state.

Setup Instructions
------------------
1. **Firebase Credentials**: Ensure `google-services.json` (Android) and `GoogleService-Info.plist` (iOS) are present in `Assets`. `DBLoader` uses hard-coded `AppOptions`—replace with your project values if different.

2. **Place DBLoader**: Add `DBLoader` to a bootstrap scene (or a `DontDestroyOnLoad` object). Optionally assign a `convertToGeo` component if you need geo conversion.

3. **Map Scene**:
   - Ensure `InteractiveMap` references the shared `DBLoader`.
   - Provide a marker prefab with a collider so raycasts can detect clicks.
   - Confirm `StoreSelectedScan` is used (via `SpawnOnMap.cs`) to pass the selected ID to other scenes.

4. **Overlay Scenes** (`UIController`, `UIControllerMapOverlay`):
   - Attach the controllers to the relevant `UIDocument` objects.
   - Fill in the serialized UI element names so `root.Q<>()` can locate each label/button.
   - Assign `DBLoader` (or let the script find it with `FindObjectOfType`).

5. **History Scene**:
   - Use `HistoryLogController` to populate the history list and call `StoreSelectedScan` when navigating to detail scenes.

Runtime Flow
------------
1. `DBLoader` starts, checks Firebase dependencies, loads cached JSON or downloads from Firebase, then stores the records.
2. `InteractiveMap.WaitForOverlayAndSpawn()` blocks until both the map and `DBLoader` are ready, then spawns markers.
3. When the player taps a marker, `StoreSelectedScan.Id` is set and the info scene loads (`MapObjectInfoScene`).
4. UI controllers read `StoreSelectedScan.Id`, query `DBLoader` for the matching `Substation`, and populate the labels.
5. Navigation buttons (via `MapOverlayController` or `UIControllerMap`) simply call `SceneManager.LoadScene` for the target scene.

Debugging Tips
--------------
- Use `DBLoader` context menu commands (e.g., “Dump Local JSON (raw)”) to inspect cached data.
- Call `DBLoader.PrintSubstationsSample()` to verify the data fields you expect are present.
- Watch the Unity Console for `[DBLoader]` logs to trace the load path (local vs Firebase).

By keeping `DBLoader` as the single source of truth, every scene can safely request substation data without re-fetching, and all navigation flows share the same cached dataset.

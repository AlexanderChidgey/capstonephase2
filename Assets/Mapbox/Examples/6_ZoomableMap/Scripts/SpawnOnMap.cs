using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using System.Collections.Generic;
using System.Collections;

public class InteractiveMap : MonoBehaviour
{
    [SerializeField] private AbstractMap _map;
    [SerializeField] private GameObject _markerPrefab;
    [SerializeField] private float _spawnScale = 5f;
    [SerializeField] private DBLoader _dbLoader;

    private List<GameObject> _spawnedMarkers = new();
    private List<Vector2d> _markerLocations = new();
    private float _visibleRadiusKm = 0.5f;

    void Start()
    {
        StartCoroutine(SpawnMarkersWhenReady());
    }

    private IEnumerator SpawnMarkersWhenReady()
    {
        // Wait until data is loaded
        while (!_dbLoader.IsLoaded)
            yield return null;

        var substations = _dbLoader.GetSubstations();
        foreach (var sub in substations)
        {
            Vector2d latLon = new Vector2d(sub.LAT, sub.LON);
            _markerLocations.Add(latLon);

			var marker = Instantiate(_markerPrefab, _map.transform);
            marker.transform.localScale = Vector3.one * _spawnScale;
            var dataComponent = marker.AddComponent<MarkerData>();
            dataComponent.Substation = sub;

            _spawnedMarkers.Add(marker);
        }

        UpdateMarkerPositions();
    }

    void Update()
    {
        UpdateMarkerPositions();
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                var markerData = hit.collider.gameObject.GetComponent<MarkerData>();
                if (markerData != null)
                    ShowMarkerInfo(markerData);
            }
        }
    }

    private void UpdateMarkerPositions()
    {
        Vector2d center = _map.CenterLatitudeLongitude;

        for (int i = 0; i < _spawnedMarkers.Count; i++)
        {
            double distanceKm = DistanceMatching.Haversine(
                (float)center.x, (float)center.y,
                (float)_markerLocations[i].x, (float)_markerLocations[i].y);

            bool visible = distanceKm <= _visibleRadiusKm;
            _spawnedMarkers[i].SetActive(visible);

			if (visible)
			{
				Vector3 pos = _map.GeoToWorldPosition(_markerLocations[i], true);
				pos.y = 0f; // flatten to map surface
				_spawnedMarkers[i].transform.localPosition = pos;

				// _spawnedMarkers[i].transform.localPosition = _map.GeoToWorldPosition(_markerLocations[i], true);
			}
        }
    }

    private void ShowMarkerInfo(MarkerData data)
    {
        Debug.Log($"Clicked on substation: {data.Substation}");
    }
}

public class MarkerData : MonoBehaviour
{
    public Substation Substation;
}


// // Before "Improvements"
// namespace Mapbox.Examples
// {
// 	using UnityEngine;
// 	using Mapbox.Utils;
// 	using Mapbox.Unity.Map;
// 	using Mapbox.Unity.MeshGeneration.Factories;
// 	using Mapbox.Unity.Utilities;
// 	using System.Collections.Generic;
// 	using System.Collections;

// 	public class SpawnOnMap : MonoBehaviour
// 	{
// 		[SerializeField]
// 		AbstractMap _map;

// 		[SerializeField]
// 		[Geocode]
// 		string[] _locationStrings;

// 		[SerializeField]
// 		float _spawnScale = 2f;

// 		[SerializeField]
// 		GameObject _markerPrefab;
// 		[SerializeField]
// 		private DBLoader _dbLoader;
// 		List<GameObject> _spawnedObjects;
// 		private List<Vector2d> _locations;

// 		private DistanceMatching distanceMatching;


// 		void Start()
// 		{
// 			_locations = new List<Vector2d>();
// 			_spawnedObjects = new List<GameObject>();
// 			Input.location.Start();
// 			Vector2d currentLocation = new Vector2d(Input.location.lastData.latitude, Input.location.lastData.longitude);
// 			_map.SetCenterLatitudeLongitude(currentLocation);
// 			_map.UpdateMap();

// 			StartCoroutine(SpawnAfterDataLoaded());
// 		}

// 		private IEnumerator SpawnAfterDataLoaded()
// {
// 	while (!_dbLoader.IsLoaded)
// 	{
// 		yield return null;
// 	}

// 	var _substations = _dbLoader.GetSubstations();
// 	Debug.Log($"Got {_substations.Count} substations from DB");

// 	double userLat = Input.location.lastData.latitude;
// 	double userLon = Input.location.lastData.longitude;

// 	// --- Step 1: Build list with distances ---
// 	var nearbySubs = new List<(Substation sub, double dist)>();
// 	foreach (var sub in _substations)
// 	{
// 		double dist = DistanceMatching.Haversine(
// 			(float)userLat, (float)userLon,
// 			(float)sub.LAT, (float)sub.LON
// 		);

// 		// Only consider subs within ~10km, then we’ll cut to 50 closest
// 		if (dist <= 10)
// 		{
// 			nearbySubs.Add((sub, dist));
// 		}
// 	}

// 	// --- Step 2: Sort by distance ---
// 	nearbySubs.Sort((a, b) => a.dist.CompareTo(b.dist));

// 	// --- Step 3: Take the top 50 ---
// 	int limit = Mathf.Min(50, nearbySubs.Count);
// 	for (int i = 0; i < limit; i++)
// 	{
// 		var sub = nearbySubs[i].sub;
// 		Vector2d latLon = new Vector2d(sub.LAT, sub.LON);
// 		_locations.Add(latLon);

// 		var instance = Instantiate(_markerPrefab, _map.transform);
// 		instance.transform.localPosition = _map.GeoToWorldPosition(latLon, false);
// 		instance.transform.localScale = Vector3.one * _spawnScale;
// 		_spawnedObjects.Add(instance);
// 	}

// 	Debug.Log($"Spawned {limit} closest substations");
// }

// 		private void Update()
// 		{
// 			float latitude = Input.location.lastData.latitude;
// 			float longitude = Input.location.lastData.longitude;

// 			// int count = Mathf.Min(_spawnedObjects.Count, 10);
// 			Debug.Log($"Spawning Objects {_spawnedObjects.Count}");

// 			for (int i = 0; i < _spawnedObjects.Count; i++)
// 			{
// 				Debug.Log($"Added point{i} of {_spawnedObjects.Count}");
// 				_spawnedObjects[i].transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
// 				_spawnedObjects[i].transform.localScale = Vector3.one * _spawnScale;
// 			}
// 		}
// 	}
// }



// using UnityEngine;
// using Mapbox.Utils;
// using Mapbox.Unity.Map;
// using System.Collections;
// using System.Collections.Generic;

// public class SpawnOnMap : MonoBehaviour
// {
//     [SerializeField] private AbstractMap _map;
//     [SerializeField] private GameObject _markerPrefab;
//     [SerializeField] private float _spawnScale = 50f;
//     [SerializeField] private DBLoader _dbLoader;
// 	[SerializeField] private DistanceMatching distanceMatching;

//     private List<GameObject> _spawnedObjects = new();
//     private bool _markersSpawned = false;

// 	IEnumerator Start()
// {
//     // Start location services
//     Input.location.Start();
//     Input.compass.enabled = true;

//     int maxWait = 20;
//     while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
//     {
//         yield return new WaitForSeconds(1);
//         maxWait--;
//     }

//     if (Input.location.status != LocationServiceStatus.Running)
//     {
//         Debug.LogError("Unable to start location services.");
//         yield break;
//     }

//     // Set map center to current location
//     Vector2d currentLocation = new Vector2d(Input.location.lastData.latitude, Input.location.lastData.longitude);
//     _map.SetCenterLatitudeLongitude(currentLocation);
//     _map.UpdateMap();

//     // Wait until substation data is loaded
//     while (!_dbLoader.IsLoaded)
//     {
//         yield return null;
//     }

//     // Wait until the Mapbox map signals initialization
//     bool mapReady = false;
//     _map.OnInitialized += () => { mapReady = true; };
//     yield return new WaitUntil(() => mapReady);

//     // Now safe to spawn markers
//     List<Substation> substations = _dbLoader.GetSubstations();
//     double maxDistance = 10.0;
//     List<Substation> nearbySubs = substations.FindAll(sub =>
//         distanceMatching.Haversine(
//             (float)Input.location.lastData.latitude,
//             (float)Input.location.lastData.longitude,
//             (float)sub.LAT,
//             (float)sub.LON
//         ) <= maxDistance
//     );

//     SpawnSubstationMarkers(nearbySubs);
// }


//     private void SpawnSubstationMarkers(List<Substation> substations)
//     {
//         if (_markersSpawned) return; // avoid spawning multiple times
//         _markersSpawned = true;

//         foreach (var sub in substations)
//         {
//             Vector2d latLon = new Vector2d(sub.LAT, sub.LON);
//             var marker = Instantiate(_markerPrefab, _map.transform);
//             marker.transform.localPosition = _map.GeoToWorldPosition(latLon, true);
//             marker.transform.localScale = Vector3.one * _spawnScale;
//             _spawnedObjects.Add(marker);
//         }

//         Debug.Log($"Spawned {_spawnedObjects.Count} substation markers.");
//     }

//     // Optional: only update marker positions if map moves/zooms
//     void Update()
//     {
//         if (!_markersSpawned) return;

//         for (int i = 0; i < _spawnedObjects.Count; i++)
//         {
//             var sub = _dbLoader.GetSubstations()[i];
//             Vector2d latLon = new Vector2d(sub.LAT, sub.LON);
//             _spawnedObjects[i].transform.localPosition = _map.GeoToWorldPosition(latLon, true);
//         }
//     }
// }

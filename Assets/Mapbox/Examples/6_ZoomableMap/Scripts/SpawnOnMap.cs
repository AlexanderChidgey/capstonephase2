using UnityEngine;
using UnityEngine.UIElements;
using Mapbox.Unity.Map;
using Mapbox.Utils;
using System.Collections;
using System.Collections.Generic;

public class InteractiveMap : MonoBehaviour
{
    [Header("Map / Data")]
    [SerializeField] private AbstractMap _map;
    [SerializeField] private GameObject _markerPrefab;
    [SerializeField] private float _spawnScale = 5f;
    [SerializeField] private DBLoader _dbLoader;

    private readonly List<GameObject> _spawnedMarkers = new();
    private readonly List<Vector2d> _markerLocations = new();
    private MarkerData _lastClickedMarker;
    private float _visibleRadiusKm = 0.5f;

    void OnEnable()
    {
        if (_map != null) _map.OnInitialized += OnMapReady;
    }
    void OnDisable()
    {
        if (_map != null) _map.OnInitialized -= OnMapReady;
    }

    void Start()
    {
        if (_map != null && _map.Root.childCount > 0)
            OnMapReady();
    }

    private void OnMapReady()
    {
        StartCoroutine(WaitForOverlayAndSpawn());
    }

    private IEnumerator WaitForOverlayAndSpawn()
    {
        // Wait for controller singleton
        if (MapOverlayController.Instance == null)
    {
        var go = new GameObject("OverlayControllerRuntime");
        go.AddComponent<MapOverlayController>(); // this will load the UXML at runtime
    }


        // Wait for data
        while (_dbLoader != null && !_dbLoader.IsLoaded)
            yield return null;

        // Spawn markers
        var substations = _dbLoader.GetSubstations();
        foreach (var sub in substations)
        {
            var latLon = new Vector2d(sub.LAT, sub.LON);
            _markerLocations.Add(latLon);

            var marker = Instantiate(_markerPrefab, _map.transform);
            marker.transform.localScale = Vector3.one * _spawnScale;

            var data = marker.AddComponent<MarkerData>();
            data.Substation = sub;
            data.Renderer = marker.GetComponentInChildren<Renderer>();
            if (data.Renderer != null)
            {
                data.OriginalColor = data.Renderer.material.color;
                data.Renderer.material = new Material(data.Renderer.material);
            }

            _spawnedMarkers.Add(marker);
        }

        UpdateMarkerPositions();
    }

    void Update()
    {
        UpdateMarkerPositions();

        var overlay = MapOverlayController.Instance;
        if (overlay != null && overlay.IsOpen)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var markerData = hit.collider.GetComponentInParent<MarkerData>();
                if (markerData != null) ShowMarkerInfo(markerData);
            }
        }
    }

    private void UpdateMarkerPositions()
    {
        if (_map == null) return;
        var center = _map.CenterLatitudeLongitude;
        for (int i = 0; i < _spawnedMarkers.Count; i++)
        {
            double distanceKm = DistanceMatching.Haversine(
                (float)center.x, (float)center.y,
                (float)_markerLocations[i].x, (float)_markerLocations[i].y);

            bool visible = distanceKm <= _visibleRadiusKm;
            _spawnedMarkers[i].SetActive(visible);
            if (visible)
            {
                var pos = _map.GeoToWorldPosition(_markerLocations[i], true);
                _spawnedMarkers[i].transform.localPosition = pos;
            }
        }
    }

    private void ShowMarkerInfo(MarkerData data)
    {
        var c = MapOverlayController.Instance;
        if (c == null)
        {
            Debug.LogWarning("[SpawnOnMap] No MapOverlayController in scene.");
            return;
        }

        // un-highlight previous
        if (_lastClickedMarker?.Renderer != null)
            _lastClickedMarker.Renderer.material.color = _lastClickedMarker.OriginalColor;

        // highlight current
        if (data.Renderer != null) data.Renderer.material.color = Color.red;
        _lastClickedMarker = data;

        var s = data.Substation;
        string info =
            $"System ID: {s.SYSTEM_ID}\n" +
            $"User Ref: {s.USER_REF_I}\n" +
            $"Site: {s.SITE_DESC}\n" +
            $"Type: {s.TR_TYPE}, KVA: {s.MAX_KVA}, Volt: {s.MAX_VOLT}\n" +
            $"Location: ({s.LAT}, {s.LON})\n" +
            $"Updated: {s.REFRESH_DT}";

        c.Show(s);
    }
}

public class MarkerData : MonoBehaviour
{
    public Substation Substation;
    public Renderer Renderer;
    public Color OriginalColor;
}

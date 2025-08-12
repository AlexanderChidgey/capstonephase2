using System.Collections.Generic;
using UnityEngine;
using Mapbox.Unity.Map;
using Mapbox.Utils;


public class POIPlacementCustom : MonoBehaviour
{
    public AbstractMap map;

    // Prefab to spawn
    public GameObject prefab;

    // Your custom list of coordinates
    public List<Vector2d> coordinates = new List<Vector2d>()
    {
        new Vector2d(37.784179, -122.401583),  // Example 1
        new Vector2d(37.785000, -122.400000),  // Example 2
        new Vector2d(37.783500, -122.402000)   // Example 3
    };

}
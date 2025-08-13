using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

public class DistanceMatching : MonoBehaviour
{
    [Header("Search Parameters")]
    public float searchRadiusKm = 2f;
    [Tooltip("Weight between 0 and 1 to balance distance vs angle in sorting (0 = distance only, 1 = angle only)")]
    [Range(0, 1)]
    public float angleWeight = 0.5f;

    [Header("References")]
    [SerializeField]
    private DBLoader dbLoader;
    public bool debugMode = true;

    private const float EARTH_RADIUS = 6371.0f;
    private List<MatchCandidate> cachedResults;
    private bool hasFirstDetection = false;

    [System.Serializable]
    public class MatchCandidate
    {
        public double lat;
        public double lon;
        public string systemId;
        public float distance;
        public float bearing;
        public float angleDiff;
        public string trType;
        public string siteDesc;
        public string maxKVA;

        // Calculate a score based on both distance and angle
        public float GetScore(float maxDistance, float angleWeight)
        {
            // Normalize distance and angle to 0-1 range
            float normalizedDistance = distance / maxDistance;
            float normalizedAngle = angleDiff / 180f; // angles are 0-180

            // Weighted combination (lower is better)
            return (normalizedDistance * (1 - angleWeight)) + (normalizedAngle * angleWeight);
        }

        public override string ToString()
        {
            string location = string.IsNullOrEmpty(siteDesc) ? "" : $" ({siteDesc})";
            return string.Format("[{0}] {1}{2}, {3}KVA, dist={4:F3} km, bearing={5:F1}°, angle_diff={6:F1}°",
                systemId, trType, location, maxKVA, distance, bearing, angleDiff);
        }
    }

    void Awake()
    {
        // Get reference to DBLoader if not set
        if (dbLoader == null)
        {
            dbLoader = GetComponent<DBLoader>();
            if (dbLoader == null)
            {
                dbLoader = gameObject.AddComponent<DBLoader>();
                if (debugMode) Debug.Log("Added DBLoader component");
            }
        }
    }

    public string FindNearbySubstations(float latitude, float longitude, float heading, string objectType)
    {
        // Return cached results if we have them
        if (hasFirstDetection && cachedResults != null)
        {
            if (debugMode) Debug.Log("Using results from first detection");
            return FormatResults(cachedResults, true);
        }

        // First detection - calculate everything
        List<MatchCandidate> candidates = new List<MatchCandidate>();

        var substations = dbLoader.GetSubstations();
        Debug.Log(substations.Count);

        foreach (var substation in substations)
        {
            if (substation == null) continue;


            if (substation.TR_TYPE != objectType) continue;
            // Debug.Log($"HELP! objectType: {objectType}, TR_TYPE: {(string)substation.TR_TYPE}");


            try
            {
                float distance = Haversine(latitude, longitude, (float)substation.LAT, (float)substation.LON);

                if (distance <= searchRadiusKm)
                {
                    float bearing = BearingBetweenPoints(latitude, longitude, (float)substation.LAT, (float)substation.LON);
                    float angleDiff = AngleDifference(heading, bearing);
                    Debug.Log($"Substation: {substation.SYSTEM_ID}, Distance: {distance}, Bearing: {bearing}, Angle Diff: {angleDiff}");

                    candidates.Add(new MatchCandidate
                    {
                        lat = substation.LAT,
                        lon = substation.LON,
                        systemId = substation.SYSTEM_ID,
                        distance = distance,
                        bearing = bearing,
                        angleDiff = angleDiff,
                        trType = substation.TR_TYPE,
                        siteDesc = substation.SITE_DESC,
                        maxKVA = substation.MAX_KVA
                    });
                }
            }
            catch (Exception e)
            {
                if (debugMode) Debug.LogWarning($"Error processing substation: {e.Message}");
            }
        }

        // Sort by combined score of distance and angle
        candidates.Sort((a, b) => {
            float scoreA = a.GetScore(searchRadiusKm, angleWeight);
            float scoreB = b.GetScore(searchRadiusKm, angleWeight);
            return scoreA.CompareTo(scoreB);
        });

        // Store results and mark first detection as complete
        cachedResults = candidates;
        hasFirstDetection = true;
        
        if (debugMode) Debug.Log("First detection completed and cached");
        
        return FormatResults(candidates, false);
    }

    private string FormatResults(List<MatchCandidate> candidates, bool fromCache)
    {
        string results = "";
        StringBuilder sb = new StringBuilder();
        if (fromCache)
        {
            sb.AppendLine("(Using First Detection Results)");
            results += "(Using First Detection Results)";
        }
        sb.AppendLine($"\nFound {candidates.Count} substations within {searchRadiusKm:F3} km:");
        results += $"\nFound {candidates.Count} substations within {searchRadiusKm:F3} km:";
        
        int count = Mathf.Min(candidates.Count, 3);
        for (int i = 0; i < count; i++)
        {
            var c = candidates[i];
            string location = string.IsNullOrEmpty(c.siteDesc) ? "" : $" at {c.siteDesc}";
            float score = c.GetScore(searchRadiusKm, angleWeight);
            sb.AppendLine($"Match {i + 1}: {c.trType}{location}\n" +
                         $"  {c.maxKVA}KVA, ID:{c.systemId}\n" +
                         $"  {c.distance:F3}km away, {c.bearing:F1}° bearing (diff: {c.angleDiff:F1}°)\n" +
                         $"  Score: {score:F3} (lower is better)");
            results += $"Match {i + 1}: {c.trType}{location}\n";
        }
        Debug.Log("Results from DistanceMatching: " + sb.ToString());
        return sb.ToString();
    }

    private float Haversine(float lat1, float lon1, float lat2, float lon2)
    {
        float lat1Rad = lat1 * Mathf.Deg2Rad;
        float lon1Rad = lon1 * Mathf.Deg2Rad;
        float lat2Rad = lat2 * Mathf.Deg2Rad;
        float lon2Rad = lon2 * Mathf.Deg2Rad;

        float dlat = lat2Rad - lat1Rad;
        float dlon = lon2Rad - lon1Rad;

        float a = Mathf.Pow(Mathf.Sin(dlat / 2), 2) + 
                  Mathf.Cos(lat1Rad) * Mathf.Cos(lat2Rad) * 
                  Mathf.Pow(Mathf.Sin(dlon / 2), 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
        
        return EARTH_RADIUS * c;
    }

    private float BearingBetweenPoints(float lat1, float lon1, float lat2, float lon2)
    {
        float v1 = lat1 * Mathf.Deg2Rad;
        float v2 = lat2 * Mathf.Deg2Rad;
        float dpi = (lon2 - lon1) * Mathf.Deg2Rad;

        float y = Mathf.Sin(dpi) * Mathf.Cos(v2);
        float x = Mathf.Cos(v1) * Mathf.Sin(v2) - 
                  Mathf.Sin(v1) * Mathf.Cos(v2) * Mathf.Cos(dpi);
        float bearing = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
        
        return (bearing + 360) % 360;
    }

    private float AngleDifference(float a, float b)
    {
        float diff = Mathf.Abs(a - b) % 360;
        return Mathf.Min(diff, 360 - diff);
    }
} 
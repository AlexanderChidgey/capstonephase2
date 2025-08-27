using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[System.Serializable]
// public class Substation1
// {
//     public string SYSTEM_ID;
//     public string USER_REF_I;
//     public string SITE_DESC;
//     public string TR_TYPE;
//     public string MAX_KVA;
//     public string MAX_VOLT;
//     public double LON;
//     public double LAT;
//     public string REFRESH_DT;
// }

public class convertToGeo : MonoBehaviour
{
    [Header("File Settings")]
    public string outputFileName = "substations.geojson";

    // Public method to convert a list of substations to GeoJSON
    public void Convert(List<Substation> substations)
    {
        if (substations == null || substations.Count == 0)
        {
            Debug.LogWarning("No substations to convert.");
            return;
        }

        JObject geoJson = new JObject(
            new JProperty("type", "FeatureCollection"),
            new JProperty("features", new JArray(
                substations.ConvertAll(sub =>
                    new JObject(
                        new JProperty("type", "Feature"),
                        new JProperty("geometry", new JObject(
                            new JProperty("type", "Point"),
                            new JProperty("coordinates", new JArray(sub.LON, sub.LAT))
                        )),
                        new JProperty("properties", new JObject(
                            new JProperty("SYSTEM_ID", sub.SYSTEM_ID),
                            new JProperty("USER_REF_I", sub.USER_REF_I),
                            new JProperty("SITE_DESC", sub.SITE_DESC),
                            new JProperty("TR_TYPE", sub.TR_TYPE),
                            new JProperty("MAX_KVA", sub.MAX_KVA),
                            new JProperty("MAX_VOLT", sub.MAX_VOLT),
                            new JProperty("REFRESH_DT", sub.REFRESH_DT)
                        ))
                    )
                )
            ))
        );

        string outputPath = Path.Combine(Application.streamingAssetsPath, outputFileName);
        File.WriteAllText(outputPath, geoJson.ToString());
        Debug.Log($"GeoJSON saved to {outputPath} with {substations.Count} features.");
    }
}

namespace Mapbox.Examples
{
    using UnityEngine;
    using Mapbox.Unity.MeshGeneration.Interfaces;
    using System.Collections.Generic;

    public class PoiMarkerHelper : MonoBehaviour, IFeaturePropertySettable
    {
        Dictionary<string, object> _props;

        public void Set(Dictionary<string, object> props)
        {
            _props = props;
        }

        void OnMouseUpAsButton()
        {
            if (_props == null)
            {
                return;
            }

            // Build info string
            string infoText = "";
            foreach (var prop in _props)
            {
                infoText += $"{prop.Key}: {prop.Value}\n";
            }

            // Show in overlay if available
            var overlay = FindObjectOfType<UIControllerMapOverlay>();
            if (overlay != null)
            {
                overlay.ShowOverlay(infoText);
            }
            else
            {
                Debug.Log(infoText);
            }
        }
    }
}

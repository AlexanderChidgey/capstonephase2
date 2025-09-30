using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct HistoryRecord
{
    public string Id;
    public string ObjectType;
    public string Model;
    public string Voltage;
    public string SerialNumber;
    public DateTime Utc;
}

public static class HistoryStoreScan
{
    private const int Max = 50;
    private static readonly List<HistoryRecord> items = new();

    public static void Add(string id)
        => Add(id, objectType: null, serialNumber: null, model: null, voltage: null);

    public static void Add(string id, string objectType, string serialNumber, string model, string voltage)
    {
        if (string.IsNullOrEmpty(id)) return;
        items.Add(new HistoryRecord
        {
            Id = id,
            ObjectType = string.IsNullOrEmpty(objectType) ? "Object" : objectType,
            SerialNumber = serialNumber,
            Model = model,
            Voltage = voltage,
            Utc = DateTime.UtcNow
        });
        if (items.Count > Max) items.RemoveAt(0);
        Debug.Log($"[History] add {id}");
    }

    public static IReadOnlyList<HistoryRecord> All => items;
    public static void Clear() => items.Clear();
}

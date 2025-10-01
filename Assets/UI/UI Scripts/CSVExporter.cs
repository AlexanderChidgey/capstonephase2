using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class CsvExporter
{
    private const string Header =
        "Id,ObjectType,SerialNumber,Model,Voltage,Utc," +
        "SYSTEM_ID,USER_REF_I,SITE_DESC,TR_TYPE,MAX_KVA,MAX_VOLT,LON,LAT,REFRESH_DT," +
        "SERIAL_NUMBER,MODEL_NUMBER,NUMBER_OF_PHASES,LAST_SERVICE_DATE,NEXT_SERVICE_DATE,ADDRESS";

    /// Export a CSV of all scans. Use like this: string path = CsvExporter.ExportAllCsv(HistoryStoreScan.All, db);
    public static string ExportAllCsv(IEnumerable<HistoryRecord> items, DBLoader db, string filePrefix = "scan_history")
    {
        List<HistoryRecord> list = items?.ToList() ?? new List<HistoryRecord>();
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var r in list)
            sb.AppendLine(ToCsvLine(in r, db));

        string filename = $"{filePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return SaveCsv(filename, sb.ToString());
    }

    /// Export a CSV for a single scan. Use it like this: string path = CsvExporter.ExportOneCsv(r, db);
    public static string ExportOneCsv(in HistoryRecord r, DBLoader db, string filePrefix = "scan")
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(Header);
        sb.AppendLine(ToCsvLine(in r, db));

        string safeId = string.IsNullOrEmpty(r.Id) ? "item" : r.Id;
        string filename = $"{filePrefix}_{Sanitize(safeId)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return SaveCsv(filename, sb.ToString());
    }

    private static string ToCsvLine(in HistoryRecord r, DBLoader db)
    {
        if (IsDefault(r)) return "";

        Substation sub = LookupSubstation(db, r.Id);

        string objectType = FirstNonEmpty(sub?.TR_TYPE, r.ObjectType);
        string serialNumber = FirstNonEmpty(sub?.SERIAL_NUMBER, r.SerialNumber);
        string model = FirstNonEmpty(sub?.MODEL_NUMBER, r.Model);
        string voltage = FirstNonEmpty(sub?.MAX_VOLT, r.Voltage);

        string utcIso = r.Utc == default
            ? ""
            : r.Utc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);

        string lon = sub != null ? sub.LON.ToString("G", CultureInfo.InvariantCulture) : "";
        string lat = sub != null ? sub.LAT.ToString("G", CultureInfo.InvariantCulture) : "";

        return string.Join(",",
            Csv(r.Id),
            Csv(objectType),
            Csv(serialNumber),
            Csv(model),
            Csv(voltage),
            Csv(utcIso),

            Csv(sub?.SYSTEM_ID),
            Csv(sub?.USER_REF_I),
            Csv(sub?.SITE_DESC),
            Csv(sub?.TR_TYPE),
            Csv(sub?.MAX_KVA),
            Csv(sub?.MAX_VOLT),
            Csv(lon),
            Csv(lat),
            Csv(sub?.REFRESH_DT),

            Csv(sub?.SERIAL_NUMBER),
            Csv(sub?.MODEL_NUMBER),
            Csv(sub?.NUMBER_OF_PHASES),
            Csv(sub?.LAST_SERVICE_DATE),
            Csv(sub?.NEXT_SERVICE_DATE),
            Csv(sub?.ADDRESS)
        );
    }

    private static Substation LookupSubstation(DBLoader db, string id)
    {
        if (db == null || string.IsNullOrEmpty(id)) return null;
        List<Substation> list = db.GetSubstations();
        if (list == null) return null;

        return list.FirstOrDefault(s => string.Equals(s.SYSTEM_ID, id, StringComparison.OrdinalIgnoreCase))
            ?? list.FirstOrDefault(s => string.Equals(s.USER_REF_I, id, StringComparison.OrdinalIgnoreCase));
    }

    private static string FirstNonEmpty(string a, string b) => string.IsNullOrEmpty(a) ? (b ?? "") : a;

    private static string Csv(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        bool needQuotes = s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
        s = s.Replace("\"", "\"\"");
        return needQuotes ? $"\"{s}\"" : s;
    }

    private static string Sanitize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "item";
        foreach (char c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }

    private static string SaveCsv(string filename, string content)
    {
        string dir = Application.persistentDataPath; // I THINK THIS IS IN Documents FOR IPHONES. I don't have an iphone to check :(
        string path = Path.Combine(dir, filename);
        try
        {
            File.WriteAllText(path, content, Encoding.UTF8);
            Debug.Log($"[HistoryCSV] Saved: {path}");
            return path;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HistoryCSV] Failed to save CSV: {ex.Message}");
            return path;
        }
    }

    private static bool IsDefault(HistoryRecord r) =>
        EqualityComparer<HistoryRecord>.Default.Equals(r, default);
}

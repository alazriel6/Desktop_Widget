using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public class WidgetSettings
{
    public double ClockX { get; set; } = 20;
    public double ClockY { get; set; } = 20;
    public double WeatherX { get; set; } = 400;
    public double WeatherY { get; set; } = 50;
    public double VisualizerX { get; set; } = 0;
    public double VisualizerY { get; set; } = 250;

    // sizes
    public double ClockWidth { get; set; } = Double.NaN;
    public double ClockHeight { get; set; } = Double.NaN;
    public double WeatherWidth { get; set; } = Double.NaN;
    public double WeatherHeight { get; set; } = Double.NaN;
    public double VisualizerWidth { get; set; } = 350;
    public double VisualizerHeight { get; set; } = 100;

    // z-index
    public int ClockZ { get; set; } = 0;
    public int WeatherZ { get; set; } = 0;
    public int VisualizerZ { get; set; } = 0;

    // transforms
    public double ClockRotate { get; set; } = 0;
    public double ClockSkewX { get; set; } = 0;
    public double ClockSkewY { get; set; } = 0;
    // (no scale/perspective fields)

    public double WeatherRotate { get; set; } = 0;
    public double WeatherSkewX { get; set; } = 0;
    public double WeatherSkewY { get; set; } = 0;

    public double VisualizerRotate { get; set; } = -10;
    public double VisualizerSkewX { get; set; } = 20;
    public double VisualizerSkewY { get; set; } = 0;

    // colors (store color name string)
    public string ClockColor { get; set; } = "White";
    public string WeatherTempColor { get; set; } = "White";
    public string WeatherLocationColor { get; set; } = "White";
    public string WeatherSummaryColor { get; set; } = "White";
    public string WeatherDetailsColor { get; set; } = "White";
    public string VisualizerColor { get; set; } = "White";

    // store custom colors for the ColorDialog (WinForms uses int[] for CustomColors)
    public int[] ColorDialogCustomColors { get; set; } = new int[0];


    // file ops
    private static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DesktopWidget", "widgetsettings.json");

    public static WidgetSettings Load()
{
    try
    {
        var p = Path;
        if (!File.Exists(p)) return new WidgetSettings();

        var json = File.ReadAllText(p);
        var opts = new JsonSerializerOptions
        {
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        return JsonSerializer.Deserialize<WidgetSettings>(json, opts) ?? new WidgetSettings();
    }
    catch (Exception)
    {
        // optionally log the error somewhere useful
        return new WidgetSettings();
    }
}

public void Save()
{
    try
    {
        var dir = System.IO.Path.GetDirectoryName(Path)!;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        var opts = new JsonSerializerOptions
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        var json = JsonSerializer.Serialize(this, opts);
        File.WriteAllText(Path, json);
    }
    catch (Exception)
    {
        // don't silently swallow in debug — consider logging to file/console
        // For release you may keep this silent but at least don't let serialization quietly fail.
    }
}

    public void RestoreDefaults()
    {
        var def = new WidgetSettings();
        // copy defaults
        this.ClockX = def.ClockX;
        this.ClockY = def.ClockY;
        this.WeatherX = def.WeatherX;
        this.WeatherY = def.WeatherY;
        this.VisualizerX = def.VisualizerX;
        this.VisualizerY = def.VisualizerY;

        this.VisualizerWidth = def.VisualizerWidth;
        this.VisualizerHeight = def.VisualizerHeight;

        this.ClockZ = def.ClockZ;
        this.WeatherZ = def.WeatherZ;
        this.VisualizerZ = def.VisualizerZ;

        this.ClockRotate = def.ClockRotate;
        this.ClockSkewX = def.ClockSkewX;
        this.ClockSkewY = def.ClockSkewY;
    // no scale fields to reset
        this.WeatherRotate = def.WeatherRotate;
        this.WeatherSkewX = def.WeatherSkewX;
        this.WeatherSkewY = def.WeatherSkewY;
        this.VisualizerRotate = def.VisualizerRotate;
        this.VisualizerSkewX = def.VisualizerSkewX;
        this.VisualizerSkewY = def.VisualizerSkewY;

        this.ClockColor = def.ClockColor;
        this.WeatherTempColor = def.WeatherTempColor;
        this.VisualizerColor = def.VisualizerColor;
        this.ColorDialogCustomColors = def.ColorDialogCustomColors;
    }
}

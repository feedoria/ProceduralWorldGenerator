using System;
using System.Text;
using UnityEngine;

public class RunContext
{
    public CustomTerrain terrain;
    public TerrainConfig config;
    public string outDir;
    public StringBuilder log = new StringBuilder();

    public TerrainData Data => terrain.terrainData;

    public void Log(string msg)
    {
        log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {msg}");
        Debug.Log("[Gen] " + msg);
    }
}
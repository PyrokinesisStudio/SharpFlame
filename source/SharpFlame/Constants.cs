internal static class Constants
{
    public const string ProgramName = "SharpFlame";
    public const string ProgramVersionNumber = "0.24";
#if Mono
    public const string ProgramPlatform = "Mono 0.24";
#else
    public const string ProgramPlatform = "Windows";
#endif
    public const int PlayerCountMax = 10;
    public const int GameTypeCount = 3;
    public const int DefaultHeightMultiplier = 2;
    public const int MinimapDelay = 100;
    public const int SectorTileSize = 8;
    public const int MaxDroidWeapons = 3;
    public const int WzMapMaxSize = 250;
    public const int MapMaxSize = 512;
    public const int MinimapMaxSize = 512;

    public const int IniRotationMax = 65536;
    public const int TileTypeNumWater = 7;
    public const int TileTypeNumCliff = 8;
    public const int TerrainGridSpacing = 128;
}
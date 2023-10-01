namespace AssetIO
{
    /// <summary>
    /// Specifies the types of Free Realms assets.
    /// </summary>
    [Flags]
    public enum AssetType
    {
        /// <summary>
        /// Assets for the game, typically located in the "Free Realms/" directory.
        /// </summary>
        Game = 1,
        /// <summary>
        /// Image assets for the TCG, typically located in the "Free Realms/assets/" directory.
        /// </summary>
        Tcg = 2,
        /// <summary>
        /// Resource assets for the TCG, typically located in the "Free Realms/tcg/" directory.
        /// </summary>
        Resource = 4,
        /// <summary>
        /// Asset .dat files, which come with a manifest.dat file consisting of asset information.
        /// </summary>
        Dat = 8,
        /// <summary>
        /// Asset .pack files, which consist of both asset information and content.
        /// </summary>
        Pack = 16
    }
}

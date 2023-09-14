namespace FreeRealmsUnpacker
{
    /// <summary>
    /// Specifies the types of Free Realms assets.
    /// </summary>
    public enum AssetType
    {
        /// <summary>
        /// Assets for the game, typically located in the "Free Realms/" directory.
        /// </summary>
        Game,
        /// <summary>
        /// Image assets for the TCG, typically located in the "Free Realms/assets/" directory.
        /// </summary>
        Tcg,
        /// <summary>
        /// Resource assets for the TCG, typically located in the "Free Realms/tcg/" directory.
        /// </summary>
        Resource
    }
}

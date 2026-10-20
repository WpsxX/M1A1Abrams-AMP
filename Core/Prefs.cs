using MelonLoader;

namespace M1A1Abrams
{
    /// <summary>
    /// Preference entries are created from several Config() methods. On a hot reload the previous
    /// entries are still registered and CreateEntry throws, which aborts the rest of the setup, so
    /// every call goes through here and reuses an existing entry instead.
    /// </summary>
    internal static class Prefs
    {
        internal static MelonPreferences_Entry<T> Entry<T>(MelonPreferences_Category cfg, string identifier, T default_value)
        {
            MelonPreferences_Entry<T> existing = cfg.GetEntry<T>(identifier);
            if (existing != null) return existing;
            return cfg.CreateEntry<T>(identifier, default_value);
        }
    }
}

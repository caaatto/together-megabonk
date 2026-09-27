using BepInEx.Configuration;

namespace MegabonkTogether.Configuration
{
    public static class ModConfig
    {
        private static ConfigFile configFile;

        // DEV_URL is "ws://127.0.0.1:5000"

        public static ConfigEntry<string> PlayerName { get; private set; }
        public static ConfigEntry<bool> CheckForUpdates { get; private set; }
        public static ConfigEntry<string> ServerUrl { get; private set; }
        public static ConfigEntry<uint> RDVServerPort { get; private set; }
        public static ConfigEntry<bool> ShowChangelog { get; private set; }
        public static ConfigEntry<string> PreviousVersion { get; private set; }
        public static ConfigEntry<bool> AllowSavesDuringNetplay { get; private set; }
        public static ConfigEntry<bool> EnabledSharedExperience { get; private set; }
        public static ConfigEntry<float> EncounterFailsafeTimeoutSeconds { get; private set; }
        public static ConfigEntry<bool> SynchronizeTimers { get; private set; }

        public static void Initialize(ConfigFile config)
        {
            configFile = config;

            PlayerName = config.Bind(
                "Player",
                "PlayerName",
                "Player",
                "Your display name shown to other players. Please be respectful!"
            );
            CheckForUpdates = config.Bind(
                "Updates",
                "CheckForUpdates",
                true,
                "Check for updates on startup . Recommend leaving this enabled"
            );
            ServerUrl = config.Bind(
                "Network",
                "ServerUrl",
                "wss://megabonk-together-matchmaking.balatro-vs-matchmaking.eu",
                "The URL of the matchmaking server. Do not change this unless you know what you're doing (e.g. for self-hosting). Use ws://127.0.0.1:5000 on localhost for testing purpose"
            );
            RDVServerPort = config.Bind(
                "Network",
                "RDVServerPort",
                (uint)5678,
                "The port of the relay server. Do not change this unless you know what you're doing"
            );
            ShowChangelog = config.Bind(
                "Updates",
                "ShowChangelog",
                false,
                "Internal flag to show changelog after an update. Do not modify manually."
            );
            PreviousVersion = config.Bind(
                "Updates",
                "PreviousVersion",
                "",
                "Internal flag to store the previous version before an update. Do not modify manually."
            );
            AllowSavesDuringNetplay = config.Bind(
                "Gameplay",
                "AllowSavesDuringNetplay",
                false,
                "Allow game saves during netplay sessions."
            );
            EnabledSharedExperience = config.Bind(
                "Gameplay",
                "EnabledSharedExperience",
                false,
                "Enable Host experience (Same XP and pause enabled). Disable for no pause and separate XP."
            );
            EncounterFailsafeTimeoutSeconds = config.Bind(
                "Gameplay",
                "EncounterFailsafeTimeoutSeconds",
                60f,
                "Shared experience only. If an encounter (level up, chest, shrine, ...) stays open longer than this many seconds, the host closes it for everyone so a lost packet cannot soft lock the run. Set to 0 to disable the failsafe."
            );
            SynchronizeTimers = config.Bind(
                "Gameplay",
                "SynchronizeTimers",
                true,
                "Let the host keep every player on the same run, stage, swarm, difficulty and crypt clock. Without this the clocks drift apart, because a shared experience pause lasts a different amount of time for each player."
            );
        }

        public static void Save()
        {
            configFile?.Save();
        }
    }
}

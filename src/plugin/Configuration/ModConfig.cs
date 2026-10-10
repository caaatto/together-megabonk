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
        public static ConfigEntry<float> LobbyReadyTimeoutSeconds { get; private set; }
        public static ConfigEntry<float> ShrineChargeSpeedPerExtraPlayer { get; private set; }
        public static ConfigEntry<float> RewardInputGraceSeconds { get; private set; }

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
            LobbyReadyTimeoutSeconds = config.Bind(
                "Network",
                "LobbyReadyTimeoutSeconds",
                45f,
                "How long to wait on the \"Waiting for other players\" screen when loading a map before starting anyway. Without a limit a dropped ready message leaves the game paused on that screen with no way out. Set to 0 to wait forever."
            );
            ShrineChargeSpeedPerExtraPlayer = config.Bind(
                "Gameplay",
                "ShrineChargeSpeedPerExtraPlayer",
                0.5f,
                "How much faster a charge shrine gets for each additional player standing in it. The required time is divided by (1 + extraPlayers * thisValue), so at 0.5 two players charge 1.5x as fast and four players 2.5x. Set to 0 to keep the single player speed regardless of how many stand in it."
            );
            RewardInputGraceSeconds = config.Bind(
                "Gameplay",
                "RewardInputGraceSeconds",
                0.4f,
                "How long button presses are ignored right after a reward, chest or level up window opens, so a key you were already holding down in the fight does not instantly pick something for you. Movement is not affected, it runs on axes rather than buttons. Set to 0 to disable."
            );
        }

        public static void Save()
        {
            configFile?.Save();
        }
    }
}

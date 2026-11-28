using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Constants
{
    public const string PLAYER_NAME_PREF_KEY = "PlayerName";
    public const string TOKEN_PREF_KEY = "Token";
    public const string NICK_NAME_PREF_KEY = "NickName";
    public const string GAME_MODE_PROP_KEY = "gm";
    public const string GAME_MODE_PREF_KEY = "GameMode";
    // the playlist id a match is about to play, from the menus to the game scene
    public const string PLAYLIST_PREF_KEY = "Playlist";
    public const string CUSTOMS_IS_SINGLEPLAYER_PREF_KEY = "CustomsIsSingleplayer";
    public const string CUSTOMS_IS_ADVANCED_COMBAT_PREF_KEY = "CustomsIsAdvancedCombat";
    public const string CUSTOMS_IS_SALVO_PREF_KEY = "CustomsIsSalvo";
    public const string CUSTOMS_IS_BONUS_PREF_KEY = "CustomsIsBonus";
    public const string CUSTOMS_IS_UNLIMITED_PREF_KEY = "CustomsIsUnlimited";
    public const string CUSTOMS_TURN_TIME_LIMIT_KEY = "CustomsTurnTimeLimit";
    public const string CUSTOMS_ALLOW_CHAT_KEY = "CustomsAllowChat";
    public const string CUSTOMS_NAME_KEY = "CustomsName";
    public const string DO_TUTORIAL_PREF_KEY = "DoTutorial";
    public const string BOT_DIFFICULTY_PREF_KEY = "BotDifficulty";
    public const string GAMES_PLAYED_PREF_KEY = "GamesPlayed";
    public const string MAIN_VOLUME_PREF_KEY = "MainVolume";
    public const string MUSIC_VOLUME_PREF_KEY = "MusicVolume";
    public const string INPUT_BINDINGS_PREF_KEY = "InputBindings";
    public const string PLAYER_COLOR_PREF_KEY = "PlayerColor";
    public const string ENEMY_COLOR_PREF_KEY = "EnemyColor";
    public const string REDUCED_MOTION_PREF_KEY = "ReducedMotion";
    public const string FULL_RESOLUTION_PREF_KEY = "FullResolution";
    public const string HIGH_DETAIL_PREF_KEY = "HighDetail";
    public const string ANTI_ALIASING_PREF_KEY = "AntiAliasing";
    public const string VSYNC_PREF_KEY = "VSync";
    public const string CAPTAIN_PREF_KEY = "Captain";
    public const string CAPTAIN_STATE_PREF_PREFIX = "CaptainState_";
    public const string MISSION_STATE_PREF_PREFIX = "MissionState_";
    // the mission a bot match is about to play, from the campaign to the game scene
    public const string CAMPAIGN_MISSION_PREF_KEY = "CampaignMission";
    // stats, nothing records these yet; the progression page shows a dash for any that are missing
    public const string STAT_CAMPAIGN_PROGRESS = "StatCampaignProgress";
    public const string STAT_SINGLEPLAYER_GAMES = "StatSingleplayerGames";
    public const string STAT_MULTIPLAYER_XP = "StatMultiplayerXp";
    public const string STAT_MULTIPLAYER_RANK = "StatMultiplayerRank";
    public const string STAT_WINS = "StatWins";
    public const string STAT_LOSSES = "StatLosses";
    public const string STAT_FAVORITE_CAPTAIN = "StatFavoriteCaptain";
    public const string STAT_FAVORITE_SQUARE = "StatFavoriteSquare";
    public const string STAT_FAVORITE_WEAPON = "StatFavoriteWeapon";
    public const string STAT_SECONDS_PLAYED = "StatSecondsPlayed";
    public const int ROOM_NUM_EXPECTED_PLAYERS = 2;
}

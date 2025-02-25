namespace PalworldRcon;

public class Player
{
    public string PlayerName;
    public string CharacterID;
    public string SteamID;

    public Player(string playerName, string characterID, string steamID)
    {
        PlayerName = playerName;
        CharacterID = characterID;
        SteamID = steamID;
    }

    public override string ToString()
    {
        return $"Player Name: {PlayerName} | SteamID: {SteamID}";
    }
}
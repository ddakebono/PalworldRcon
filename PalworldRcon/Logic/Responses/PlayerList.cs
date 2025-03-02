using System.Collections.Generic;
using System.Text.RegularExpressions;
using CoreRCON.Parsers;

namespace PalworldRcon.Logic.Responses;

public class PlayerList : IParseable
{
    public List<Player> Players { get; set; } = new();
}

public class PlayerListParser : IParser<PlayerList>
{
    public bool IsMatch(string input)
    {
        return input.StartsWith("name,playeruid,steamid");
    }

    public PlayerList Load(GroupCollection groups)
    {
        throw new System.NotImplementedException();
    }

    public PlayerList Parse(string input)
    {
        Regex regex = new Regex(Pattern, RegexOptions.Compiled);
        MatchCollection matches = regex.Matches(input);

        var playerList = new PlayerList();

        foreach (Match match in matches)
        {
            playerList.Players.Add(new Player(match.Groups["playername"].Value, match.Groups["charid"].Value, match.Groups["steamid"].Value));
        }

        return playerList;
    }

    public PlayerList Parse(Group group)
    {
        throw new System.NotImplementedException();
    }

    public string Pattern => "(?'playername'.*),(?'charid'(?!playeruid).*),(?'steamid'.*)";
}
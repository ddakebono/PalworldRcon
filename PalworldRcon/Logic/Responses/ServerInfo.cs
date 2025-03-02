using System.Text.RegularExpressions;
using CoreRCON.Parsers;

namespace PalworldRcon.Logic.Responses;

public class ServerInfo : IParseable
{
    public string ServerName { get; set; } = "Not Connected";
    public string ServerVersion { get; set; } = "v0.0.0.0";
}

public class ServerInfoParser : DefaultParser<ServerInfo>
{
    public override ServerInfo Load(GroupCollection groups)
    {
        return new ServerInfo()
        {
            ServerName = groups["servername"].Value,
            ServerVersion = groups["version"].Value,
        };
    }

    public override string Pattern => "\\[(?'version'.*)\\] (?'servername'.*)";
}
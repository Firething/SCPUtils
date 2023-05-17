﻿namespace SCPUtils
{
    using Newtonsoft.Json;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using PluginAPI.Core;

    public static class DiscordWebHook
    {
        public static async Task<WebResponse> Message(string userid, PluginAPI.Core.Player player)
        {
            WebResponse response = null;

            WebRequest wr = (HttpWebRequest)WebRequest.Create(ScpUtils.StaticInstance.configs.WebhookUrl);

            wr.ContentType = "application/json";
            wr.Method = "POST";
            wr.Timeout = 1350;

            using (var vr = new StreamWriter(wr.GetRequestStream()))
            {
                string json = JsonConvert.SerializeObject(new
                {
                    username = ScpUtils.StaticInstance.configs.WebhookNickname,
                    embeds = new[]
                    {
                        new
                        {

                            title = "Mute evasion report!",
                            description = $"Mute evasion detected! Userid of muted user: {userid}\n" +
                            $"Player info:\n" +
                            $"Username: {player.Nickname}\n" +
                            $"User-ID: {player.UserId}\n" +
                            $"Temporary ID: {player.PlayerId}",
                            color = "25233"
                        }
                    }
                });
                vr.Write(json);
            }
            return response = await wr.GetResponseAsync();

        }

    }
}
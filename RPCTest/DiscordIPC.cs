using Dec.DiscordIPC;
using Dec.DiscordIPC.Commands;
using Dec.DiscordIPC.Events;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using Windows.Media.Protection.PlayReady;
using static Dec.DiscordIPC.Entities.VoiceSettings;
using static DiscordAudioController.ConfigManager;

namespace DiscordAudioController
{
    public class DiscordIPC
    {

        public class AuthTokens
        {
            public string access_token { get; set; } = string.Empty;
            public DateTime access_token_expire { get; set; } = DateTime.Now;
            public string refresh_token { get; set; } = string.Empty;
        }
        private static async Task<Authorize.Data> GetCodeAsync(Dec.DiscordIPC.DiscordIPC client)
        {
            Authorize.Data codeResponse = await client.SendCommandAsync(
            new Authorize.Args()
            {
                scopes = new List<string>() { "rpc" },
                client_id = Program.config.client_id
            });
            return codeResponse;
        }
        public static async Task Authenticate(Dec.DiscordIPC.DiscordIPC client)
        {
            await client.SendCommandAsync(new Authenticate.Args()
            {
                access_token = Program.config.access_token
            });
        }
        public static async Task<bool> AuthorizeAsync(Dec.DiscordIPC.DiscordIPC client)
        {

            if (!string.IsNullOrEmpty(Program.config.refresh_token))
            {
                // todo refresh token when expired
                try
                {
                    Console.WriteLine("Refreshing token!");

                    DiscordIPC.AuthTokens authTokens = DiscordIPC.getRefreshToken();

                    ConfigManager.UpdateTokens(ref Program.config, authTokens);
                    ConfigManager.Save(ref Program.config);
                }
                catch (ErrorResponseException)
                {
                    
                    Console.WriteLine("Failed to refresh the token! Relaunch the app");

                    // if failed reset the token so it can be refreshed again!
                    Program.config.refresh_token = "";
                    ConfigManager.Save(ref Program.config);

                    return false;
                }
            }
            else
            {
                try
                {

                    Authorize.Data codeResponse = await GetCodeAsync(client);

                    DiscordIPC.AuthTokens authTokens = DiscordIPC.getAccessTokenFromAuthCode(codeResponse.code);

                    ConfigManager.UpdateTokens(ref Program.config, authTokens);
                    ConfigManager.Save(ref Program.config);

                }
                catch (ErrorResponseException)
                {
                    Console.WriteLine("User denied authorization");
                    return false;
                }
            }

            return true;
        }

        private static AuthTokens getRefreshToken()
        {

            var parameters = new Dictionary<string, string> {
                {"grant_type", "refresh_token"},
                {"refresh_token",Program.config.refresh_token}
            };

            var reply = RequestAsync(
                "https://discord.com/api/oauth2/token",
                buildQueryString(parameters)).GetAwaiter().GetResult();

            return HandleAuthReply(reply);

        }

        private static async Task<string> RequestAsync(string uri, string query)
        {
            string authBasic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Program.config.client_id}:{Program.config.client_secret}"));

            HttpClient client = new HttpClient();
            var requestContent = new StringContent(query);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authBasic);
            HttpResponseMessage responseAuthorization = await client.PostAsync(uri, requestContent);


            return await responseAuthorization.Content.ReadAsStringAsync();


        }

        private static string buildQueryString(Dictionary<string, string> parameters)
        {
            return parameters.Aggregate(
                "",
                (c, p) => c + ("&" + p.Key + "=" + HttpUtility.UrlEncode(p.Value)))
                .Substring(1);
        }

        public static AuthTokens getAccessTokenFromAuthCode(string code)
        {
            var parameters = new Dictionary<string, string> {
                {"redirect_uri", "127.0.0.1:3000"},
                {"code", code},
                {"grant_type", "authorization_code"}
            };

            var reply = RequestAsync(
                "https://discord.com/api/oauth2/token",
                buildQueryString(parameters)).GetAwaiter().GetResult();

            return HandleAuthReply(reply);
        }

        private static AuthTokens HandleAuthReply(string reply)
        {
            try
            {
                var replyObj = JsonSerializer.Deserialize<Dictionary<string, object>>(reply);

                AuthTokens authTokens = new()
                {
                    access_token = replyObj["access_token"].ToString() ?? "",
                    refresh_token = replyObj["refresh_token"].ToString() ?? "",
                    access_token_expire = DateTime.Now.AddSeconds(double.Parse(replyObj["expires_in"].ToString())),
                };

                return authTokens;
            }
            catch (Exception)
            {
                return new();
            }

        }

        public static class VoiceSettings
        {
            public static bool mute { get; set; } = false;
            public static bool deaf { get; set; } = false;
            public static string mode_type { get; set; } = "";

        }
        private static EventHandler<VoiceSettingsUpdate.Data> VoiceSettingsHandler = (sender, data) =>
        {
            VoiceSettings.mute = data.mute ?? false;
            VoiceSettings.deaf = data.deaf ?? false;
            VoiceSettings.mode_type = data.mode.type;

            SerialPortManager.SendVoiceSettings();
        };


        public static bool IsDiscordAvailable = false;
        public static Dec.DiscordIPC.DiscordIPC client = new(Program.config.client_id);

        public static async void DiscordIPCThread()
        {
            #region DiscordClient

            while (true)
            {
                if (!IsDiscordAvailable)
                {
                    try
                    {
                        Console.WriteLine("Trying to connect to Discord...");
                        await client.InitAsync();
                        IsDiscordAvailable = true;
                        // Authorize
                        if (Program.config.access_token_expire < DateTime.Now)
                        {
                            if (!await DiscordIPC.AuthorizeAsync(client))
                            {
                                return;
                            }
                        }

                        try
                        {
                            await DiscordIPC.Authenticate(client);
                        }
                        catch (ErrorResponseException)
                        {
                            Console.WriteLine("Something went wrong with the access token!");
                            if (!await DiscordIPC.AuthorizeAsync(client))
                            {
                                return;
                            }
                            await DiscordIPC.Authenticate(client);
                        }

                        Console.WriteLine("Connected to Discord IPC");

                        client.OnVoiceSettingsUpdate += VoiceSettingsHandler;
                        await client.SubscribeAsync(new VoiceSettingsUpdate.Args());


                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Couldn't connect to Discord! Is it even running? Retrying in 5s...");
                        Thread.Sleep(5000);
                    }
                }
                else
                {
                    try
                    {
                        await client.SendCommandAsync(new GetSelectedVoiceChannel.Args() { });
                        Thread.Sleep(5000);
                    }
                    catch(Exception ex)
                    {
                        IsDiscordAvailable = false;
                        Console.WriteLine($"IPC connection lost: {ex.Message} Waiting for Discord Client...");
                        client.OnVoiceSettingsUpdate -= VoiceSettingsHandler;
                        client.Dispose();
                    }
                    
                }

            }
            #endregion
        }
    }
}

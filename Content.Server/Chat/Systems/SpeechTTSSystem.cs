using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Shared.CCVar;
using Content.Shared.Chat; // Needs MsgTTSAudio
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Server.Chat.Systems
{
    public sealed class SpeechTTSSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly IServerNetManager _netManager = default!;
        private readonly HttpClient _httpClient = new();

        public override void Initialize()
        {
            base.Initialize();
            // Server registers the message so it knows how to send it
            _netManager.RegisterNetMessage<MsgTTSAudio>();
        }

        public async void AttemptSpeech(EntityUid source, string message)
        {
            if (!_cfg.GetCVar(CCVars.TTSEnabled)) return;
            if (string.IsNullOrWhiteSpace(message)) return;

            var voiceId = "aidar";

            var audioData = await SynthesizeSpeech(message, voiceId);
            if (audioData == null) return;

            var msg = new MsgTTSAudio { Data = audioData, Source = source };
            _netManager.ServerSendToAll(msg);
        }

        private async Task<byte[]?> SynthesizeSpeech(string text, string speaker)
        {
            try
            {
                var apiUrl = _cfg.GetCVar(CCVars.TTSApiUrl);
                var token = _cfg.GetCVar(CCVars.TTSApiToken);
                var payload = new { text, speaker, language = "ru", api_token = token };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", token);

                var response = await _httpClient.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    var respJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(respJson);

                    if (doc.RootElement.TryGetProperty("results", out var results) &&
                        results.GetArrayLength() > 0 &&
                        results[0].TryGetProperty("audio", out var audioProp))
                    {
                        var base64 = audioProp.GetString();
                        if (!string.IsNullOrEmpty(base64))
                            return Convert.FromBase64String(base64);
                    }
                }
            }
            catch { /* Ignore */ }
            return null;
        }
    }
}

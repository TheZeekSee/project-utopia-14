using System;
using System.Collections.Generic;
using System.IO;
using Content.Shared.Chat;
using Content.Shared.Corvax.CCCVars;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.Audio
{
    public sealed class TTSAudioSystem : EntitySystem
    {
        [Dependency] private readonly IAudioManager _audioManager = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        // предотвращаем прерывание звука, поток буферизуется полностью в LoadAudioOggVorbis
        private readonly Dictionary<IAudioSource, MemoryStream> _activeStreams = new();

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // очищаем завершенные аудиоисточники и их потоки
            var toRemove = new List<IAudioSource>();
            foreach (var (source, stream) in _activeStreams)
            {
                if (!source.Playing)
                {
                    toRemove.Add(source);
                }
            }

            foreach (var source in toRemove)
            {
                if (_activeStreams.TryGetValue(source, out var stream))
                {
                    _activeStreams.Remove(source);
                    stream.Dispose();
                    source.Dispose();
                }
            }
        }
        public void PlayTTSAudio(byte[] audioData, EntityUid source, bool isRadio)
        {
            Logger.InfoS("TTS", $"PlayTTSAudio called: isRadio={isRadio}, source={source}, dataLength={audioData.Length}");
            // создаём поток и поддерживаем его
            var stream = new MemoryStream(audioData);
            try
            {
                var audioStream = _audioManager.LoadAudioOggVorbis(stream);
                var audioSource = _audioManager.CreateAudioSource(audioStream);

                if (audioSource != null)
                {
                    // скэйлим звук по настройкам громкости TTS из CCCVars
                    var ttsVolume = _cfg.GetCVar(CCCVars.TTSVolume);
                    var ttsGain = ttsVolume * ContentAudioSystem.TtsMultiplier;
                    var audioParams = AudioParams.Default
                        .WithVolume(-4f)
                        .WithPitchScale(1.0f); // Радио идёт от пайтона с нормальным питчем

                    if (isRadio)
                        audioSource.Global = true;
                        audioSource.Volume = audioParams.Volume;
                        audioSource.Pitch = audioParams.Pitch;
                        audioSource.Gain = ttsGain;
                        Logger.InfoS("TTS", $"Radio TTS: Playing global audio, Volume={audioSource.Volume}, Pitch={audioSource.Pitch}, Gain={audioSource.Gain}");
                    }
                    else if (source.Valid && EntityManager.TryGetComponent<TransformComponent>(source, out var xform))
                    {
                        // Локальный звук от источника
                        audioSource.Global = false;
                        audioSource.Position = xform.WorldPosition;
                        audioSource.RolloffFactor = audioParams.RolloffFactor;
                        audioSource.MaxDistance = audioParams.MaxDistance;
                        audioSource.ReferenceDistance = audioParams.ReferenceDistance;
                        audioSource.Volume = audioParams.Volume;
                        audioSource.Pitch = audioParams.Pitch;
                        audioSource.Gain = ttsGain;
                        Logger.InfoS("TTS", $"Local TTS: Playing positional audio at {xform.WorldPosition}, Volume={audioSource.Volume}, Gain={audioSource.Gain}");
                    }
                    else
                    {
                        // Не играется голос
                        Logger.WarningS("TTS", $"TTS: Invalid source entity {source}, disposing audio");
                        audioSource.Dispose();
                        stream.Dispose();
                        return;
                    }

                    _activeStreams[audioSource] = stream;

                    audioSource.StartPlaying();
                    Logger.InfoS("TTS", "TTS audio playback started");
                }
                else
                {
                    Logger.WarningS("TTS", "TTS: Failed to create audio source");
                    stream.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorS("TTS", $"TTS: Exception during audio playback: {ex}");
                stream.Dispose();
            }
        }
    }
}

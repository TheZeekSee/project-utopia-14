using System.IO;
using Content.Shared.Chat;
using Robust.Client.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Sources;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Content.Client.Audio
{
    public sealed class TTSAudioSystem : EntitySystem
    {
        [Dependency] private readonly IAudioManager _audioManager = default!;
        public void PlayTTSAudio(byte[] audioData, EntityUid source)
        {
            var stream = new MemoryStream(audioData);
            try
            {
                var audioStream = _audioManager.LoadAudioOggVorbis(stream);
                var audioSource = _audioManager.CreateAudioSource(audioStream);

                if (audioSource != null)
                {
                    if (source.Valid && EntityManager.TryGetComponent<TransformComponent>(source, out var xform))
                    {
                        audioSource.Global = false;
                        audioSource.Position = xform.WorldPosition;
                        audioSource.RolloffFactor = 100f; // Надо поднастроить (По дефолту 1)
                        audioSource.MaxDistance = 20f; // Голоса около ~7 тайлов
                    }
                    else
                    {
                        // Глобал отключён из-за "призрачных" голосов, надо придумать что сделать с радио и аннонсами

                        // Не играется голос
                        audioSource.Dispose();
                        stream.Dispose();
                        return;
                    }

                    audioSource.Volume = -2f; // Слайт стонкс или не стонкс
                    audioSource.StartPlaying();
                }
            }
            catch
            {
                stream.Dispose();
            }
        }
    }
}

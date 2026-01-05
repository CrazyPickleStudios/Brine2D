using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;

namespace Brine2D.Audio.ECS;

/// <summary>
/// System that processes audio components and plays sounds.
/// Lives in Brine2D.Audio.ECS because it's the bridge between ECS and Audio.
/// </summary>
public class AudioSystem : IUpdateSystem
{
    public int UpdateOrder => 400;

    private readonly IEntityWorld _world;
    private readonly IAudioService _audio;

    public AudioSystem(IEntityWorld world, IAudioService audio)
    {
        _world = world;
        _audio = audio;
    }

    public void Update(GameTime gameTime)
    {
        var audioSources = _world.GetEntitiesWithComponent<AudioSourceComponent>();

        foreach (var entity in audioSources)
        {
            var audioSource = entity.GetComponent<AudioSourceComponent>();

            if (audioSource == null || !audioSource.IsEnabled)
                continue;

            // Handle play trigger
            if (audioSource.TriggerPlay)
            {
                audioSource.TriggerPlay = false;

                if (audioSource.SoundEffect != null)
                {
                    _audio.PlaySound(audioSource.SoundEffect, audioSource.Volume, audioSource.LoopCount);
                    audioSource.IsPlaying = true;
                }
                else if (audioSource.Music != null)
                {
                    _audio.PlayMusic(audioSource.Music, audioSource.LoopCount);
                    audioSource.IsPlaying = true;
                }
            }

            // Handle stop trigger
            if (audioSource.TriggerStop)
            {
                audioSource.TriggerStop = false;

                if (audioSource.Music != null)
                {
                    _audio.StopMusic();
                }

                audioSource.IsPlaying = false;
            }

            // Auto-play on enable
            if (audioSource.PlayOnEnable && audioSource.IsEnabled && !audioSource.IsPlaying)
            {
                audioSource.TriggerPlay = true;
            }
        }
    }
}
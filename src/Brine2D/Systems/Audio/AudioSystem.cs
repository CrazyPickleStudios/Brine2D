using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;
using System.Collections.Concurrent;
using System.Numerics;
using Brine2D.Audio;

namespace Brine2D.Systems.Audio;

/// <summary>
/// System that processes audio components with 2D spatial audio support.
/// Tracks component state to properly stop sounds when disabled.
/// </summary>
public class AudioSystem : IUpdateSystem
{
    public int UpdateOrder => 400;

    private readonly IEntityWorld _world;
    private readonly IAudioService _audio;
    
    // Thread-safe queue for audio events from SDL thread
    private readonly ConcurrentQueue<AudioEvent> _audioEvents = new();
    
    // Track which entity owns which track handle
    private readonly Dictionary<Entity, nint> _entityTracks = new();
    
    // Track previous enabled state to detect changes
    private readonly Dictionary<Entity, bool> _previousEnabledState = new();

    public AudioSystem(IEntityWorld world, IAudioService audio)
    {
        _world = world;
        _audio = audio;
        
        _audio.OnTrackStopped += OnTrackStopped;
    }

    private void OnTrackStopped(nint track)
    {
        // Called from SDL audio thread - queue event for game thread
        _audioEvents.Enqueue(new AudioEvent 
        { 
            Type = AudioEventType.TrackStopped, 
            Track = track 
        });
    }

    public void Update(GameTime gameTime)
    {
        // Process audio events from SDL thread (thread-safe)
        ProcessAudioEvents();
        
        // Find the active audio listener
        var listener = FindActiveListener();

        var audioSources = _world.GetEntitiesWithComponent<AudioSourceComponent>();

        foreach (var entity in audioSources)
        {
            var audioSource = entity.GetComponent<AudioSourceComponent>();

            if (audioSource == null)
                continue;

            bool wasEnabled = _previousEnabledState.TryGetValue(entity, out var prevState) && prevState;
            bool isNowDisabled = wasEnabled && !audioSource.IsEnabled;
            
            if (isNowDisabled && audioSource.IsPlaying)
            {
                // Component was disabled - stop the sound
                if (_entityTracks.TryGetValue(entity, out var track))
                {
                    _audio.StopTrack(track);
                    _entityTracks.Remove(entity);
                }
                audioSource.IsPlaying = false;
            }
            
            _previousEnabledState[entity] = audioSource.IsEnabled;

            if (!audioSource.IsEnabled)
                continue;

            // Update spatial audio if enabled
            if (audioSource.EnableSpatialAudio && listener != null)
            {
                UpdateSpatialAudio(audioSource, entity, listener);
                
                // Update playing track with new spatial values every frame
                if (audioSource.IsPlaying && _entityTracks.TryGetValue(entity, out var activeTrack))
                {
                    _audio.UpdateTrackSpatialAudio(activeTrack, audioSource.SpatialVolume, audioSource.SpatialPan);
                }
            }
            else
            {
                audioSource.SpatialVolume = audioSource.Volume;
                audioSource.SpatialPan = 0f;
            }

            // Auto-play on enable (check if newly enabled)
            if (audioSource.PlayOnEnable && !audioSource.IsPlaying && !wasEnabled)
            {
                audioSource.TriggerPlay = true;
            }

            // Handle play trigger
            if (audioSource.TriggerPlay)
            {
                audioSource.TriggerPlay = false;

                // Stop existing track if playing
                if (_entityTracks.TryGetValue(entity, out var existingTrack))
                {
                    _audio.StopTrack(existingTrack);
                    _entityTracks.Remove(entity);
                }

                if (audioSource.SoundEffect != null)
                {
                    var track = _audio.PlaySoundWithTrack(
                        audioSource.SoundEffect, 
                        audioSource.SpatialVolume, 
                        audioSource.LoopCount,
                        audioSource.SpatialPan);
                    
                    if (track != nint.Zero)
                    {
                        _entityTracks[entity] = track;
                        audioSource.IsPlaying = true;
                    }
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

                if (_entityTracks.TryGetValue(entity, out var track))
                {
                    _audio.StopTrack(track);
                    _entityTracks.Remove(entity);
                }
                
                if (audioSource.Music != null)
                {
                    _audio.StopMusic();
                }

                audioSource.IsPlaying = false;
            }
        }
    }

    private void ProcessAudioEvents()
    {
        while (_audioEvents.TryDequeue(out var audioEvent))
        {
            if (audioEvent.Type == AudioEventType.TrackStopped)
            {
                // Find which entity owned this track
                Entity? ownerEntity = null;
                foreach (var kvp in _entityTracks)
                {
                    if (kvp.Value == audioEvent.Track)
                    {
                        ownerEntity = kvp.Key;
                        break;
                    }
                }

                if (ownerEntity != null)
                {
                    var audioSource = ownerEntity.GetComponent<AudioSourceComponent>();
                    if (audioSource != null)
                    {
                        audioSource.IsPlaying = false;
                    }
                    _entityTracks.Remove(ownerEntity);
                }
            }
        }
    }

    private Entity? FindActiveListener()
    {
        var listeners = _world.GetEntitiesWithComponent<AudioListenerComponent>();
        
        // Return first enabled listener
        foreach (var listener in listeners)
        {
            var listenerComp = listener.GetComponent<AudioListenerComponent>();
            if (listenerComp?.IsEnabled == true)
            {
                return listener;
            }
        }

        return null;
    }

    private void UpdateSpatialAudio(AudioSourceComponent audioSource, Entity sourceEntity, Entity listenerEntity)
    {
        var sourceTransform = sourceEntity.GetComponent<TransformComponent>();
        var listenerTransform = listenerEntity.GetComponent<TransformComponent>();
        var listenerComp = listenerEntity.GetComponent<AudioListenerComponent>();

        if (sourceTransform == null || listenerTransform == null || listenerComp == null)
        {
            // Fallback to non-spatial
            audioSource.SpatialVolume = audioSource.Volume;
            audioSource.SpatialPan = 0f;
            return;
        }

        // Calculate distance
        var sourcePos = sourceTransform.WorldPosition;
        var listenerPos = listenerTransform.WorldPosition;
        var distance = Vector2.Distance(sourcePos, listenerPos);

        // Calculate distance-based attenuation
        float distanceAttenuation = CalculateDistanceAttenuation(
            distance, 
            audioSource.MinDistance, 
            audioSource.MaxDistance, 
            audioSource.RolloffFactor);

        // Calculate stereo panning
        float pan = CalculateStereoPan(sourcePos, listenerPos, audioSource.SpatialBlend);

        // Apply spatial processing
        audioSource.SpatialVolume = audioSource.Volume * distanceAttenuation * listenerComp.GlobalSpatialVolume;
        audioSource.SpatialPan = pan;
    }

    private static float CalculateDistanceAttenuation(float distance, float minDistance, float maxDistance, float rolloffFactor)
    {
        // No attenuation within min distance
        if (distance <= minDistance)
            return 1.0f;

        // Silent beyond max distance
        if (distance >= maxDistance)
            return 0.0f;

        // Calculate attenuation curve
        float normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);
        
        // Apply rolloff curve
        float attenuation = rolloffFactor switch
        {
            0f => 1.0f, // No rolloff
            1f => 1.0f - normalizedDistance, // Linear
            _ => MathF.Pow(1.0f - normalizedDistance, rolloffFactor) // Custom curve
        };

        return Math.Clamp(attenuation, 0f, 1f);
    }

    private static float CalculateStereoPan(Vector2 sourcePos, Vector2 listenerPos, float spatialBlend)
    {
        if (spatialBlend <= 0.001f)
            return 0f; // No panning

        // Calculate direction from listener to source
        var direction = sourcePos - listenerPos;
        
        if (direction.LengthSquared() < 0.001f)
            return 0f; // Source at listener position

        direction = Vector2.Normalize(direction);

        // X component determines left/right pan
        // Positive X = right, Negative X = left
        float pan = direction.X;

        // Apply spatial blend (0 = center only, 1 = full stereo)
        pan *= spatialBlend;

        return Math.Clamp(pan, -1f, 1f);
    }

    private enum AudioEventType
    {
        TrackStopped
    }

    private struct AudioEvent
    {
        public AudioEventType Type;
        public nint Track;
    }
}
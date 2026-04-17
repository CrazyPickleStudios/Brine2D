using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace Brine2D.Systems.Audio;

/// <summary>
/// System that processes audio components with 2D spatial audio support.
/// Tracks component state to properly stop sounds when disabled.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Update"/> must be called from a single thread (the game thread).
/// All state (<see cref="_entityTracks"/>, <see cref="_trackEntities"/>, 
/// <see cref="_previousEnabledState"/>) is accessed exclusively during Update.
/// </para>
/// <para>
/// Sound-effect tracks are tracked via <see cref="_entityTracks"/>/<see cref="_trackEntities"/>
/// and cleaned up by polling <see cref="IAudioPlayer.IsTrackAlive"/> each frame.
/// A single entity can have multiple concurrent tracks (overlapping sounds).
/// <see cref="SoundEffectSourceComponent.IsPlaying"/> remains <see langword="true"/>
/// until the last track completes. Music tracks are not stored here because only
/// one plays at a time — completion is detected by polling
/// <see cref="IAudioPlayer.IsMusicPlaying"/> in <see cref="SyncMusicEntityState"/>.
/// </para>
/// </remarks>
public sealed class AudioSystem : UpdateSystemBase, IDisposable
{
    private readonly IAudioPlayer _audio;
    private readonly ILogger<AudioSystem> _logger;
    private readonly Dictionary<Entity, List<nint>> _entityTracks = new();
    private readonly Dictionary<nint, TrackState> _trackEntities = new();
    private readonly Dictionary<Entity, bool> _previousEnabledState = new();
    private readonly Dictionary<Entity, bool> _previousBusPauseState = new();
    private readonly Dictionary<Entity, Vector2> _previousSourcePositions = new();
    private readonly HashSet<Entity> _activeEntities = new();
    private readonly HashSet<Entity> _individuallyPausedTracks = new();
    private readonly List<Entity> _staleEntities = new();
    private readonly List<nint> _stoppedTracks = new();
    private readonly List<nint> _fadedOutTracks = new();
    private (Entity Entity, MusicSourceComponent Component)? _musicEntity;
    private Vector2 _previousListenerPosition;
    private bool _hasListenerPreviousPosition;
    private bool _multipleListenersWarned;
    private int _disposed;
    private CachedEntityQuery<AudioSourceComponent>? _audioSourceQuery;
    private CachedEntityQuery<AudioListenerComponent>? _audioListenerQuery;

    public AudioSystem(IAudioPlayer audio, ILogger<AudioSystem> logger)
    {
        ArgumentNullException.ThrowIfNull(audio);
        ArgumentNullException.ThrowIfNull(logger);
        _audio = audio;
        _logger = logger;
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        PollStoppedTracks();
        float deltaTime = (float)gameTime.DeltaTime;
        AdvanceTrackFades(deltaTime);
        _audio.Update(deltaTime);

        var listener = FindActiveListener(world);

        Vector2 listenerVelocity = Vector2.Zero;
        Vector2 currentListenerPos = Vector2.Zero;
        bool hasListenerPos = false;

        if (listener != null)
        {
            var lt = listener.GetComponent<TransformComponent>();
            if (lt != null)
            {
                currentListenerPos = lt.Position;
                hasListenerPos = true;
                if (_hasListenerPreviousPosition && deltaTime > 0f)
                    listenerVelocity = (currentListenerPos - _previousListenerPosition) / deltaTime;
            }
        }

        _audioSourceQuery ??= world.CreateCachedQuery<AudioSourceComponent>().Build();

        _activeEntities.Clear();

        foreach (var (entity, audioSource) in _audioSourceQuery)
        {
            _activeEntities.Add(entity);

            bool wasEnabled = _previousEnabledState.TryGetValue(entity, out var prevState) && prevState;
            bool isNowDisabled = wasEnabled && !audioSource.IsEnabled;

            if (isNowDisabled && audioSource.IsPlaying)
            {
                if (_entityTracks.TryGetValue(entity, out var disableTracks))
                {
                    foreach (var track in disableTracks)
                    {
                        _audio.StopTrack(track);
                        _trackEntities.Remove(track);
                    }
                    _entityTracks.Remove(entity);
                }

                if (audioSource is SoundEffectSourceComponent sfxDisable)
                {
                    sfxDisable.IsPaused = false;
                    _individuallyPausedTracks.Remove(entity);
                    _previousBusPauseState.Remove(entity);
                    _previousSourcePositions.Remove(entity);
                }

                bool musicFadingOut = false;
                if (audioSource is MusicSourceComponent mc && _musicEntity?.Entity == entity)
                {
                    // NOTE: If a crossfade is in progress, StopMusic with a fade duration
                    // calls FinishCrossfade first, which jump-cuts the outgoing track before
                    // starting the new fade-out. This is an SDL3_mixer limitation.
                    if (mc.FadeOutDuration > 0f)
                    {
                        _audio.StopMusic(mc.FadeOutDuration);
                        musicFadingOut = true;
                    }
                    else
                    {
                        _audio.StopMusic();
                        _musicEntity = null;
                        mc.IsPaused = false;
                    }
                }

                if (!musicFadingOut)
                    audioSource.IsPlaying = false;
            }

            _previousEnabledState[entity] = audioSource.IsEnabled;

            if (!audioSource.IsEnabled)
            {
                if (audioSource is SoundEffectSourceComponent)
                    _previousSourcePositions.Remove(entity);

                continue;
            }

            if (audioSource is SoundEffectSourceComponent sfxSource)
            {
                if (sfxSource.EnableSpatialAudio && listener != null)
                {
                    UpdateSpatialAudio(sfxSource, entity, listener, listenerVelocity, deltaTime);
                }
                else
                {
                    sfxSource.SpatialVolume = sfxSource.Volume;
                    sfxSource.SpatialPan = 0f;
                    sfxSource.SpatialPitch = 1.0f;
                    _previousSourcePositions.Remove(entity);
                }

                if (sfxSource.IsPlaying && _entityTracks.TryGetValue(entity, out var activeTracks))
                {
                    foreach (var activeTrack in activeTracks)
                    {
                        if (_trackEntities.TryGetValue(activeTrack, out var state))
                        {
                            _audio.SetTrackVolumeAndPan(activeTrack, sfxSource.SpatialVolume * state.VolumeScale * state.FadeGain, sfxSource.SpatialPan);
                            _audio.SetTrackPitch(activeTrack, (sfxSource.Pitch + state.PitchOffset) * sfxSource.SpatialPitch);
                        }
                    }
                }
            }

            ProcessTriggers(entity, audioSource, wasEnabled);

            if (audioSource is MusicSourceComponent musicComp)
            {
                ProcessMusicTriggers(entity, musicComp);

                if (_musicEntity?.Entity == entity && musicComp.IsPlaying && !_audio.IsMusicFadingOut)
                {
                    _audio.SetMusicTrackVolume(musicComp.Volume);
                    _audio.SetMusicPitch(musicComp.Pitch);
                }
            }

            if (audioSource is SoundEffectSourceComponent sfxTriggers)
            {
                ProcessSfxPauseTriggers(entity, sfxTriggers);
                ProcessSfxStopOldest(entity, sfxTriggers);
            }
        }

        if (hasListenerPos)
        {
            _previousListenerPosition = currentListenerPos;
            _hasListenerPreviousPosition = true;
        }
        else
        {
            _hasListenerPreviousPosition = false;
        }

        PruneDestroyedEntities();
        SyncMusicEntityState();
        SyncBusPauseState();
    }

    private void PollStoppedTracks()
    {
        if (_trackEntities.Count == 0)
            return;

        _stoppedTracks.Clear();
        foreach (var track in _trackEntities.Keys)
        {
            if (!_audio.IsTrackAlive(track))
                _stoppedTracks.Add(track);
        }

        if (_stoppedTracks.Count == 0)
            return;

        foreach (var track in _stoppedTracks)
            CleanupTrackState(track);
    }

    private void CleanupTrackState(nint track)
    {
        if (_trackEntities.Remove(track, out var owner))
        {
            if (_entityTracks.TryGetValue(owner.Entity, out var tracks))
            {
                tracks.Remove(track);
                if (tracks.Count == 0)
                {
                    _entityTracks.Remove(owner.Entity);
                    owner.Component.IsPlaying = false;
                    owner.Component.IsPaused = false;
                    owner.Component.PlaybackEnded = true;
                    _individuallyPausedTracks.Remove(owner.Entity);
                    _previousBusPauseState.Remove(owner.Entity);
                }
            }
        }
    }

    private void ProcessTriggers(Entity entity, AudioSourceComponent audioSource, bool wasEnabled)
    {
        if (audioSource.PlayOnEnable && !audioSource.IsPlaying && !wasEnabled)
        {
            audioSource.TriggerPlay = true;
        }

        if (audioSource.TriggerPlay && audioSource.TriggerStop)
        {
            _logger.LogWarning(
                "Entity {Name} ({Id}) has both TriggerPlay and TriggerStop set; both cleared",
                entity.Name, entity.Id);
            audioSource.TriggerPlay = false;
            audioSource.TriggerStop = false;
            return;
        }

        if (audioSource.TriggerPlay)
        {
            audioSource.TriggerPlay = false;
            audioSource.PlaybackEnded = false;

            switch (audioSource)
            {
                case SoundEffectSourceComponent sfx:
                    PlaySoundEffect(entity, sfx);
                    break;
                case MusicSourceComponent music:
                    PlayMusicSource(entity, music);
                    break;
                default:
                    _logger.LogDebug(
                        "TriggerPlay consumed but entity {Name} ({Id}) has no concrete audio source",
                        entity.Name, entity.Id);
                    break;
            }
        }

        if (audioSource.TriggerStop)
        {
            audioSource.TriggerStop = false;

            bool startedFadeOut = false;
            if (audioSource is SoundEffectSourceComponent { FadeOutDuration: > 0f } sfxFade
                && _entityTracks.TryGetValue(entity, out var fadeTracks) && fadeTracks.Count > 0)
            {
                bool alreadyFading = false;
                foreach (var track in fadeTracks)
                {
                    if (_trackEntities.TryGetValue(track, out var state) && state.FadeRate < 0f)
                    {
                        alreadyFading = true;
                        break;
                    }
                }

                if (!alreadyFading)
                {
                    float rate = -1f / sfxFade.FadeOutDuration;
                    foreach (var track in fadeTracks)
                    {
                        if (_trackEntities.TryGetValue(track, out var state))
                            state.FadeRate = rate;
                    }
                    startedFadeOut = true;
                }
            }

            if (!startedFadeOut)
            {
                if (_entityTracks.TryGetValue(entity, out var stopTracks))
                {
                    foreach (var track in stopTracks)
                    {
                        _audio.StopTrack(track);
                        _trackEntities.Remove(track);
                    }
                    _entityTracks.Remove(entity);
                }

                bool fadingOut = false;
                if (audioSource is MusicSourceComponent musicComp && _musicEntity?.Entity == entity)
                {
                    // NOTE: If a crossfade is in progress, StopMusic with a fade duration
                    // calls FinishCrossfade first, which jump-cuts the outgoing track before
                    // starting the new fade-out. This is an SDL3_mixer limitation.
                    if (musicComp.FadeOutDuration > 0f)
                    {
                        _audio.StopMusic(musicComp.FadeOutDuration);
                        fadingOut = true;
                    }
                    else
                    {
                        _audio.StopMusic();
                        _musicEntity = null;
                        musicComp.IsPaused = false;
                    }
                }

                if (!fadingOut)
                    audioSource.IsPlaying = false;

                if (audioSource is SoundEffectSourceComponent sfxStop)
                {
                    sfxStop.IsPaused = false;
                    _individuallyPausedTracks.Remove(entity);
                    _previousBusPauseState.Remove(entity);
                }
            }
        }
    }

    private void ProcessMusicTriggers(Entity entity, MusicSourceComponent music)
    {
        if (music.TriggerPause && music.TriggerResume)
        {
            _logger.LogWarning(
                "Entity {Name} ({Id}) has both TriggerPause and TriggerResume set; both cleared",
                entity.Name, entity.Id);
            music.TriggerPause = false;
            music.TriggerResume = false;
            return;
        }

        if (music.TriggerPause)
        {
            music.TriggerPause = false;
            if (_musicEntity?.Entity == entity && music.IsPlaying && !music.IsPaused)
                _audio.PauseMusic();
        }

        if (music.TriggerResume)
        {
            music.TriggerResume = false;
            if (_musicEntity?.Entity == entity && music.IsPaused)
                _audio.ResumeMusic();
        }

        if (music.TriggerSeek)
        {
            music.TriggerSeek = false;
            if (_musicEntity?.Entity == entity && music.IsPlaying)
                _audio.SeekMusic(music.SeekPositionMs);
        }
    }

    private void ProcessSfxPauseTriggers(Entity entity, SoundEffectSourceComponent sfx)
    {
        if (sfx.TriggerPause && sfx.TriggerResume)
        {
            _logger.LogWarning(
                "Entity {Name} ({Id}) has both TriggerPause and TriggerResume set; both cleared",
                entity.Name, entity.Id);
            sfx.TriggerPause = false;
            sfx.TriggerResume = false;
            return;
        }

        if (sfx.TriggerPause)
        {
            sfx.TriggerPause = false;
            if (sfx.IsPlaying && !sfx.IsPaused && _entityTracks.TryGetValue(entity, out var tracks))
            {
                foreach (var track in tracks)
                    _audio.PauseTrack(track);
                sfx.IsPaused = true;
                _individuallyPausedTracks.Add(entity);
            }
        }

        if (sfx.TriggerResume)
        {
            sfx.TriggerResume = false;
            if (sfx.IsPaused && _entityTracks.TryGetValue(entity, out var tracks))
            {
                if (!_audio.IsBusPaused(sfx.Bus))
                {
                    foreach (var track in tracks)
                        _audio.ResumeTrack(track);
                }

                sfx.IsPaused = false;
                _individuallyPausedTracks.Remove(entity);
            }
        }
    }

    private void PlaySoundEffect(Entity entity, SoundEffectSourceComponent sfx)
    {
        if (sfx.SoundEffect == null)
        {
            _logger.LogDebug(
                "TriggerPlay consumed but no SoundEffect assigned on entity {Name} ({Id})",
                entity.Name, entity.Id);
            if (!_entityTracks.TryGetValue(entity, out var existing) || existing.Count == 0)
                sfx.PlaybackEnded = true;
            return;
        }

        if (!sfx.SoundEffect.IsLoaded)
        {
            _logger.LogDebug(
                "TriggerPlay consumed but SoundEffect {Sound} is disposed on entity {Name} ({Id})",
                sfx.SoundEffect.Name, entity.Name, entity.Id);
            if (!_entityTracks.TryGetValue(entity, out var existing) || existing.Count == 0)
                sfx.PlaybackEnded = true;
            return;
        }

        if (sfx.MaxConcurrentInstances > 0)
        {
            int count = 0;
            foreach (var (_, owner) in _trackEntities)
            {
                if (string.Equals(owner.SoundName, sfx.SoundEffect.Name, StringComparison.Ordinal)
                    && ++count >= sfx.MaxConcurrentInstances)
                {
                    _logger.LogDebug(
                        "MaxConcurrentInstances ({Max}) reached for {Sound} on entity {Name} ({Id})",
                        sfx.MaxConcurrentInstances, sfx.SoundEffect.Name, entity.Name, entity.Id);
                    if (!_entityTracks.TryGetValue(entity, out var existing) || existing.Count == 0)
                        sfx.PlaybackEnded = true;
                    return;
                }
            }
        }

        float pitchOffset = 0f;
        float volumeScale = 1f;

        if (sfx.PitchVariation > 0f)
            pitchOffset = Random.Shared.NextSingle() * sfx.PitchVariation * 2f - sfx.PitchVariation;

        if (sfx.VolumeVariation > 0f)
            volumeScale = 1f - Random.Shared.NextSingle() * sfx.VolumeVariation;

        float initialFadeGain = sfx.FadeInDuration > 0f ? 0f : 1f;

        var track = _audio.PlaySound(
            sfx.SoundEffect,
            sfx.SpatialVolume * volumeScale * initialFadeGain,
            sfx.LoopCount,
            sfx.SpatialPan,
            (sfx.Pitch + pitchOffset) * sfx.SpatialPitch,
            sfx.Priority,
            sfx.Bus);

        if (track != nint.Zero)
        {
            if (!_entityTracks.TryGetValue(entity, out var tracks))
            {
                tracks = new List<nint>(1);
                _entityTracks[entity] = tracks;
            }
            tracks.Add(track);
            _trackEntities[track] = new TrackState
            {
                Entity = entity,
                Component = sfx,
                SoundName = sfx.SoundEffect.Name,
                PitchOffset = pitchOffset,
                VolumeScale = volumeScale,
                FadeGain = initialFadeGain,
                FadeRate = sfx.FadeInDuration > 0f ? 1f / sfx.FadeInDuration : 0f
            };
            sfx.IsPlaying = true;

            if (sfx.IsPaused || _audio.IsBusPaused(sfx.Bus))
                _audio.PauseTrack(track);
        }
        else
        {
            if (!_entityTracks.TryGetValue(entity, out var existing) || existing.Count == 0)
                sfx.PlaybackEnded = true;

            _logger.LogDebug(
                "PlaySound returned zero for entity {Name} ({Id}) — tracks: {Active}/{Max}",
                entity.Name, entity.Id, _audio.ActiveSoundTrackCount, _audio.MaxSoundTracks);
        }
    }

    private void PlayMusicSource(Entity entity, MusicSourceComponent music)
    {
        if (music.Music == null)
        {
            _logger.LogDebug(
                "TriggerPlay consumed but no Music assigned on entity {Name} ({Id})",
                entity.Name, entity.Id);
            music.PlaybackEnded = true;
            return;
        }

        if (!music.Music.IsLoaded)
        {
            _logger.LogDebug(
                "TriggerPlay consumed but Music {Music} is disposed on entity {Name} ({Id})",
                music.Music.Name, entity.Name, entity.Id);
            music.PlaybackEnded = true;
            return;
        }

        if (_musicEntity != null && _musicEntity.Value.Entity != entity)
        {
            _musicEntity.Value.Component.IsPlaying = false;
            _musicEntity.Value.Component.IsPaused = false;
        }

        if (music.CrossfadeDuration > 0f)
            _audio.CrossfadeMusic(music.Music, music.CrossfadeDuration, music.LoopCount, music.LoopStartMs, music.Bus);
        else
            _audio.PlayMusic(music.Music, music.LoopCount, music.LoopStartMs, music.Bus);

        _audio.SetMusicTrackVolume(music.Volume);

        music.IsPlaying = _audio.IsMusicPlaying;
        music.IsPaused = false;
        music.PlaybackEnded = false;
        _musicEntity = (entity, music);

        if (music.IsPlaying)
            _audio.SetMusicPitch(music.Pitch);
    }

    private void SyncMusicEntityState()
    {
        if (_musicEntity == null)
            return;

        var mc = _musicEntity.Value.Component;
        if (mc.IsPlaying && !_audio.IsMusicPlaying)
        {
            mc.IsPlaying = false;
            mc.IsPaused = false;
            mc.PlaybackEnded = true;
            _musicEntity = null;
            return;
        }

        mc.IsPaused = _audio.IsMusicPaused;
    }

    /// <summary>
    /// Detects per-bus pause transitions and syncs
    /// <see cref="SoundEffectSourceComponent.IsPaused"/> on tracked sound-effect
    /// entities. When a bus is unpaused, individually paused tracks are re-paused at
    /// the native layer since <see cref="IAudioPlayer.ResumeBus"/> resumes every
    /// tagged track.
    /// </summary>
    private void SyncBusPauseState()
    {
        foreach (var (entity, tracks) in _entityTracks)
        {
            if (tracks.Count == 0)
                continue;

            if (!_trackEntities.TryGetValue(tracks[0], out var owner))
                continue;

            var component = owner.Component;
            bool busPaused = _audio.IsBusPaused(component.Bus);
            bool wasBusPaused = _previousBusPauseState.GetValueOrDefault(entity);
            _previousBusPauseState[entity] = busPaused;

            if (busPaused == wasBusPaused)
                continue;

            bool individuallyPaused = _individuallyPausedTracks.Contains(entity);
            component.IsPaused = busPaused || individuallyPaused;

            if (!busPaused && individuallyPaused)
            {
                foreach (var track in tracks)
                    _audio.PauseTrack(track);
            }
        }
    }

    /// <summary>
    /// Removes tracking state for entities no longer returned by the audio source
    /// query (entity destroyed or component removed). Uses the set of entities
    /// visited during the current frame rather than per-entity <c>GetComponent</c> lookups.
    /// </summary>
    private void PruneDestroyedEntities()
    {
        if (_previousEnabledState.Count == 0) return;

        _staleEntities.Clear();
        foreach (var entity in _previousEnabledState.Keys)
        {
            if (!_activeEntities.Contains(entity))
                _staleEntities.Add(entity);
        }

        if (_staleEntities.Count == 0) return;

        foreach (var entity in _staleEntities)
        {
            if (_entityTracks.Remove(entity, out var tracks))
            {
                foreach (var track in tracks)
                {
                    _trackEntities.Remove(track);
                    _audio.StopTrack(track);
                }
            }

            if (_musicEntity?.Entity == entity)
            {
                _audio.StopMusic();
                _musicEntity = null;
            }

            _previousEnabledState.Remove(entity);
            _individuallyPausedTracks.Remove(entity);
            _previousBusPauseState.Remove(entity);
            _previousSourcePositions.Remove(entity);
        }
    }

    private Entity? FindActiveListener(IEntityWorld world)
    {
        _audioListenerQuery ??= world.CreateCachedQuery<AudioListenerComponent>().Build();

        Entity? found = null;
        bool multiple = false;
        foreach (var (entity, listener) in _audioListenerQuery)
        {
            if (!listener.IsEnabled)
                continue;

            if (found == null)
            {
                found = entity;
            }
            else
            {
                multiple = true;
                break;
            }
        }

        if (multiple && !_multipleListenersWarned)
        {
            _logger.LogWarning("Multiple active AudioListenerComponents detected; only the first is used");
            _multipleListenersWarned = true;
        }

        return found;
    }

    private void UpdateSpatialAudio(
        SoundEffectSourceComponent source,
        Entity sourceEntity,
        Entity listenerEntity,
        Vector2 listenerVelocity,
        float deltaTime)
    {
        var sourceTransform = sourceEntity.GetComponent<TransformComponent>();
        var listenerTransform = listenerEntity.GetComponent<TransformComponent>();
        var listenerComp = listenerEntity.GetComponent<AudioListenerComponent>();

        if (sourceTransform == null || listenerTransform == null || listenerComp == null)
        {
            source.SpatialVolume = source.Volume;
            source.SpatialPan = 0f;
            source.SpatialPitch = 1.0f;
            return;
        }

        var sourcePos = sourceTransform.Position;
        var listenerPos = listenerTransform.Position;
        var distance = Vector2.Distance(sourcePos, listenerPos);

        float distanceAttenuation = CalculateDistanceAttenuation(
            distance,
            source.MinDistance,
            source.MaxDistance,
            source.RolloffFactor);

        float pan = CalculateStereoPan(sourcePos, listenerPos, source.SpatialBlend, listenerTransform.Rotation);

        source.SpatialVolume = source.Volume * distanceAttenuation * listenerComp.GlobalSpatialVolume;
        source.SpatialPan = pan;

        if (source.DopplerFactor > 0.001f && deltaTime > 0f)
        {
            Vector2 sourceVelocity = Vector2.Zero;
            if (_previousSourcePositions.TryGetValue(sourceEntity, out var prevSourcePos))
                sourceVelocity = (sourcePos - prevSourcePos) / deltaTime;

            source.SpatialPitch = CalculateDopplerPitch(
                sourcePos, listenerPos,
                sourceVelocity, listenerVelocity,
                source.DopplerFactor, listenerComp.SpeedOfSound);
        }
        else
        {
            source.SpatialPitch = 1.0f;
        }

        _previousSourcePositions[sourceEntity] = sourcePos;
    }

    private static float CalculateDistanceAttenuation(float distance, float minDistance, float maxDistance, float rolloffFactor)
    {
        if (distance <= minDistance)
            return 1.0f;

        if (distance >= maxDistance)
            return 0.0f;

        if (maxDistance <= minDistance)
            return 1.0f;

        float normalizedDistance = (distance - minDistance) / (maxDistance - minDistance);

        float attenuation = rolloffFactor switch
        {
            0f => 1.0f,
            _ => MathF.Pow(1.0f - normalizedDistance, rolloffFactor)
        };

        return Math.Clamp(attenuation, 0f, 1f);
    }

    private static float CalculateStereoPan(Vector2 sourcePos, Vector2 listenerPos, float spatialBlend, float listenerRotation)
    {
        if (spatialBlend <= 0.001f)
            return 0f;

        var direction = sourcePos - listenerPos;

        if (direction.LengthSquared() < 0.001f)
            return 0f;

        direction = Vector2.Normalize(direction);

        if (MathF.Abs(listenerRotation) > 0.001f)
            direction = Vector2.Transform(direction, Matrix3x2.CreateRotation(-listenerRotation));

        float pan = direction.X;
        pan *= spatialBlend;

        return Math.Clamp(pan, -1f, 1f);
    }

    private static float CalculateDopplerPitch(
        Vector2 sourcePos, Vector2 listenerPos,
        Vector2 sourceVelocity, Vector2 listenerVelocity,
        float dopplerFactor, float speedOfSound)
    {
        var toSource = sourcePos - listenerPos;
        float distance = toSource.Length();
        if (distance < 0.001f)
            return 1f;

        var direction = toSource / distance;
        float relativeVelocity = Vector2.Dot(listenerVelocity - sourceVelocity, direction);
        float pitch = 1f + relativeVelocity * dopplerFactor / speedOfSound;
        return Math.Clamp(pitch, 0.5f, 2f);
    }

    private sealed class TrackState
    {
        public required Entity Entity;
        public required SoundEffectSourceComponent Component;
        public required string? SoundName;
        public float PitchOffset;
        public float VolumeScale;
        public float FadeGain = 1f;
        public float FadeRate;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        try
        {
            foreach (var tracks in _entityTracks.Values)
            {
                foreach (var track in tracks)
                    _audio.StopTrack(track);
            }

            if (_musicEntity != null)
                _audio.StopMusic();
        }
        catch (ObjectDisposedException)
        {
        }

        _entityTracks.Clear();
        _trackEntities.Clear();
        _previousEnabledState.Clear();
        _previousBusPauseState.Clear();
        _previousSourcePositions.Clear();
        _activeEntities.Clear();
        _individuallyPausedTracks.Clear();
        _hasListenerPreviousPosition = false;
        _musicEntity = null;
    }

    private void AdvanceTrackFades(float deltaTime)
    {
        if (_trackEntities.Count == 0 || deltaTime <= 0f)
            return;

        _fadedOutTracks.Clear();
        foreach (var (track, state) in _trackEntities)
        {
            if (state.FadeRate == 0f)
                continue;

            if (state.Component.IsPaused || _audio.IsBusPaused(state.Component.Bus))
                continue;

            state.FadeGain += state.FadeRate * deltaTime;

            if (state.FadeRate > 0f && state.FadeGain >= 1f)
            {
                state.FadeGain = 1f;
                state.FadeRate = 0f;
            }
            else if (state.FadeRate < 0f && state.FadeGain <= 0f)
            {
                state.FadeGain = 0f;
                _fadedOutTracks.Add(track);
            }
        }

        foreach (var track in _fadedOutTracks)
        {
            _audio.StopTrack(track);
            CleanupTrackState(track);
        }
    }

    private void ProcessSfxStopOldest(Entity entity, SoundEffectSourceComponent sfx)
    {
        if (!sfx.TriggerStopOldest)
            return;

        sfx.TriggerStopOldest = false;

        if (!_entityTracks.TryGetValue(entity, out var tracks) || tracks.Count == 0)
            return;

        var oldest = tracks[0];
        _audio.StopTrack(oldest);
        CleanupTrackState(oldest);
    }
}
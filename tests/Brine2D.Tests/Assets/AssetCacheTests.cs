using Brine2D.Assets;
using Brine2D.Audio;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using System.Collections.Concurrent;

namespace Brine2D.Tests.Assets;

public class AssetCacheTests : IAsyncDisposable
{
    private readonly ITextureLoader _textureLoader = Substitute.For<ITextureLoader>();
    private readonly ISoundLoader _soundLoader = Substitute.For<ISoundLoader>();
    private readonly IMusicLoader _musicLoader = Substitute.For<IMusicLoader>();
    private readonly IFontLoader _fontLoader = Substitute.For<IFontLoader>();
    private readonly AssetCache _sut;

    public AssetCacheTests()
    {
        _sut = new AssetCache(
            NullLogger<AssetCache>.Instance,
            _textureLoader,
            _soundLoader,
            _musicLoader,
            _fontLoader);
    }

    public async ValueTask DisposeAsync() => await _sut.DisposeAsync();

    [Fact]
    public async Task GetOrLoadTextureAsync_ReturnsCachedTexture_OnSecondCall()
    {
        var texture = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(texture));

        var key = RefCountKey.ForTexture("img.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        var first = await _sut.GetOrLoadTextureAsync("img.png");
        _sut.TrackDirectRef(key);
        var second = await _sut.GetOrLoadTextureAsync("img.png");

        Assert.Same(first, second);
        await _textureLoader.Received(1)
            .LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrLoadTextureAsync_DifferentScaleModes_AreSeparateCacheEntries()
    {
        var linear = Substitute.For<ITexture>();
        var nearest = Substitute.For<ITexture>();

        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(linear));
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Nearest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(nearest));

        _sut.TrackDirectRef(RefCountKey.ForTexture("img.png", TextureScaleMode.Linear));
        var a = await _sut.GetOrLoadTextureAsync("img.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(RefCountKey.ForTexture("img.png", TextureScaleMode.Nearest));
        var b = await _sut.GetOrLoadTextureAsync("img.png", TextureScaleMode.Nearest);

        Assert.NotSame(a, b);
    }

    [Fact]
    public async Task ConcurrentLoads_ForSameKey_DeduplicateToOneCall()
    {
        var tcs = new TaskCompletionSource<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var key = RefCountKey.ForTexture("img.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        var task1 = _sut.GetOrLoadTextureAsync("img.png");
        _sut.TrackDirectRef(key);
        var task2 = _sut.GetOrLoadTextureAsync("img.png");

        tcs.SetResult(Substitute.For<ITexture>());

        var result1 = await task1;
        var result2 = await task2;

        Assert.Same(result1, result2);
        await _textureLoader.Received(1)
            .LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrLoadFontAsync_DifferentSizes_AreSeparateCacheEntries()
    {
        var small = Substitute.For<IFont>();
        var large = Substitute.For<IFont>();

        _fontLoader.LoadFontAsync("ui.ttf", 12, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(small));
        _fontLoader.LoadFontAsync("ui.ttf", 32, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(large));

        _sut.TrackDirectRef(RefCountKey.ForFont("ui.ttf", 12));
        var a = await _sut.GetOrLoadFontAsync("ui.ttf", 12);
        _sut.TrackDirectRef(RefCountKey.ForFont("ui.ttf", 32));
        var b = await _sut.GetOrLoadFontAsync("ui.ttf", 32);

        Assert.NotSame(a, b);
    }

    [Theory]
    [InlineData("assets/./images/./player.png", "assets/images/player.png")]
    [InlineData("/assets/img.png", "assets/img.png")]
    [InlineData("///assets/img.png", "assets/img.png")]
    [InlineData("Assets/Player.PNG", "assets/player.png")]
    public void NormalizePath_ProducesCanonicalForm(string input, string expected)
    {
        Assert.Equal(expected, AssetCache.NormalizePath(input));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("../secret.png")]
    [InlineData("../../etc/passwd")]
    [InlineData("a/../../secret.png")]
    [InlineData("a/b/../../../secret.png")]
    [InlineData("assets/old/../images/player.png")]
    public void NormalizePath_RejectsPathTraversal(string path)
    {
        Assert.Throws<ArgumentException>(() => AssetCache.NormalizePath(path));
    }

    [Theory]
    [InlineData("a/..")]
    [InlineData("a/b/../..")]
    [InlineData("./")]
    [InlineData("a/./b/../../..")]
    public void NormalizePath_RejectsEmptyResolvedPath(string path)
    {
        Assert.Throws<ArgumentException>(() => AssetCache.NormalizePath(path));
    }

    [Fact]
    public void TryGetTexture_ReturnsFalse_WhenNotLoaded()
    {
        Assert.False(_sut.TryGetTexture("missing.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task TryGetTexture_ReturnsTrue_AfterLoad()
    {
        var texture = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(texture));

        _sut.TrackDirectRef(RefCountKey.ForTexture("img.png", TextureScaleMode.Linear));
        await _sut.GetOrLoadTextureAsync("img.png");

        Assert.True(_sut.TryGetTexture("img.png", TextureScaleMode.Linear, out var cached));
        Assert.Same(texture, cached);
    }

    [Fact]
    public async Task TryGetFont_DistinguishesBySize()
    {
        var small = Substitute.For<IFont>();
        _fontLoader.LoadFontAsync("ui.ttf", 12, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(small));

        _sut.TrackDirectRef(RefCountKey.ForFont("ui.ttf", 12));
        await _sut.GetOrLoadFontAsync("ui.ttf", 12);

        Assert.True(_sut.TryGetFont("ui.ttf", 12, out var found));
        Assert.Same(small, found);
        Assert.False(_sut.TryGetFont("ui.ttf", 32, out _));
    }

    [Fact]
    public async Task TryGetTexture_ReturnsFalse_AfterUnloadAll()
    {
        var texture = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(texture));

        _sut.TrackDirectRef(RefCountKey.ForTexture("img.png", TextureScaleMode.Linear));
        await _sut.GetOrLoadTextureAsync("img.png");
        _sut.UnloadAll();

        Assert.False(_sut.TryGetTexture("img.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task UnloadAll_FreesAllCachedAssets()
    {
        var texture = Substitute.For<ITexture>();
        var sound = Substitute.For<ISoundEffect>();

        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(texture));
        _soundLoader.LoadSoundAsync("sfx.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sound));

        _sut.TrackDirectRef(RefCountKey.ForTexture("img.png", TextureScaleMode.Linear));
        await _sut.GetOrLoadTextureAsync("img.png");
        _sut.TrackDirectRef(RefCountKey.ForSound("sfx.wav"));
        await _sut.GetOrLoadSoundAsync("sfx.wav");

        _sut.UnloadAll();

        _textureLoader.Received(1).UnloadTexture(texture);
        _soundLoader.Received(1).UnloadSound(sound);
    }

    [Fact]
    public async Task UnloadAll_FreesDirectlyLoadedAssets()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        _sut.TrackDirectRef(RefCountKey.ForTexture("img.png", TextureScaleMode.Linear));
        await _sut.GetOrLoadTextureAsync("img.png");

        _sut.UnloadAll();

        _textureLoader.Received(1).UnloadTexture(tex);
        Assert.False(_sut.TryGetTexture("img.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task UnloadAll_DuringInflight_DisposesOrphanedResource()
    {
        var tcs = new TaskCompletionSource<ITexture>();
        _textureLoader.LoadTextureAsync("slow.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var loadTask = _sut.GetOrLoadTextureAsync("slow.png");

        _sut.UnloadAll();

        var orphan = Substitute.For<ITexture>();
        tcs.SetResult(orphan);

        await Assert.ThrowsAnyAsync<Exception>(() => loadTask);

        _textureLoader.Received(1).UnloadTexture(orphan);
    }

    [Fact]
    public async Task Unload_FreesOnlyManifestAssets()
    {
        var manifestTex = Substitute.For<ITexture>();
        var otherTex = Substitute.For<ITexture>();

        _textureLoader.LoadTextureAsync("a.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(manifestTex));
        _textureLoader.LoadTextureAsync("b.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(otherTex));

        var manifest = new SingleTextureManifest("a.png");
        await _sut.PreloadAsync(manifest);

        _sut.TrackDirectRef(RefCountKey.ForTexture("b.png", TextureScaleMode.Linear));
        await _sut.GetOrLoadTextureAsync("b.png");

        _sut.Unload(manifest);

        _textureLoader.Received(1).UnloadTexture(manifestTex);
        _textureLoader.DidNotReceive().UnloadTexture(otherTex);
    }

    [Fact]
    public async Task Unload_CancelsInflightLoad()
    {
        var loadStarted = new TaskCompletionSource();
        var tcs = new TaskCompletionSource<ITexture>();
        _textureLoader.LoadTextureAsync("slow.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                loadStarted.SetResult();
                return tcs.Task;
            });

        var manifest = new SingleTextureManifest("slow.png");
        var preloadTask = _sut.PreloadAsync(manifest);

        await loadStarted.Task;

        _sut.Unload(manifest);

        tcs.SetResult(Substitute.For<ITexture>());

        await Assert.ThrowsAnyAsync<Exception>(() => preloadTask);
    }

    [Fact]
    public async Task Unload_WithSharedKey_OnlyUnloadsUnreferencedAssets()
    {
        var tex = Substitute.For<ITexture>();
        var sound = Substitute.For<ISoundEffect>();

        _textureLoader.LoadTextureAsync("shared.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));
        _soundLoader.LoadSoundAsync("only-in-m1.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sound));

        var m1 = new ManifestWithUniqueAndShared();
        var m2 = new ManifestWithSharedOnly();

        await _sut.PreloadAsync(m1);
        await _sut.PreloadAsync(m2);

        _sut.Unload(m1);

        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
        Assert.True(_sut.TryGetTexture("shared.png", TextureScaleMode.Linear, out _));

        _soundLoader.Received(1).UnloadSound(sound);
        Assert.False(_sut.TryGetSound("only-in-m1.wav", out _));

        Assert.False(m1.OnlyInM1.IsLoaded);
        Assert.False(m1.Shared.IsLoaded);
        Assert.True(m2.Shared.IsLoaded);
    }

    [Fact]
    public async Task Unload_SameManifestTwice_IsNoOp()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var manifest = new SingleTextureManifest("img.png");
        await _sut.PreloadAsync(manifest);

        _sut.Unload(manifest);
        _sut.Unload(manifest);

        _textureLoader.Received(1).UnloadTexture(tex);
    }

    [Fact]
    public async Task Unload_DuringPreload_PreventsRemainingItemsFromStarting()
    {
        await using var cache = new AssetCache(
            NullLogger<AssetCache>.Instance,
            _textureLoader, _soundLoader, _musicLoader, _fontLoader,
            new AssetOptions { MaxPreloadParallelism = 1 });

        var firstStarted = new TaskCompletionSource();
        var firstGate = new TaskCompletionSource<ITexture>();

        _textureLoader.LoadTextureAsync("first.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                firstStarted.SetResult();
                return firstGate.Task;
            });

        _soundLoader.LoadSoundAsync("second.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ISoundEffect>()));

        var manifest = new BlockingManifest();
        var preloadTask = cache.PreloadAsync(manifest);

        await firstStarted.Task;

        cache.Unload(manifest);
        firstGate.SetResult(Substitute.For<ITexture>());

        await Assert.ThrowsAnyAsync<Exception>(() => preloadTask);

        await _soundLoader.DidNotReceive()
            .LoadSoundAsync("second.wav", Arg.Any<CancellationToken>());

        Assert.False(cache.TryGetTexture("first.png", TextureScaleMode.Linear, out _));
        Assert.False(cache.TryGetSound("second.wav", out _));
    }

    [Fact]
    public async Task PreloadAsync_ReportsProgress()
    {
        _textureLoader.LoadTextureAsync("a.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));
        _soundLoader.LoadSoundAsync("b.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ISoundEffect>()));

        var manifest = new TwoAssetManifest();
        var reports = new ConcurrentBag<AssetLoadProgress>();

        await _sut.PreloadAsync(manifest, new SyncProgress<AssetLoadProgress>(reports.Add));

        Assert.True(reports.Count > 0);
        Assert.Contains(reports, r => r.TotalAssets == 2);
    }

    [Fact]
    public async Task PreloadAsync_WithPartialFailures_ThrowsAggregateException()
    {
        _textureLoader.LoadTextureAsync("good.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));
        _soundLoader.LoadSoundAsync("bad.wav", Arg.Any<CancellationToken>())
            .Returns<ISoundEffect>(_ => throw new FileNotFoundException("bad.wav"));

        var manifest = new GoodAndBadManifest();

        var ex = await Assert.ThrowsAsync<AggregateException>(() => _sut.PreloadAsync(manifest));
        Assert.Single(ex.InnerExceptions);
    }

    [Fact]
    public async Task PreloadAsync_ThenUnload_ThenReload_Works()
    {
        var tex1 = Substitute.For<ITexture>();
        var tex2 = Substitute.For<ITexture>();
        var callCount = 0;

        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++callCount == 1 ? tex1 : tex2));

        var manifest = new SingleTextureManifest("img.png");

        await _sut.PreloadAsync(manifest);
        Assert.Same(tex1, manifest.Tex.Value);

        _sut.Unload(manifest);

        await _sut.PreloadAsync(manifest);
        Assert.Same(tex2, manifest.Tex.Value);
    }

    [Fact]
    public async Task PreloadAsync_SameManifestTwice_DoesNotDoubleRefCount()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var manifest = new SingleTextureManifest("img.png");

        await _sut.PreloadAsync(manifest);
        await _sut.PreloadAsync(manifest);

        _sut.Unload(manifest);

        _textureLoader.Received(1).UnloadTexture(tex);
        Assert.False(_sut.TryGetTexture("img.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task PreloadAsync_RetryWithoutUnload_ResolvesRemainingRefs()
    {
        _textureLoader.LoadTextureAsync("stable.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));

        var callCount = 0;
        _soundLoader.LoadSoundAsync("flaky.wav", Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (++callCount == 1)
                    throw new IOException("transient");
                return Task.FromResult(Substitute.For<ISoundEffect>());
            });

        var manifest = new FlakyManifest();

        await Assert.ThrowsAsync<AggregateException>(() => _sut.PreloadAsync(manifest));
        Assert.True(manifest.Stable.IsLoaded);
        Assert.False(manifest.Flaky.IsLoaded);

        await _sut.PreloadAsync(manifest);
        Assert.True(manifest.Stable.IsLoaded);
        Assert.True(manifest.Flaky.IsLoaded);
    }

    [Fact]
    public async Task ReleaseDirectRef_FreesTrackedTexture()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var key = RefCountKey.ForTexture("img.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadTextureAsync("img.png");
        var freed = _sut.ReleaseDirectRef(key);

        Assert.True(freed);
        _textureLoader.Received(1).UnloadTexture(tex);
        Assert.False(_sut.TryGetTexture("img.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task ReleaseDirectRef_FreesTrackedSound()
    {
        var sound = Substitute.For<ISoundEffect>();
        _soundLoader.LoadSoundAsync("sfx.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sound));

        var key = RefCountKey.ForSound("sfx.wav");
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadSoundAsync("sfx.wav");
        var freed = _sut.ReleaseDirectRef(key);

        Assert.True(freed);
        _soundLoader.Received(1).UnloadSound(sound);
        Assert.False(_sut.TryGetSound("sfx.wav", out _));
    }

    [Fact]
    public async Task ReleaseDirectRef_FreesTrackedMusic()
    {
        var music = Substitute.For<IMusic>();
        _musicLoader.LoadMusicAsync("track.ogg", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(music));

        var key = RefCountKey.ForMusic("track.ogg");
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadMusicAsync("track.ogg");
        var freed = _sut.ReleaseDirectRef(key);

        Assert.True(freed);
        _musicLoader.Received(1).UnloadMusic(music);
        Assert.False(_sut.TryGetMusic("track.ogg", out _));
    }

    [Fact]
    public async Task ReleaseDirectRef_FreesTrackedFont()
    {
        var font = Substitute.For<IFont>();
        _fontLoader.LoadFontAsync("ui.ttf", 16, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(font));

        var key = RefCountKey.ForFont("ui.ttf", 16);
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadFontAsync("ui.ttf", 16);
        var freed = _sut.ReleaseDirectRef(key);

        Assert.True(freed);
        _fontLoader.Received(1).UnloadFont(font);
        Assert.False(_sut.TryGetFont("ui.ttf", 16, out _));
    }

    [Fact]
    public void ReleaseDirectRef_ReturnsFalse_WhenNeverTracked()
    {
        Assert.False(_sut.ReleaseDirectRef(RefCountKey.ForTexture("missing.png", TextureScaleMode.Linear)));
    }

    [Fact]
    public async Task ReleaseDirectRef_Twice_SecondCallIsNoOp()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var key = RefCountKey.ForTexture("img.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadTextureAsync("img.png");

        Assert.True(_sut.ReleaseDirectRef(key));
        Assert.False(_sut.ReleaseDirectRef(key));

        _textureLoader.Received(1).UnloadTexture(tex);
    }

    [Fact]
    public async Task ReleaseDirectRef_ReturnsFalse_WhenManifestStillHoldsRef()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("shared.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var key = RefCountKey.ForTexture("shared.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadTextureAsync("shared.png");
        var manifest = new SingleTextureManifest("shared.png");
        await _sut.PreloadAsync(manifest);

        var freed = _sut.ReleaseDirectRef(key);

        Assert.False(freed);
        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
        Assert.True(_sut.TryGetTexture("shared.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task ReleaseDirectRef_ReturnsFalse_WhenOnlyLoadedViaManifest()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("manifest-only.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var manifest = new SingleTextureManifest("manifest-only.png");
        await _sut.PreloadAsync(manifest);

        var freed = _sut.ReleaseDirectRef(RefCountKey.ForTexture("manifest-only.png", TextureScaleMode.Linear));

        Assert.False(freed);
        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
        Assert.True(_sut.TryGetTexture("manifest-only.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task ReleaseDirectRef_ThenReload_CreatesNewRef()
    {
        var tex1 = Substitute.For<ITexture>();
        var tex2 = Substitute.For<ITexture>();
        var callCount = 0;

        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(++callCount == 1 ? tex1 : tex2));

        var key = RefCountKey.ForTexture("img.png", TextureScaleMode.Linear);

        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadTextureAsync("img.png");
        _sut.ReleaseDirectRef(key);
        _textureLoader.Received(1).UnloadTexture(tex1);

        _sut.TrackDirectRef(key);
        var result = await _sut.GetOrLoadTextureAsync("img.png");
        Assert.Same(tex2, result);

        Assert.True(_sut.ReleaseDirectRef(key));
        _textureLoader.Received(1).UnloadTexture(tex2);
    }

    [Fact]
    public async Task ReleaseDirectRef_DistinguishesByScaleMode()
    {
        var linear = Substitute.For<ITexture>();
        var nearest = Substitute.For<ITexture>();

        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(linear));
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Nearest, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(nearest));

        var linearKey = RefCountKey.ForTexture("img.png", TextureScaleMode.Linear);
        var nearestKey = RefCountKey.ForTexture("img.png", TextureScaleMode.Nearest);

        _sut.TrackDirectRef(linearKey);
        _sut.TrackDirectRef(nearestKey);
        await _sut.GetOrLoadTextureAsync("img.png", TextureScaleMode.Linear);
        await _sut.GetOrLoadTextureAsync("img.png", TextureScaleMode.Nearest);

        _sut.ReleaseDirectRef(linearKey);

        _textureLoader.Received(1).UnloadTexture(linear);
        _textureLoader.DidNotReceive().UnloadTexture(nearest);
        Assert.True(_sut.TryGetTexture("img.png", TextureScaleMode.Nearest, out _));
    }

    [Fact]
    public async Task ReleaseDirectRef_DistinguishesByFontSize()
    {
        var small = Substitute.For<IFont>();
        var large = Substitute.For<IFont>();

        _fontLoader.LoadFontAsync("ui.ttf", 12, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(small));
        _fontLoader.LoadFontAsync("ui.ttf", 32, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(large));

        var smallKey = RefCountKey.ForFont("ui.ttf", 12);
        var largeKey = RefCountKey.ForFont("ui.ttf", 32);

        _sut.TrackDirectRef(smallKey);
        _sut.TrackDirectRef(largeKey);
        await _sut.GetOrLoadFontAsync("ui.ttf", 12);
        await _sut.GetOrLoadFontAsync("ui.ttf", 32);

        _sut.ReleaseDirectRef(smallKey);

        _fontLoader.Received(1).UnloadFont(small);
        _fontLoader.DidNotReceive().UnloadFont(large);
        Assert.True(_sut.TryGetFont("ui.ttf", 32, out _));
    }

    [Fact]
    public async Task ReleaseDirectRef_RequiresOneReleasePerTrack()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var key = RefCountKey.ForTexture("img.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadTextureAsync("img.png");

        Assert.False(_sut.ReleaseDirectRef(key));
        Assert.True(_sut.ReleaseDirectRef(key));

        _textureLoader.Received(1).UnloadTexture(tex);
        Assert.False(_sut.TryGetTexture("img.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task DirectRef_SurvivesManifestUnload_WhenTrackedFirst()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("shared.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var key = RefCountKey.ForTexture("shared.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadTextureAsync("shared.png");

        var manifest = new SingleTextureManifest("shared.png");
        await _sut.PreloadAsync(manifest);

        _sut.Unload(manifest);

        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
        Assert.True(_sut.TryGetTexture("shared.png", TextureScaleMode.Linear, out var cached));
        Assert.Same(tex, cached);
    }

    [Fact]
    public async Task DirectRef_SurvivesManifestUnload_WhenTrackedAfterManifest()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("shared.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var manifest = new SingleTextureManifest("shared.png");
        await _sut.PreloadAsync(manifest);

        var key = RefCountKey.ForTexture("shared.png", TextureScaleMode.Linear);
        _sut.TrackDirectRef(key);
        await _sut.GetOrLoadTextureAsync("shared.png");

        _sut.Unload(manifest);

        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
        Assert.True(_sut.TryGetTexture("shared.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public void AssetRef_TryGetValue_ReturnsFalse_BeforeLoad()
    {
        var manifest = new SingleTextureManifest("img.png");

        Assert.False(manifest.Tex.TryGetValue(out _));
    }

    [Fact]
    public async Task AssetRef_TryGetValue_ReturnsTrue_AfterLoad()
    {
        var texture = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(texture));

        var manifest = new SingleTextureManifest("img.png");
        await _sut.PreloadAsync(manifest);

        Assert.True(manifest.Tex.TryGetValue(out var value));
        Assert.Same(texture, value);
    }

    [Fact]
    public async Task AssetRef_TryGetValue_ReturnsFalse_AfterUnload()
    {
        var texture = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(texture));

        var manifest = new SingleTextureManifest("img.png");
        await _sut.PreloadAsync(manifest);
        _sut.Unload(manifest);

        Assert.False(manifest.Tex.TryGetValue(out _));
    }

    [Fact]
    public void ManifestInConstructor_RejectsPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => new SingleTextureManifest("../../etc/passwd"));
    }

    [Fact]
    public async Task ManifestWithProperty_ThrowsOnDiscovery()
    {
        var manifest = new PropertyManifest();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PreloadAsync(manifest));
    }

    [Fact]
    public async Task ManifestWithMutableField_ThrowsOnDiscovery()
    {
        var manifest = new MutableFieldManifest();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.PreloadAsync(manifest));
    }

    [Fact]
    public async Task Dispose_PreventsSubsequentLoads()
    {
        await _sut.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => _sut.GetOrLoadTextureAsync("img.png"));
    }

    [Fact]
    public async Task DisposeAsync_WithInflightLoad_DrainsAndUnloadsOrphan()
    {
        var tcs = new TaskCompletionSource<ITexture>();
        _textureLoader.LoadTextureAsync("slow.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        _ = _sut.GetOrLoadTextureAsync("slow.png");

        var disposeTask = _sut.DisposeAsync();

        var orphan = Substitute.For<ITexture>();
        tcs.SetResult(orphan);

        await disposeTask;

        _textureLoader.Received(1).UnloadTexture(orphan);
    }

    [Fact]
    public async Task PreloadAsync_ReportsProgressRatio_ReachesOneOnCompletion()
    {
        _textureLoader.LoadTextureAsync("a.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));
        _soundLoader.LoadSoundAsync("b.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ISoundEffect>()));

        var manifest = new TwoAssetManifest();
        var reports = new ConcurrentBag<AssetLoadProgress>();

        await _sut.PreloadAsync(manifest, new SyncProgress<AssetLoadProgress>(reports.Add));

        var final = reports.First(r => r.SucceededAssets + r.FailedAssets == r.TotalAssets);
        Assert.Equal(1f, final.ProgressRatio);
        Assert.Equal(1f, final.SuccessRatio);
    }

    [Fact]
    public async Task PreloadAsync_WithFailure_ProgressRatioExceedsSuccessRatio()
    {
        _textureLoader.LoadTextureAsync("good.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));
        _soundLoader.LoadSoundAsync("bad.wav", Arg.Any<CancellationToken>())
            .Returns<ISoundEffect>(_ => throw new FileNotFoundException("bad.wav"));

        var manifest = new GoodAndBadManifest();
        var reports = new ConcurrentBag<AssetLoadProgress>();

        await Assert.ThrowsAsync<AggregateException>(() =>
            _sut.PreloadAsync(manifest, new SyncProgress<AssetLoadProgress>(reports.Add)));

        var final = reports.First(r => r.SucceededAssets + r.FailedAssets == r.TotalAssets);
        Assert.Equal(1f, final.ProgressRatio);
        Assert.Equal(0.5f, final.SuccessRatio);
    }

    [Fact]
    public async Task PreloadAsync_ConcurrentWithUnload_CancelsPreload()
    {
        var tcs = new TaskCompletionSource<ITexture>();
        _textureLoader.LoadTextureAsync("slow.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var manifest = new SingleTextureManifest("slow.png");
        var preloadTask = _sut.PreloadAsync(manifest);

        _sut.Unload(manifest);

        tcs.SetResult(Substitute.For<ITexture>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => preloadTask);
    }

    [Fact]
    public async Task DisposeAsync_DuringActivePreload_CancelsAndCompletes()
    {
        var tcs = new TaskCompletionSource<ITexture>();
        _textureLoader.LoadTextureAsync("hang.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ct = ci.ArgAt<CancellationToken>(2);
                return Task.Delay(Timeout.Infinite, ct).ContinueWith(_ => tcs.Task.Result, TaskScheduler.Default);
            });

        var manifest = new SingleTextureManifest("hang.png");
        var preloadTask = _sut.PreloadAsync(manifest);

        await _sut.DisposeAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => preloadTask);
    }

    private sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }

    private sealed class SingleTextureManifest(string path) : AssetManifest
    {
        public readonly AssetRef<ITexture> Tex = Texture(path);
    }

    private sealed class TwoAssetManifest : AssetManifest
    {
        public readonly AssetRef<ITexture> Tex = Texture("a.png");
        public readonly AssetRef<ISoundEffect> Sfx = Sound("b.wav");
    }

    private sealed class GoodAndBadManifest : AssetManifest
    {
        public readonly AssetRef<ITexture> Good = Texture("good.png");
        public readonly AssetRef<ISoundEffect> Bad = Sound("bad.wav");
    }

    private sealed class PropertyManifest : AssetManifest
    {
        public AssetRef<ITexture> Oops => Texture("nope.png");
    }

    private sealed class MutableFieldManifest : AssetManifest
    {
        public AssetRef<ITexture> Oops = Texture("nope.png");
    }

    private sealed class ManifestWithUniqueAndShared : AssetManifest
    {
        public readonly AssetRef<ISoundEffect> OnlyInM1 = Sound("only-in-m1.wav");
        public readonly AssetRef<ITexture> Shared = Texture("shared.png");
    }

    private sealed class ManifestWithSharedOnly : AssetManifest
    {
        public readonly AssetRef<ITexture> Shared = Texture("shared.png");
    }

    private sealed class FlakyManifest : AssetManifest
    {
        public readonly AssetRef<ITexture> Stable = Texture("stable.png");
        public readonly AssetRef<ISoundEffect> Flaky = Sound("flaky.wav");
    }

    private sealed class BlockingManifest : AssetManifest
    {
        public readonly AssetRef<ITexture> First = Texture("first.png");
        public readonly AssetRef<ISoundEffect> Second = Sound("second.wav");
    }
}
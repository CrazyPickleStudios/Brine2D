using Brine2D.Assets;
using Brine2D.Audio;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Brine2D.Tests.Assets;

public class AssetLoaderTests : IAsyncDisposable
{
    private readonly ITextureLoader _textureLoader = Substitute.For<ITextureLoader>();
    private readonly ISoundLoader _soundLoader = Substitute.For<ISoundLoader>();
    private readonly IMusicLoader _musicLoader = Substitute.For<IMusicLoader>();
    private readonly IFontLoader _fontLoader = Substitute.For<IFontLoader>();
    private readonly AssetCache _cache;

    public AssetLoaderTests()
    {
        _cache = new AssetCache(
            NullLogger<AssetCache>.Instance,
            _textureLoader,
            _soundLoader,
            _musicLoader,
            _fontLoader);
    }

    public async ValueTask DisposeAsync() => await _cache.DisposeAsync();

    private AssetLoader CreateScope() => new(_cache, NullLogger<AssetLoader>.Instance);

    [Fact]
    public async Task Dispose_ReleasesDirectlyLoadedTexture()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var scope = CreateScope();
        await scope.GetOrLoadTextureAsync("img.png");
        scope.Dispose();

        _textureLoader.Received(1).UnloadTexture(tex);
        Assert.False(_cache.TryGetTexture("img.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task Dispose_ReleasesDirectlyLoadedSound()
    {
        var sound = Substitute.For<ISoundEffect>();
        _soundLoader.LoadSoundAsync("sfx.wav", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(sound));

        var scope = CreateScope();
        await scope.GetOrLoadSoundAsync("sfx.wav");
        scope.Dispose();

        _soundLoader.Received(1).UnloadSound(sound);
    }

    [Fact]
    public async Task Dispose_ReleasesDirectlyLoadedMusic()
    {
        var music = Substitute.For<IMusic>();
        _musicLoader.LoadMusicAsync("track.ogg", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(music));

        var scope = CreateScope();
        await scope.GetOrLoadMusicAsync("track.ogg");
        scope.Dispose();

        _musicLoader.Received(1).UnloadMusic(music);
    }

    [Fact]
    public async Task Dispose_ReleasesDirectlyLoadedFont()
    {
        var font = Substitute.For<IFont>();
        _fontLoader.LoadFontAsync("ui.ttf", 16, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(font));

        var scope = CreateScope();
        await scope.GetOrLoadFontAsync("ui.ttf", 16);
        scope.Dispose();

        _fontLoader.Received(1).UnloadFont(font);
    }

    [Fact]
    public async Task Dispose_ReleasesManifestAssets()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var scope = CreateScope();
        var manifest = new SingleTextureManifest("img.png");
        await scope.PreloadAsync(manifest);
        scope.Dispose();

        _textureLoader.Received(1).UnloadTexture(tex);
        Assert.False(manifest.Tex.IsLoaded);
    }

    [Fact]
    public async Task ManualRelease_ThenDispose_DoesNotDoubleRelease()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var scope = CreateScope();
        await scope.GetOrLoadTextureAsync("img.png");
        scope.ReleaseTexture("img.png");
        scope.Dispose();

        _textureLoader.Received(1).UnloadTexture(tex);
    }

    [Fact]
    public async Task TwoScopes_SharedAsset_SurvivesFirstDispose()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("shared.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var scope1 = CreateScope();
        var scope2 = CreateScope();

        await scope1.GetOrLoadTextureAsync("shared.png");
        await scope2.GetOrLoadTextureAsync("shared.png");

        scope1.Dispose();

        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
        Assert.True(_cache.TryGetTexture("shared.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task TwoScopes_SharedAsset_FreedAfterBothDispose()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("shared.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var scope1 = CreateScope();
        var scope2 = CreateScope();

        await scope1.GetOrLoadTextureAsync("shared.png");
        await scope2.GetOrLoadTextureAsync("shared.png");

        scope1.Dispose();
        scope2.Dispose();

        _textureLoader.Received(1).UnloadTexture(tex);
    }

    [Fact]
    public async Task SceneTransition_SharedAssetsStay_UniqueAssetsFreed()
    {
        var player = Substitute.For<ITexture>();
        var enemy = Substitute.For<ITexture>();
        var coin = Substitute.For<ITexture>();

        _textureLoader.LoadTextureAsync("player.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(player));
        _textureLoader.LoadTextureAsync("enemy.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(enemy));
        _textureLoader.LoadTextureAsync("coin.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(coin));

        var scopeA = CreateScope();
        var manifestA = new TwoTextureManifest("player.png", "enemy.png");
        await scopeA.PreloadAsync(manifestA);

        var scopeB = CreateScope();
        var manifestB = new TwoTextureManifest("player.png", "coin.png");
        await scopeB.PreloadAsync(manifestB);

        scopeA.Dispose();

        _textureLoader.DidNotReceive().UnloadTexture(player);
        _textureLoader.Received(1).UnloadTexture(enemy);
        _textureLoader.DidNotReceive().UnloadTexture(coin);

        Assert.True(_cache.TryGetTexture("player.png", TextureScaleMode.Linear, out _));
        Assert.False(_cache.TryGetTexture("enemy.png", TextureScaleMode.Linear, out _));
        Assert.True(_cache.TryGetTexture("coin.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public async Task DirectLoad_SurvivesManifestScopeDispose()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("shared.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        var scopeA = CreateScope();
        var manifest = new SingleTextureManifest("shared.png");
        await scopeA.PreloadAsync(manifest);

        var scopeB = CreateScope();
        await scopeB.GetOrLoadTextureAsync("shared.png");

        scopeA.Dispose();

        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
        Assert.True(_cache.TryGetTexture("shared.png", TextureScaleMode.Linear, out _));

        scopeB.Dispose();

        _textureLoader.Received(1).UnloadTexture(tex);
    }

    [Fact]
    public async Task UnloadAll_ReleasesOnlyScopedAssets()
    {
        var scopedTex = Substitute.For<ITexture>();
        var otherTex = Substitute.For<ITexture>();

        _textureLoader.LoadTextureAsync("scoped.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(scopedTex));
        _textureLoader.LoadTextureAsync("other.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(otherTex));

        using var scope1 = CreateScope();
        using var scope2 = CreateScope();

        await scope1.GetOrLoadTextureAsync("scoped.png");
        await scope2.GetOrLoadTextureAsync("other.png");

        scope1.UnloadAll();

        _textureLoader.Received(1).UnloadTexture(scopedTex);
        _textureLoader.DidNotReceive().UnloadTexture(otherTex);
        Assert.True(_cache.TryGetTexture("other.png", TextureScaleMode.Linear, out _));
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var scope = CreateScope();
        scope.Dispose();
        scope.Dispose();
    }

    [Fact]
    public async Task Dispose_ThenLoad_ThrowsObjectDisposedException()
    {
        var scope = CreateScope();
        scope.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => scope.GetOrLoadTextureAsync("img.png"));
    }

    [Fact]
    public void Dispose_ThenRelease_ThrowsObjectDisposedException()
    {
        var scope = CreateScope();
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.ReleaseTexture("img.png"));
    }

    [Fact]
    public void Dispose_ThenUnloadAll_ThrowsObjectDisposedException()
    {
        var scope = CreateScope();
        scope.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scope.UnloadAll());
    }

    [Fact]
    public async Task MultipleLoads_SameKey_RequiresMatchingReleaseCount()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("img.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(tex));

        using var scope = CreateScope();
        await scope.GetOrLoadTextureAsync("img.png");
        await scope.GetOrLoadTextureAsync("img.png");

        Assert.False(scope.ReleaseTexture("img.png"));
        Assert.True(scope.ReleaseTexture("img.png"));

        _textureLoader.Received(1).UnloadTexture(tex);
    }

    [Fact]
    public async Task FailedLoad_RollsBackRefCount_NothingUnloadedOnDispose()
    {
        _textureLoader.LoadTextureAsync("bad.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ITexture>(new FileNotFoundException()));

        var scope = CreateScope();

        await Assert.ThrowsAsync<FileNotFoundException>(
            () => scope.GetOrLoadTextureAsync("bad.png"));

        scope.Dispose();

        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
    }

    [Fact]
    public async Task FailedLoad_ThenRetry_RequiresOnlyOneRelease()
    {
        var tex = Substitute.For<ITexture>();
        _textureLoader.LoadTextureAsync("flaky.png", TextureScaleMode.Linear, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromException<ITexture>(new IOException()),
                Task.FromResult(tex));

        using var scope = CreateScope();

        await Assert.ThrowsAsync<IOException>(
            () => scope.GetOrLoadTextureAsync("flaky.png"));

        var result = await scope.GetOrLoadTextureAsync("flaky.png");
        Assert.Same(tex, result);

        Assert.True(scope.ReleaseTexture("flaky.png"));
        _textureLoader.Received(1).UnloadTexture(tex);
    }

    [Fact]
    public async Task PreloadAsync_Cancelled_RollsBackManifest()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var scope = CreateScope();
        var manifest = new SingleTextureManifest("img.png");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => scope.PreloadAsync(manifest, cancellationToken: cts.Token));

        Assert.False(manifest.Tex.IsLoaded);
        Assert.False(_cache.TryGetTexture("img.png", TextureScaleMode.Linear, out _));

        scope.Dispose();
        _textureLoader.DidNotReceive().UnloadTexture(Arg.Any<ITexture>());
    }

    private sealed class SingleTextureManifest(string path) : AssetManifest
    {
        public readonly AssetRef<ITexture> Tex = Texture(path);
    }

    private sealed class TwoTextureManifest(string pathA, string pathB) : AssetManifest
    {
        public readonly AssetRef<ITexture> A = Texture(pathA);
        public readonly AssetRef<ITexture> B = Texture(pathB);
    }
}
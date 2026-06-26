using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectAscension.Api.Services;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Tests;

public class LlmSkillComposerTests
{
    /// <summary>An IChatClient whose reply (or behavior) is supplied per test.</summary>
    private sealed class FakeChatClient : IChatClient
    {
        private readonly Func<CancellationToken, Task<string>> _respond;
        public FakeChatClient(Func<CancellationToken, Task<string>> respond) => _respond = respond;

        public async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => new ChatResponse(new ChatMessage(ChatRole.Assistant, await _respond(cancellationToken)));

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;
        public void Dispose() { }
    }

    private static LlmSkillComposer Composer(Func<CancellationToken, Task<string>> respond, TimeSpan? timeout = null)
        => new(new FakeChatClient(respond),
            new LlmComposerOptions { Timeout = timeout ?? TimeSpan.FromSeconds(30) },
            NullLogger<LlmSkillComposer>.Instance);

    private static CompositionRequest Request(int budget = 30)
        => new("theme", new[] { "arcane" }, PrimitiveKind.Projectile, new PowerBudget(budget));

    [Fact]
    public async Task GoodJson_ReturnsValidPackedSkill()
    {
        const string json =
            """{"name":"Arc Bolt","description":"d","primitives":[{"kind":"Projectile","magnitude":2},{"kind":"Homing","magnitude":1}]}""";
        var composer = Composer(_ => Task.FromResult(json));

        var skill = await composer.ComposeAsync(Request(30));

        Assert.Equal("Arc Bolt", skill.Name);
        Assert.NotEmpty(skill.Primitives);
        Assert.True(CompositionValidator.Validate(skill, new PowerBudget(30)).IsValid);
    }

    [Fact]
    public async Task MalformedJson_ReturnsInvalid()
    {
        var composer = Composer(_ => Task.FromResult("not json at all"));
        var skill = await composer.ComposeAsync(Request());
        Assert.Empty(skill.Primitives); // invalid sentinel → pipeline retries
    }

    [Fact]
    public async Task CallThrows_ReturnsInvalid()
    {
        var composer = Composer(_ => throw new InvalidOperationException("boom"));
        var skill = await composer.ComposeAsync(Request());
        Assert.Empty(skill.Primitives);
    }

    [Fact]
    public async Task Timeout_ReturnsInvalid()
    {
        var composer = Composer(
            async ct => { await Task.Delay(TimeSpan.FromSeconds(5), ct); return "{}"; },
            timeout: TimeSpan.FromMilliseconds(50));

        var skill = await composer.ComposeAsync(Request());
        Assert.Empty(skill.Primitives); // timed out → deferred
    }

    [Fact]
    public async Task ExternalCancellation_Propagates()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var composer = Composer(async ct => { await Task.Delay(TimeSpan.FromSeconds(5), ct); return "{}"; });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => composer.ComposeAsync(Request(), cts.Token));
    }
}

using Microsoft.AspNetCore.Mvc;
using ProjectAscension.Api.Controllers;
using ProjectAscension.Api.Services;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Shared;

namespace ProjectAscension.Api.Tests;

/// <summary>The HTTP boundary for the licensing 409 (the playtest bug's symptom): a re-sell attempt
/// on already-licensed knowledge must surface as an actual 409 Conflict, not a 500 or a silent
/// success — the client's error handling (CatalogApiClient.ParseErrorMessage) depends on it.</summary>
public class KnowledgeControllerTests
{
    private sealed class FakeKnowledgeService : IKnowledgeService
    {
        public Result<PlayerStateResponse> LicenseResult { get; set; } = Result<PlayerStateResponse>.Fail(Error.Conflict);

        public Task<Result<IReadOnlyList<KnowledgeResponse>>> GetByOwnerAsync(Guid ownerActorId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task<Result<PlayerStateResponse>> LicenseAsync(LicenseKnowledgeRequest request, CancellationToken ct = default)
            => Task.FromResult(LicenseResult);
    }

    [Fact]
    public async Task License_AlreadyLicensed_Returns409Conflict()
    {
        var service = new FakeKnowledgeService(); // defaults to CONFLICT, as a real second attempt would
        var controller = new KnowledgeController(service);

        var result = await controller.License(new LicenseKnowledgeRequest(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task License_Succeeds_Returns200WithPlayerState()
    {
        var state = new PlayerStateResponse(120, 5, Array.Empty<ResourceCount>(), Array.Empty<string>());
        var service = new FakeKnowledgeService { LicenseResult = Result<PlayerStateResponse>.Ok(state) };
        var controller = new KnowledgeController(service);

        var result = await controller.License(new LicenseKnowledgeRequest(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(state, ok.Value);
    }
}

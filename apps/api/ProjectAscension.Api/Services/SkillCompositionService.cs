using System.Text.Json;
using ProjectAscension.Contracts.Requests;
using ProjectAscension.Contracts.Responses;
using ProjectAscension.Domain.Entities;
using ProjectAscension.Domain.Enums;
using ProjectAscension.Domain.Interfaces;
using ProjectAscension.SkillForge;

namespace ProjectAscension.Api.Services;

public class SkillCompositionService : ISkillCompositionService
{
    private const int MaxComposeAttempts = 3;

    private readonly IDiscoveryRepository _discoveries;
    private readonly IDiscoverySkillRepository _skills;
    private readonly ISkillComposer _composer;

    public SkillCompositionService(
        IDiscoveryRepository discoveries,
        IDiscoverySkillRepository skills,
        ISkillComposer composer)
    {
        _discoveries = discoveries;
        _skills = skills;
        _composer = composer;
    }

    public async Task<Guid> TriggerAsync(TriggerDiscoveryRequest request, CancellationToken ct = default)
    {
        // Rule engine fixes the fact instantly (ADR 0002): who/where/when, deterministic.
        var discovery = new Discovery
        {
            Id = Guid.NewGuid(),
            Type = request.Type,
            DiscovererActorId = request.ActorId,
            RegionId = request.RegionId,
            Title = string.IsNullOrWhiteSpace(request.Theme) ? "Discovery" : request.Theme,
            Description = string.Empty,
            DiscoveredAt = DateTime.UtcNow,
        };
        await _discoveries.AddAsync(discovery, ct);

        // Content starts Pending; the AI fills it asynchronously.
        var skill = new DiscoverySkill
        {
            Id = Guid.NewGuid(),
            DiscoveryId = discovery.Id,
            Status = DiscoveryContentStatus.Pending,
            Theme = request.Theme,
            ContextTagsJson = JsonSerializer.Serialize(request.ContextTags),
            PrimaryBehavior = request.PrimaryBehavior,
            PowerBudget = request.PowerBudget,
            CreatedAt = DateTime.UtcNow,
        };
        await _skills.AddAsync(skill, ct);
        return discovery.Id;
    }

    public async Task ComposePendingAsync(int batchSize, CancellationToken ct = default)
    {
        var pending = await _skills.GetPendingAsync(batchSize, ct);
        foreach (var skill in pending)
        {
            if (!TryBuildRequest(skill, out var request))
            {
                // Malformed seed — defer (count the attempt). No fallback skill.
                skill.Attempts++;
                await _skills.UpdateAsync(skill, ct);
                continue;
            }

            var outcome = await CompositionPipeline.ForgeAsync(request, _composer, MaxComposeAttempts, ct);
            skill.Attempts += outcome.Attempts;

            if (outcome.Forged && outcome.Skill is not null)
            {
                skill.Name = outcome.Skill.Name;
                skill.Description = outcome.Skill.Description;
                skill.PrimitivesJson = JsonSerializer.Serialize(outcome.Skill.Primitives);
                skill.PowerCost = outcome.LastValidation.TotalCost;
                skill.Status = DiscoveryContentStatus.Ready;
                skill.ComposedAt = DateTime.UtcNow;
            }
            // else: leave Pending — retried on a later pass (defer, no fallback).

            await _skills.UpdateAsync(skill, ct);
        }
    }

    public async Task<DiscoverySkillResponse?> GetByDiscoveryAsync(Guid discoveryId, CancellationToken ct = default)
    {
        var skill = await _skills.GetByDiscoveryIdAsync(discoveryId, ct);
        if (skill is null) return null;

        var primitives = skill.PrimitivesJson is null
            ? new List<string>()
            : DescribePrimitives(skill.PrimitivesJson);

        return new DiscoverySkillResponse(
            skill.DiscoveryId, skill.Status, skill.Name, skill.Description, skill.PowerCost, primitives);
    }

    private static bool TryBuildRequest(DiscoverySkill skill, out CompositionRequest request)
    {
        request = default!;
        if (!Enum.TryParse<PrimitiveKind>(skill.PrimaryBehavior, ignoreCase: true, out var primary))
            return false;

        List<string>? tags;
        try
        {
            tags = JsonSerializer.Deserialize<List<string>>(skill.ContextTagsJson);
        }
        catch (JsonException)
        {
            return false;
        }

        request = new CompositionRequest(skill.Theme, tags ?? new List<string>(), primary, new PowerBudget(skill.PowerBudget));
        return true;
    }

    private static IReadOnlyList<string> DescribePrimitives(string json)
    {
        try
        {
            var primitives = JsonSerializer.Deserialize<List<ComposedPrimitive>>(json) ?? new List<ComposedPrimitive>();
            return primitives.Select(p => $"{p.Kind} x{p.Magnitude}").ToList();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}

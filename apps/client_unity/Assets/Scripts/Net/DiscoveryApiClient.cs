using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ProjectAscension.Net
{
    // DTOs mirror the server contracts (ProjectAscension.Contracts). JsonUtility needs
    // [Serializable] types with public fields and JSON names matching the wire format
    // (the API uses camelCase + JsonStringEnumConverter, so enums are strings).

    [Serializable]
    public sealed class BehaviorCountDto
    {
        public string behavior;
        public int count;
    }

    [Serializable]
    public sealed class EvaluateRequestDto
    {
        public string actorId;
        public string regionId;
        public string type; // DiscoveryType name, e.g. "Skill"
        public string theme;
        public string[] contextTags;
        public string primaryBehavior; // PrimitiveKind name
        public BehaviorCountDto[] behaviors;
        public int persistence;
    }

    [Serializable]
    public sealed class EvaluateResponseDto
    {
        public bool fired;
        public int score;
        public string discoveryId;
    }

    [Serializable]
    public sealed class SkillResponseDto
    {
        public string discoveryId;
        public string status; // "Pending" | "Ready"
        public string name;
        public string description;
        public int powerCost;
        public string[] primitives; // e.g. "Projectile x2 r1"
    }

    /// <summary>
    /// Thin UnityWebRequest client for the discovery API. Coroutine-based so callers
    /// can drive it from MonoBehaviours. Logic stays server-side / in GameSimulation;
    /// this is just IO. NOTE: the /evaluate path is slice scaffolding (ADR 0004) — in
    /// the MMO the server observes its own simulation and the client never POSTs
    /// behavior. GetSkill (reading frozen discovered content) stays valid either way.
    /// </summary>
    public sealed class DiscoveryApiClient
    {
        private readonly string _baseUrl;

        public DiscoveryApiClient(string baseUrl) => _baseUrl = baseUrl?.TrimEnd('/');

        public IEnumerator Evaluate(EvaluateRequestDto request, Action<EvaluateResponseDto> onResult)
        {
            yield return PostJson(
                $"{_baseUrl}/api/discoveries/evaluate",
                JsonUtility.ToJson(request),
                json => onResult?.Invoke(JsonUtility.FromJson<EvaluateResponseDto>(json)));
        }

        public IEnumerator GetSkill(string discoveryId, Action<SkillResponseDto> onResult)
        {
            yield return GetJson(
                $"{_baseUrl}/api/discoveries/{discoveryId}/skill",
                json => onResult?.Invoke(JsonUtility.FromJson<SkillResponseDto>(json)));
        }

        private static IEnumerator PostJson(string url, string body, Action<string> onOk)
        {
            using var req = new UnityWebRequest(url, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
            };
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onOk?.Invoke(req.downloadHandler.text);
            else
                Debug.LogWarning($"[Discovery] POST {url} failed: {req.error}");
        }

        private static IEnumerator GetJson(string url, Action<string> onOk)
        {
            using var req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
                onOk?.Invoke(req.downloadHandler.text);
            else
                Debug.LogWarning($"[Discovery] GET {url} failed: {req.error}");
        }
    }
}

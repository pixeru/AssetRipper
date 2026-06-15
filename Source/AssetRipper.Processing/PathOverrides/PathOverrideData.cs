using System.Text.Json;
using System.Text.Json.Serialization;

namespace AssetRipper.Processing.PathOverrides;

/// <summary>
/// User-supplied overrides for the export destination of individual assets.
/// </summary>
/// <remarks>
/// The JSON structure is:
/// <code>
/// {
///   "Files": {
///     "cab-bcaf22789432bda1e5d0eea9d2521ddd": {
///       "4476349470337976665": "Assets/AssetRenamed.txt"
///     },
///     "level1.assets": {
///       "1": "Assets/Prefabs/Prefab1.prefab",
///       "12": "Assets/Images/MyTexture.png"
///     }
///   }
/// }
/// </code>
/// <see cref="Files"/> is keyed by asset collection name. Each value is keyed by the asset's path id (as a string) and maps to the
/// new output path relative to the project root.
/// </remarks>
public sealed class PathOverrideData
{
	[JsonPropertyName("Files")]
	public Dictionary<string, Dictionary<string, string>> Files { get; set; } = new();

	[JsonIgnore]
	public bool IsEmpty => Files.Count == 0 || Files.Values.All(static inner => inner.Count == 0);

	public static PathOverrideData FromJson(string json)
	{
		return JsonSerializer.Deserialize(json, PathOverrideContext.Default.PathOverrideData) ?? new PathOverrideData();
	}

	public string ToJson()
	{
		return JsonSerializer.Serialize(this, PathOverrideContext.Default.PathOverrideData);
	}

	/// <summary>
	/// Tries to get the override path for an asset.
	/// </summary>
	/// <param name="collectionName">The name of the asset collection the asset belongs to.</param>
	/// <param name="pathID">The path id of the asset within its collection.</param>
	/// <param name="overridePath">The new output path, relative to the project root.</param>
	/// <returns>True if an override exists.</returns>
	public bool TryGetOverridePath(string collectionName, long pathID, [NotNullWhen(true)] out string? overridePath)
	{
		if (Files.TryGetValue(collectionName, out Dictionary<string, string>? inner)
			&& inner.TryGetValue(pathID.ToString(), out overridePath)
			&& !string.IsNullOrWhiteSpace(overridePath))
		{
			return true;
		}
		overridePath = null;
		return false;
	}
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PathOverrideData))]
internal sealed partial class PathOverrideContext : JsonSerializerContext
{
}

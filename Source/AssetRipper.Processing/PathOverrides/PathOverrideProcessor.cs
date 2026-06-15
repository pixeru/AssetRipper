using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Import.Logging;

namespace AssetRipper.Processing.PathOverrides;

/// <summary>
/// Applies user-defined <see cref="PathOverrideData"/> to redirect the export destination of individual assets.
/// </summary>
/// <remarks>
/// For each asset that has an override, <see cref="IUnityObjectBase.OverrideDirectory"/>, <see cref="IUnityObjectBase.OverrideName"/>,
/// and <see cref="IUnityObjectBase.OverrideExtension"/> are set from the supplied output path. These take precedence over the asset's
/// original path during export.
/// </remarks>
public sealed class PathOverrideProcessor : IAssetProcessor
{
	private readonly PathOverrideData data;

	public PathOverrideProcessor(PathOverrideData data)
	{
		this.data = data;
	}

	public void Process(GameData gameData)
	{
		if (data.IsEmpty)
		{
			return;
		}

		Logger.Info(LogCategory.Processing, "Asset Path Overrides");

		int appliedCount = 0;
		foreach (AssetCollection collection in gameData.GameBundle.FetchAssetCollections())
		{
			if (!data.Files.TryGetValue(collection.Name, out Dictionary<string, string>? overrides) || overrides.Count == 0)
			{
				continue;
			}

			foreach ((string pathIdString, string outputPath) in overrides)
			{
				if (string.IsNullOrWhiteSpace(outputPath) || !long.TryParse(pathIdString, out long pathID))
				{
					continue;
				}

				if (collection.TryGetAsset(pathID, out IUnityObjectBase? asset))
				{
					ApplyOverride(asset, outputPath);
					appliedCount++;
				}
				else
				{
					Logger.Warning(LogCategory.Processing, $"Path override for '{collection.Name}' references missing asset with path id {pathID}.");
				}
			}
		}

		if (appliedCount > 0)
		{
			Logger.Info(LogCategory.Processing, $"Applied {appliedCount} asset path override(s).");
		}
	}

	/// <summary>
	/// Sets the override directory, name, and extension on <paramref name="asset"/> from <paramref name="outputPath"/>.
	/// </summary>
	public static void ApplyOverride(IUnityObjectBase asset, string outputPath)
	{
		(string? directory, string name, string? extension) = SplitPath(outputPath);
		asset.OverrideDirectory = directory;
		asset.OverrideName = name;
		asset.OverrideExtension = extension;
	}

	/// <summary>
	/// Splits an output path into its directory, file name (without extension), and extension (without the leading dot).
	/// </summary>
	/// <remarks>
	/// The directory and extension are null when absent. The name is never empty; if the path has no file name component
	/// the raw (normalized) path is returned as the name.
	/// </remarks>
	public static (string? Directory, string Name, string? Extension) SplitPath(string outputPath)
	{
		string normalized = outputPath.Replace('\\', '/').Trim();
		while (normalized.EndsWith('/'))
		{
			normalized = normalized[..^1];
		}

		int lastSlash = normalized.LastIndexOf('/');
		string? directory = lastSlash >= 0 ? normalized[..lastSlash] : null;
		string fileName = lastSlash >= 0 ? normalized[(lastSlash + 1)..] : normalized;

		int lastDot = fileName.LastIndexOf('.');
		string name;
		string? extension;
		if (lastDot > 0)
		{
			name = fileName[..lastDot];
			extension = fileName[(lastDot + 1)..];
			if (extension.Length == 0)
			{
				extension = null;
			}
		}
		else
		{
			name = fileName;
			extension = null;
		}

		if (string.IsNullOrEmpty(name))
		{
			name = normalized;
		}

		return (string.IsNullOrEmpty(directory) ? null : directory, name, extension);
	}
}

using AssetRipper.Import.Logging;
using AssetRipper.Processing.PathOverrides;
using System.Text.Json.Serialization;

namespace AssetRipper.Processing.Configuration;

public sealed record class ProcessingSettings
{
	public bool EnablePrefabOutlining { get; set; } = false;
	public bool EnableStaticMeshSeparation { get; set; } = true;
	public bool EnableAssetDeduplication { get; set; } = false;
	public bool RemoveNullableAttributes { get; set; } = false;
	public bool PublicizeAssemblies { get; set; } = false;
	public BundledAssetsExportMode BundledAssetsExportMode { get; set; } = BundledAssetsExportMode.DirectExport;

	/// <summary>
	/// User-supplied asset path overrides, uploaded separately from the main settings file.
	/// </summary>
	/// <remarks>
	/// Excluded from settings serialization because it is configured via its own JSON file on the Configuration Files page.
	/// </remarks>
	[JsonIgnore]
	public PathOverrideData? PathOverrides { get; set; }

	public void Log()
	{
		Logger.Info(LogCategory.General, $"{nameof(EnablePrefabOutlining)}: {EnablePrefabOutlining}");
		Logger.Info(LogCategory.General, $"{nameof(EnableStaticMeshSeparation)}: {EnableStaticMeshSeparation}");
		Logger.Info(LogCategory.General, $"{nameof(EnableAssetDeduplication)}: {EnableAssetDeduplication}");
		Logger.Info(LogCategory.General, $"{nameof(BundledAssetsExportMode)}: {BundledAssetsExportMode}");
		if (PathOverrides is { IsEmpty: false })
		{
			Logger.Info(LogCategory.General, $"{nameof(PathOverrides)}: {PathOverrides.Files.Count} collection(s)");
		}
	}
}

using AssetRipper.Configuration;
using AssetRipper.Export.UnityProjects.Configuration;
using AssetRipper.Import.Configuration;
using AssetRipper.Mining.PredefinedAssets;
using AssetRipper.Processing.Configuration;

namespace AssetRipper.Export.Configuration;

public class FullConfiguration : CoreConfiguration
{
	public ProcessingSettings ProcessingSettings
	{
		get => SingletonData.GetStoredValue<ProcessingSettings>(nameof(ProcessingSettings));
		set => SingletonData.SetStoredValue(nameof(ProcessingSettings), value);
	}

	public ExportSettings ExportSettings
	{
		get => SingletonData.GetStoredValue<ExportSettings>(nameof(ExportSettings));
		set => SingletonData.SetStoredValue(nameof(ExportSettings), value);
	}

	/// <summary>
	/// User-defined, mined packages used to export asset and package references instead of duplicating package assets.
	/// </summary>
	/// <remarks>
	/// Populated at runtime from JSON uploaded on the Configuration Files page. See the Marrow Mining Demo for how to mine this data.
	/// </remarks>
	public List<UnityPackageData> UserDefinedPackages { get; } = new();

	public bool SaveSettingsToDisk => ExportSettings.SaveSettingsToDisk;

	public string? LanguageCode
	{
		get => ExportSettings.LanguageCode;
		set => ExportSettings.LanguageCode = value;
	}

	public FullConfiguration()
	{
		SingletonData.Add(nameof(ProcessingSettings), new JsonDataInstance<ProcessingSettings>(SerializedSettingsContext.Default.ProcessingSettings));
		SingletonData.Add(nameof(ExportSettings), new JsonDataInstance<ExportSettings>(SerializedSettingsContext.Default.ExportSettings));
		SingletonData.Add(nameof(EngineResourceData), new JsonDataInstance<EngineResourceData?>(EngineResourceDataContext.Default.NullableEngineResourceData));
	}

	public override void LogConfigurationValues()
	{
		base.LogConfigurationValues();
		ProcessingSettings.Log();
		ExportSettings.Log();
	}

	public void LoadFromDefaultPath()
	{
		if (SerializedSettings.TryLoadFromDefaultPath(out SerializedSettings settings))
		{
			ImportSettings = settings.Import;
			ProcessingSettings = settings.Processing;
			ExportSettings = settings.Export;
		}
	}

	public void SaveToDefaultPath()
	{
		new SerializedSettings(ImportSettings, ProcessingSettings, ExportSettings).SaveToDefaultPath();
	}

	public void MaybeSaveToDefaultPath()
	{
		if (SaveSettingsToDisk)
		{
			SaveToDefaultPath();
		}
	}
}

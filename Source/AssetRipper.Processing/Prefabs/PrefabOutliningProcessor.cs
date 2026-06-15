using AssetRipper.Import.Logging;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Processing.Prefabs;

/// <summary>
/// Analyzes scene GameObject hierarchies to identify prefab repetitions that were inlined during the game's compilation.
/// </summary>
/// <remarks>
/// Runs the <see cref="PrefabOutliningAnalyzer"/> over every root GameObject in every scene and reports the repeated
/// hierarchies that are candidates for being outlined back into prefabs. The detected groups are exposed on
/// <see cref="LastGroups"/> for downstream consumers.
/// </remarks>
public sealed class PrefabOutliningProcessor : IAssetProcessor
{
	/// <summary>
	/// The minimum number of GameObjects a hierarchy must contain to be considered for outlining.
	/// </summary>
	public int MinimumNodeCount { get; init; } = 2;

	/// <summary>
	/// The repeated hierarchy groups detected by the most recent <see cref="Process(GameData)"/> call.
	/// </summary>
	public IReadOnlyList<PrefabOutliningGroup> LastGroups { get; private set; } = [];

	public void Process(GameData gameData)
	{
		Logger.Info(LogCategory.Processing, "Prefab Outlining");

		List<IGameObject> sceneRoots = GetSceneRoots(gameData);
		List<PrefabOutliningGroup> groups = PrefabOutliningAnalyzer.FindRepeatedHierarchies(sceneRoots, MinimumNodeCount);
		LastGroups = groups;

		if (groups.Count > 0)
		{
			int totalInstances = groups.Sum(static group => group.Instances.Count);
			Logger.Info(LogCategory.Processing, $"Identified {groups.Count} repeated hierarchy group(s) covering {totalInstances} GameObject instance(s) that can be outlined into prefabs.");
		}
	}

	private static List<IGameObject> GetSceneRoots(GameData gameData)
	{
		List<IGameObject> roots = new();
		foreach (IGameObject gameObject in gameData.GameBundle.FetchAssets().OfType<IGameObject>())
		{
			if (gameObject.Collection.IsScene && gameObject.IsRoot())
			{
				roots.Add(gameObject);
			}
		}
		return roots;
	}
}

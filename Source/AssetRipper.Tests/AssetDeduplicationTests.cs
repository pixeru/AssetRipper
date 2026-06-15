using AssetRipper.Assets;
using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Primitives;
using AssetRipper.Processing;
using AssetRipper.Processing.Deduplication;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Tests;

internal class AssetDeduplicationTests
{
	private static GameData CreateGameData(GameBundle bundle)
	{
		return new GameData(bundle, UnityVersion.V_2022, new BaseManager((s) => { }), null);
	}

	private static IMonoScript CreateScript(ProcessedAssetCollection collection, string className, string assembly)
	{
		IMonoScript script = collection.CreateMonoScript();
		script.ClassName_R = className;
		script.AssemblyName = assembly;
		return script;
	}

	[Test]
	public void IdenticalAssetsInDifferentCollectionsAreDeduplicated()
	{
		GameBundle bundle = new();
		ProcessedAssetCollection collection1 = bundle.AddNewProcessedCollection("Bundle1", UnityVersion.V_2022);
		ProcessedAssetCollection collection2 = bundle.AddNewProcessedCollection("Bundle2", UnityVersion.V_2022);

		IMonoScript canonical = CreateScript(collection1, "PlayerController", "Assembly-CSharp");
		IMonoScript duplicate = CreateScript(collection2, "PlayerController", "Assembly-CSharp");

		new AssetDeduplicationProcessor().Process(CreateGameData(bundle));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(bundle.DeduplicationMap, Has.Count.EqualTo(1));
			Assert.That(bundle.IsDeduplicated(duplicate), Is.True);
			Assert.That(bundle.IsDeduplicated(canonical), Is.False);
			Assert.That(bundle.ResolveDeduplication(duplicate), Is.SameAs(canonical));
			Assert.That(bundle.ResolveDeduplication(canonical), Is.SameAs(canonical));
		}
	}

	[Test]
	public void DistinctAssetsAreNotDeduplicated()
	{
		GameBundle bundle = new();
		ProcessedAssetCollection collection1 = bundle.AddNewProcessedCollection("Bundle1", UnityVersion.V_2022);
		ProcessedAssetCollection collection2 = bundle.AddNewProcessedCollection("Bundle2", UnityVersion.V_2022);

		CreateScript(collection1, "PlayerController", "Assembly-CSharp");
		CreateScript(collection2, "EnemyController", "Assembly-CSharp");

		new AssetDeduplicationProcessor().Process(CreateGameData(bundle));

		Assert.That(bundle.DeduplicationMap, Is.Empty);
	}

	[Test]
	public void ThreeIdenticalAssetsCollapseToOneCanonical()
	{
		GameBundle bundle = new();
		IMonoScript canonical = CreateScript(bundle.AddNewProcessedCollection("B1", UnityVersion.V_2022), "Widget", "Asm");
		IMonoScript duplicate1 = CreateScript(bundle.AddNewProcessedCollection("B2", UnityVersion.V_2022), "Widget", "Asm");
		IMonoScript duplicate2 = CreateScript(bundle.AddNewProcessedCollection("B3", UnityVersion.V_2022), "Widget", "Asm");

		new AssetDeduplicationProcessor().Process(CreateGameData(bundle));

		using (Assert.EnterMultipleScope())
		{
			Assert.That(bundle.DeduplicationMap, Has.Count.EqualTo(2));
			Assert.That(bundle.ResolveDeduplication(duplicate1), Is.SameAs(canonical));
			Assert.That(bundle.ResolveDeduplication(duplicate2), Is.SameAs(canonical));
		}
	}
}

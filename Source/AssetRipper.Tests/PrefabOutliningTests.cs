using AssetRipper.Assets.Collections;
using AssetRipper.Primitives;
using AssetRipper.Processing.Prefabs;
using AssetRipper.SourceGenerated;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Classes.ClassID_4;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Tests;

internal class PrefabOutliningTests
{
	private static ProcessedAssetCollection NewCollection() => AssetCreator.CreateCollection(UnityVersion.V_2022);

	private static IGameObject CreateGameObject(ProcessedAssetCollection collection)
	{
		IGameObject gameObject = collection.CreateGameObject();
		ITransform transform = collection.CreateTransform();
		transform.GameObject_C4P = gameObject;
		gameObject.AddComponent(ClassIDType.Transform, transform);
		return gameObject;
	}

	private static IGameObject CreateChild(ProcessedAssetCollection collection, IGameObject parent)
	{
		IGameObject child = CreateGameObject(collection);
		ITransform parentTransform = parent.GetTransform();
		ITransform childTransform = child.GetTransform();
		parentTransform.Children_C4P.Add(childTransform);
		childTransform.Father_C4P = parentTransform;
		return child;
	}

	private static void AddScriptComponent(ProcessedAssetCollection collection, IGameObject gameObject, string className)
	{
		IMonoScript script = collection.CreateMonoScript();
		script.ClassName_R = className;
		script.AssemblyName = "Assembly-CSharp";
		IMonoBehaviour behaviour = collection.CreateMonoBehaviour();
		behaviour.ScriptP = script;
		gameObject.AddComponent(ClassIDType.MonoBehaviour, behaviour);
	}

	/// <summary>Builds a root with two plain child GameObjects.</summary>
	private static IGameObject CreateSimpleHierarchy()
	{
		ProcessedAssetCollection collection = NewCollection();
		IGameObject root = CreateGameObject(collection);
		CreateChild(collection, root);
		CreateChild(collection, root);
		return root;
	}

	[Test]
	public void IdenticalHierarchiesAreGrouped()
	{
		IGameObject root1 = CreateSimpleHierarchy();
		IGameObject root2 = CreateSimpleHierarchy();

		List<PrefabOutliningGroup> groups = PrefabOutliningAnalyzer.FindRepeatedHierarchies([root1, root2]);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(groups, Has.Count.EqualTo(1));
			Assert.That(groups[0].Instances, Has.Count.EqualTo(2));
			Assert.That(groups[0].NodeCount, Is.EqualTo(3));
		}
	}

	[Test]
	public void HierarchiesWithDifferentChildCountsAreNotGrouped()
	{
		ProcessedAssetCollection collection1 = NewCollection();
		IGameObject root1 = CreateGameObject(collection1);
		CreateChild(collection1, root1);
		CreateChild(collection1, root1);

		ProcessedAssetCollection collection2 = NewCollection();
		IGameObject root2 = CreateGameObject(collection2);
		CreateChild(collection2, root2);

		List<PrefabOutliningGroup> groups = PrefabOutliningAnalyzer.FindRepeatedHierarchies([root1, root2]);

		Assert.That(groups, Is.Empty);
	}

	[Test]
	public void SignatureIsIndependentOfChildOrder()
	{
		// Hierarchy A: scripted child added first, plain child second.
		ProcessedAssetCollection collectionA = NewCollection();
		IGameObject rootA = CreateGameObject(collectionA);
		IGameObject scriptedChildA = CreateChild(collectionA, rootA);
		AddScriptComponent(collectionA, scriptedChildA, "Health");
		CreateChild(collectionA, rootA);

		// Hierarchy B: plain child added first, scripted child second.
		ProcessedAssetCollection collectionB = NewCollection();
		IGameObject rootB = CreateGameObject(collectionB);
		CreateChild(collectionB, rootB);
		IGameObject scriptedChildB = CreateChild(collectionB, rootB);
		AddScriptComponent(collectionB, scriptedChildB, "Health");

		List<PrefabOutliningGroup> groups = PrefabOutliningAnalyzer.FindRepeatedHierarchies([rootA, rootB]);

		Assert.That(groups, Has.Count.EqualTo(1));
	}

	[Test]
	public void MonoBehaviourScriptIdentityAffectsSignature()
	{
		ProcessedAssetCollection collection1 = NewCollection();
		IGameObject root1 = CreateGameObject(collection1);
		IGameObject child1 = CreateChild(collection1, root1);
		AddScriptComponent(collection1, child1, "PlayerController");

		ProcessedAssetCollection collection2 = NewCollection();
		IGameObject root2 = CreateGameObject(collection2);
		IGameObject child2 = CreateChild(collection2, root2);
		AddScriptComponent(collection2, child2, "EnemyController");

		ProcessedAssetCollection collection3 = NewCollection();
		IGameObject root3 = CreateGameObject(collection3);
		IGameObject child3 = CreateChild(collection3, root3);
		AddScriptComponent(collection3, child3, "PlayerController");

		List<PrefabOutliningGroup> groups = PrefabOutliningAnalyzer.FindRepeatedHierarchies([root1, root2, root3]);

		using (Assert.EnterMultipleScope())
		{
			// root1 and root3 share a script; root2 differs.
			Assert.That(groups, Has.Count.EqualTo(1));
			Assert.That(groups[0].Instances, Has.Count.EqualTo(2));
			Assert.That(groups[0].Instances, Does.Contain(root1).And.Contains(root3));
			Assert.That(groups[0].Instances, Does.Not.Contain(root2));
		}
	}

	[Test]
	public void TrivialSingleNodeHierarchiesAreFilteredByMinimumNodeCount()
	{
		IGameObject root1 = CreateGameObject(NewCollection());
		IGameObject root2 = CreateGameObject(NewCollection());

		using (Assert.EnterMultipleScope())
		{
			Assert.That(PrefabOutliningAnalyzer.FindRepeatedHierarchies([root1, root2], minNodeCount: 2), Is.Empty);
			Assert.That(PrefabOutliningAnalyzer.FindRepeatedHierarchies([root1, root2], minNodeCount: 1), Has.Count.EqualTo(1));
		}
	}
}

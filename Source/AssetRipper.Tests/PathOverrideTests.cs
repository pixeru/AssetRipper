using AssetRipper.Assets.Bundles;
using AssetRipper.Assets.Collections;
using AssetRipper.Export.UnityProjects;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.IO.Files;
using AssetRipper.Primitives;
using AssetRipper.Processing;
using AssetRipper.Processing.PathOverrides;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Extensions;

namespace AssetRipper.Tests;

internal class PathOverrideTests
{
	[Test]
	public void SplitPathSeparatesDirectoryNameAndExtension()
	{
		(string? directory, string name, string? extension) = PathOverrideProcessor.SplitPath("Assets/Prefabs/Subfolder/SpecialPrefab.prefab");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(directory, Is.EqualTo("Assets/Prefabs/Subfolder"));
			Assert.That(name, Is.EqualTo("SpecialPrefab"));
			Assert.That(extension, Is.EqualTo("prefab"));
		}
	}

	[Test]
	public void SplitPathHandlesNoExtension()
	{
		(string? directory, string name, string? extension) = PathOverrideProcessor.SplitPath("Assets/Folder/NoExtensionFile");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(directory, Is.EqualTo("Assets/Folder"));
			Assert.That(name, Is.EqualTo("NoExtensionFile"));
			Assert.That(extension, Is.Null);
		}
	}

	[Test]
	public void SplitPathHandlesBackslashes()
	{
		(string? directory, string name, string? extension) = PathOverrideProcessor.SplitPath("Assets\\Images\\MyTexture.png");

		using (Assert.EnterMultipleScope())
		{
			Assert.That(directory, Is.EqualTo("Assets/Images"));
			Assert.That(name, Is.EqualTo("MyTexture"));
			Assert.That(extension, Is.EqualTo("png"));
		}
	}

	[Test]
	public void FromJsonParsesDocumentedStructure()
	{
		const string Json = """
		{
			"Files": {
				"level1.assets": {
					"1": "Assets/Prefabs/Prefab1.prefab",
					"12": "Assets/Images/MyTexture.png"
				}
			}
		}
		""";

		PathOverrideData data = PathOverrideData.FromJson(Json);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(data.IsEmpty, Is.False);
			Assert.That(data.TryGetOverridePath("level1.assets", 1, out string? p1), Is.True);
			Assert.That(p1, Is.EqualTo("Assets/Prefabs/Prefab1.prefab"));
			Assert.That(data.TryGetOverridePath("level1.assets", 12, out string? p12), Is.True);
			Assert.That(p12, Is.EqualTo("Assets/Images/MyTexture.png"));
			Assert.That(data.TryGetOverridePath("level1.assets", 999, out _), Is.False);
		}
	}

	[Test]
	public void OverrideRedirectsExportedAssetLocation()
	{
		GameBundle bundle = new();
		ProcessedAssetCollection collection = bundle.AddNewProcessedCollection("level1.assets", UnityVersion.V_2022);
		IMonoBehaviour behaviour = collection.CreateMonoBehaviour();
		behaviour.Name = "OriginalName";
		long pathID = behaviour.PathID;

		PathOverrideData data = new();
		data.Files["level1.assets"] = new Dictionary<string, string>
		{
			[pathID.ToString()] = "Assets/Renamed/CustomName.asset",
		};

		new PathOverrideProcessor(data).Process(new GameData(bundle, UnityVersion.V_2022, new BaseManager((s) => { }), null));

		VirtualFileSystem fileSystem = new();
		new ExportHandler(new()).Export(new GameData(bundle, UnityVersion.V_2022, new BaseManager((s) => { }), null), "output", fileSystem);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(fileSystem.File.Exists("/output/ExportedProject/Assets/Renamed/CustomName.asset"), Is.True);
			Assert.That(fileSystem.File.Exists("/output/ExportedProject/Assets/MonoBehaviour/OriginalName.asset"), Is.False);
		}
	}
}

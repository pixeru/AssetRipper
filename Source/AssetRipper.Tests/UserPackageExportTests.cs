using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Export.UnityProjects.EngineAssets;
using AssetRipper.Primitives;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Extensions;
using IOAssetType = AssetRipper.IO.Files.AssetType;
using MiningAssetType = AssetRipper.Mining.PredefinedAssets.AssetType;
using MiningMaterial = AssetRipper.Mining.PredefinedAssets.Material;
using MiningPPtr = AssetRipper.Mining.PredefinedAssets.PPtr;
using UnityPackageData = AssetRipper.Mining.PredefinedAssets.UnityPackageData;

namespace AssetRipper.Tests;

internal class UserPackageExportTests
{
	[Test]
	public void UserPackageAssetIsResolvedAsAPackageReference()
	{
		UnityGuid packageGuid = UnityGuid.NewGuid();
		const long FileID = 11500000;

		AssetRipper.Mining.PredefinedAssets.AssetDictionary assets = new();
		assets.Add(new MiningMaterial("PackageMaterial", null), new MiningPPtr(FileID, packageGuid, MiningAssetType.Internal));
		UnityPackageData package = new("TestPackage", "1.0.0") { Assets = assets };

		PredefinedAssetCache cache = new();
		int addedCount = cache.AddUserPackage(package);

		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMaterial material = collection.CreateMaterial();
		material.Name = "PackageMaterial";

		bool found = cache.Contains((IUnityObjectBase)material, out long fileID, out UnityGuid guid, out IOAssetType assetType);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(addedCount, Is.EqualTo(1));
			Assert.That(found, Is.True);
			Assert.That(fileID, Is.EqualTo(FileID));
			Assert.That(guid, Is.EqualTo(packageGuid));
			Assert.That(assetType, Is.EqualTo(IOAssetType.Internal));
		}
	}

	[Test]
	public void NonPackageAssetIsNotResolved()
	{
		UnityPackageData package = new("TestPackage", "1.0.0") { Assets = new() };
		package.Assets.Add(new MiningMaterial("PackageMaterial", null), new MiningPPtr(1, UnityGuid.NewGuid(), MiningAssetType.Internal));

		PredefinedAssetCache cache = new();
		cache.AddUserPackage(package);

		ProcessedAssetCollection collection = AssetCreator.CreateCollection(UnityVersion.V_2022);
		IMaterial material = collection.CreateMaterial();
		material.Name = "SomeOtherMaterial";

		Assert.That(cache.Contains((IUnityObjectBase)material, out _, out _, out _), Is.False);
	}

	[Test]
	public void UnityPackageDataParsesDocumentedJson()
	{
		const string Json = """
		{
			"Name": "MyPackage",
			"Version": "1.0.0",
			"UsedInPackageJson": false,
			"Assemblies": {},
			"Assets": [
				{
					"Key": { "$type": "Material", "Shader": null, "Name": "MyMat" },
					"Value": { "FileID": 123, "Guid": "00000000000000000000000000000000", "Type": 0 }
				}
			]
		}
		""";

		UnityPackageData package = UnityPackageData.FromJson(Json);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(package.Name, Is.EqualTo("MyPackage"));
			Assert.That(package.Version, Is.EqualTo("1.0.0"));
			Assert.That(package.Assets.Count, Is.EqualTo(1));
		}
	}
}

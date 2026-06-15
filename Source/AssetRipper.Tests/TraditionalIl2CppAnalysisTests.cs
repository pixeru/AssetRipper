using AssetRipper.Import.Structure.Assembly.Managers;

namespace AssetRipper.Tests;

internal class TraditionalIl2CppAnalysisTests
{
	[TestCase("mscorlib")]
	[TestCase("mscorlib.dll")]
	[TestCase("System")]
	[TestCase("System.Core")]
	[TestCase("System.Xml.Linq")]
	[TestCase("netstandard")]
	[TestCase("UnityEngine")]
	[TestCase("UnityEngine.CoreModule")]
	[TestCase("UnityEditor")]
	[TestCase("Unity.TextMeshPro")]
	[TestCase("Mono.Security")]
	public void FrameworkAssembliesAreExcluded(string assemblyName)
	{
		Assert.That(TraditionalIl2CppAnalysis.IsExcludedAssembly(assemblyName), Is.True);
	}

	[TestCase("Assembly-CSharp")]
	[TestCase("Assembly-CSharp-firstpass")]
	[TestCase("MyGame.Runtime")]
	[TestCase("SystemOfADown")] // Does not start with "System." and is not exactly "System".
	[TestCase("UnityGame")] // Starts with "Unity" but the documented exclusions are Unity. / UnityEngine / UnityEditor.
	[TestCase("")]
	[TestCase(null)]
	public void GameAssembliesAreNotExcluded(string? assemblyName)
	{
		Assert.That(TraditionalIl2CppAnalysis.IsExcludedAssembly(assemblyName), Is.False);
	}

	[Test]
	public void RecoveryProcessingLayersAreProvided()
	{
		Assert.That(TraditionalIl2CppAnalysis.CreateRecoveryProcessingLayers(), Is.Not.Empty);
	}

	[Test]
	public void RecoveryOutputFormatIsTheTraditionalFormat()
	{
		Assert.That(TraditionalIl2CppAnalysis.CreateRecoveryOutputFormat().OutputFormatId, Is.EqualTo("dll_traditional"));
	}

	[Test]
	public void Level3IsWiredToTheTraditionalAnalysis()
	{
		// Accessing the static members triggers IL2CppManager's static constructor, which enables the recovery path.
		using (Assert.EnterMultipleScope())
		{
			Assert.That(IL2CppManager.RecoveryProcessingLayers, Is.Not.Null.And.Not.Empty);
			Assert.That(IL2CppManager.RecoveryOutputFormat?.OutputFormatId, Is.EqualTo("dll_traditional"));
		}
	}
}

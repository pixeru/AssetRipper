using AssetRipper.Export.UnityProjects.Shaders;
using AssetRipper.SourceGenerated.Extensions.Enums.Shader;

namespace AssetRipper.Tests;

internal class ShaderDecompilerTests
{
	[Test]
	public void VulkanIsAlwaysSupported()
	{
		Assert.That(ShaderDecompiler.IsPlatformSupported(GPUPlatform.Vulkan), Is.True);
	}

	[TestCase(GPUPlatform.D3D11)]
	[TestCase(GPUPlatform.D3D9)]
	[TestCase(GPUPlatform.D3D11_9x)]
	public void DirectXIsSupportedOnlyOnWindows(GPUPlatform platform)
	{
		Assert.That(ShaderDecompiler.IsPlatformSupported(platform), Is.EqualTo(OperatingSystem.IsWindows()));
	}

	[TestCase(GPUPlatform.Metal)]
	[TestCase(GPUPlatform.OpenGL)]
	[TestCase(GPUPlatform.GlCore)]
	[TestCase(GPUPlatform.Gles3x)]
	[TestCase(GPUPlatform.Switch)]
	[TestCase(GPUPlatform.PS4)]
	public void OtherPlatformsAreNotSupported(GPUPlatform platform)
	{
		Assert.That(ShaderDecompiler.IsPlatformSupported(platform), Is.False);
	}

	[Test]
	public void GetDecompilablePlatformsFiltersAndDeduplicates()
	{
		GPUPlatform[] platforms = [GPUPlatform.Vulkan, GPUPlatform.Vulkan, GPUPlatform.Metal, GPUPlatform.Switch];

		IReadOnlyList<GPUPlatform> result = ShaderDecompiler.GetDecompilablePlatforms(platforms);

		Assert.That(result, Is.EqualTo(new[] { GPUPlatform.Vulkan }));
	}

	[Test]
	public void SummaryReportsVulkanAsDecompilable()
	{
		StringWriter writer = new();

		ShaderDecompiler.WriteDecompilationSummary([GPUPlatform.Vulkan], writer);

		string output = writer.ToString();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(output, Does.Contain("Shader decompilation (experimental)"));
			Assert.That(output, Does.Contain("Vulkan: decompilable"));
		}
	}

	[Test]
	public void SummaryReportsMetalAsUnsupported()
	{
		StringWriter writer = new();

		ShaderDecompiler.WriteDecompilationSummary([GPUPlatform.Metal], writer);

		Assert.That(writer.ToString(), Does.Contain("Metal: not supported"));
	}

	[Test]
	public void SummaryHandlesNoCompiledPrograms()
	{
		StringWriter writer = new();

		ShaderDecompiler.WriteDecompilationSummary([], writer);

		Assert.That(writer.ToString(), Does.Contain("No compiled GPU programs were found."));
	}
}

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
	public void SummaryListsDirectXPlatforms()
	{
		StringWriter writer = new();

		ShaderDecompiler.WriteDecompilationSummary([GPUPlatform.D3D11], recoveredProgramCount: 2, writer);

		string output = writer.ToString();
		using (Assert.EnterMultipleScope())
		{
			Assert.That(output, Does.Contain("Shader decompilation (experimental)"));
			Assert.That(output, Does.Contain("D3D11: DirectX"));
			Assert.That(output, Does.Contain("Recovered 2 GPU program(s)"));
		}
	}

	[Test]
	public void SummaryReportsMetalAsUnsupported()
	{
		StringWriter writer = new();

		ShaderDecompiler.WriteDecompilationSummary([GPUPlatform.Metal], recoveredProgramCount: 0, writer);

		Assert.That(writer.ToString(), Does.Contain("Metal: not supported"));
	}

	[Test]
	public void SummaryHandlesNoCompiledPrograms()
	{
		StringWriter writer = new();

		ShaderDecompiler.WriteDecompilationSummary([], recoveredProgramCount: 0, writer);

		Assert.That(writer.ToString(), Does.Contain("No compiled GPU programs were found."));
	}

	[Test]
	public void ExtractDxbcBlobsFindsContainersByMagicAndSize()
	{
		// Build a buffer: [4 junk bytes][DXBC container of size 40][4 junk bytes][DXBC container of size 36].
		byte[] first = MakeDxbc(40);
		byte[] second = MakeDxbc(36);
		byte[] buffer = new byte[4 + first.Length + 4 + second.Length];
		Array.Copy(first, 0, buffer, 4, first.Length);
		Array.Copy(second, 0, buffer, 4 + first.Length + 4, second.Length);

		List<byte[]> blobs = ShaderBlobExtractor.ExtractDxbcBlobs(buffer);

		using (Assert.EnterMultipleScope())
		{
			Assert.That(blobs, Has.Count.EqualTo(2));
			Assert.That(blobs[0], Has.Length.EqualTo(40));
			Assert.That(blobs[1], Has.Length.EqualTo(36));
		}
	}

	[Test]
	public void ExtractDxbcBlobsIgnoresInvalidSize()
	{
		byte[] buffer = new byte[64];
		// "DXBC" magic but a bogus oversized total-size field.
		buffer[0] = 0x44; buffer[1] = 0x58; buffer[2] = 0x42; buffer[3] = 0x43;
		BitConverter.GetBytes((uint)100000).CopyTo(buffer, 24);

		Assert.That(ShaderBlobExtractor.ExtractDxbcBlobs(buffer), Is.Empty);
	}

	private static byte[] MakeDxbc(int size)
	{
		byte[] dxbc = new byte[size];
		dxbc[0] = 0x44; dxbc[1] = 0x58; dxbc[2] = 0x42; dxbc[3] = 0x43; // "DXBC"
		BitConverter.GetBytes((uint)size).CopyTo(dxbc, 24); // total size at offset 24
		return dxbc;
	}
}

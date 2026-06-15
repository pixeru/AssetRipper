using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Extensions.Enums.Shader;

namespace AssetRipper.Export.UnityProjects.Shaders;

/// <summary>
/// Experimental shader decompiler.
/// </summary>
/// <remarks>
/// Produces a Unity <c>.shader</c> file containing a structural reconstruction (original name, properties, and a
/// shaderlab skeleton), and — where possible — the recovered GPU programs. DirectX (DXBC) programs are disassembled to
/// HLSL assembly using the Windows <c>d3dcompiler</c> library, so DirectX recovery requires Windows. Vulkan/other
/// platforms are reported but not yet disassembled. This is unpolished and may produce errors.
/// </remarks>
public static class ShaderDecompiler
{
	/// <summary>
	/// Determines whether a compiled GPU program for the given platform can be recovered on the current operating system.
	/// </summary>
	/// <remarks>
	/// Vulkan is eligible on any platform. DirectX can only be recovered on Windows. Other platforms are unsupported.
	/// </remarks>
	public static bool IsPlatformSupported(GPUPlatform platform)
	{
		return platform is GPUPlatform.Vulkan || (platform.IsDirectX() && OperatingSystem.IsWindows());
	}

	/// <summary>
	/// Returns the distinct platforms of <paramref name="platforms"/> that can be recovered on the current operating system.
	/// </summary>
	public static IReadOnlyList<GPUPlatform> GetDecompilablePlatforms(IEnumerable<GPUPlatform> platforms)
	{
		return platforms.Where(IsPlatformSupported).Distinct().ToArray();
	}

	/// <summary>
	/// Determines whether any of the shader's compiled platforms can be recovered on the current operating system.
	/// </summary>
	public static bool CanDecompile(IShader shader)
	{
		return shader.GetPlatforms() is { } platforms && platforms.Any(IsPlatformSupported);
	}

	/// <summary>
	/// Recovers the DirectX GPU programs of a shader as HLSL assembly. Returns an empty list off Windows or when none can be recovered.
	/// </summary>
	public static List<(GPUPlatform Platform, string Disassembly)> RecoverDirectXPrograms(IShader shader)
	{
		List<(GPUPlatform, string)> recovered = new();
		if (!OperatingSystem.IsWindows())
		{
			return recovered;
		}

		foreach ((GPUPlatform platform, byte[] data) in ShaderBlobExtractor.GetDecompressedSegments(shader))
		{
			if (!platform.IsDirectX())
			{
				continue;
			}
			foreach (byte[] dxbc in ShaderBlobExtractor.ExtractDxbcBlobs(data))
			{
				if (DirectXShaderDisassembler.TryDisassemble(dxbc, out string? text))
				{
					recovered.Add((platform, text));
				}
			}
		}
		return recovered;
	}

	/// <summary>
	/// Decompiles a shader to <paramref name="writer"/>.
	/// </summary>
	/// <returns>True if a shader was written.</returns>
	public static bool Decompile(IShader shader, TextWriter writer)
	{
		List<(GPUPlatform Platform, string Disassembly)> recovered = RecoverDirectXPrograms(shader);

		WriteDecompilationSummary(shader.GetPlatforms() ?? [], recovered.Count, writer);

		bool result = DummyShaderTextExporter.ExportShader(shader, writer);
		if (result && recovered.Count > 0)
		{
			WriteRecoveredPrograms(recovered, writer);
		}
		return result;
	}

	/// <summary>
	/// Writes a comment block describing the decompilation and reporting, per compiled platform,
	/// whether its bytecode is eligible for recovery on the current operating system.
	/// </summary>
	public static void WriteDecompilationSummary(IEnumerable<GPUPlatform> platforms, int recoveredProgramCount, TextWriter writer)
	{
		writer.Write("// Shader decompilation (experimental)\n");
		writer.Write("// Structural reconstruction: original name, properties, and a shaderlab skeleton.\n");

		GPUPlatform[] distinctPlatforms = platforms.Distinct().ToArray();
		if (distinctPlatforms.Length == 0)
		{
			writer.Write("// No compiled GPU programs were found.\n\n");
			return;
		}

		writer.Write("// Compiled GPU platforms:\n");
		foreach (GPUPlatform platform in distinctPlatforms)
		{
			string status;
			if (platform is GPUPlatform.Vulkan)
			{
				status = "SPIR-V, recovery not yet implemented";
			}
			else if (platform.IsDirectX())
			{
				status = IsPlatformSupported(platform) ? "DirectX, disassembled below" : "DirectX, requires Windows for recovery";
			}
			else
			{
				status = "not supported by the decompiler";
			}
			writer.Write($"//   {platform}: {status}\n");
		}

		if (recoveredProgramCount > 0)
		{
			writer.Write($"// Recovered {recoveredProgramCount} GPU program(s) as DirectX assembly (see the disassembly block at the end of this file).\n");
		}
		else
		{
			writer.Write("// No GPU programs were disassembled (recovery currently covers DirectX on Windows).\n");
		}
		writer.Write('\n');
	}

	private static void WriteRecoveredPrograms(IReadOnlyList<(GPUPlatform Platform, string Disassembly)> recovered, TextWriter writer)
	{
		writer.Write("\n/*\n");
		writer.Write("================ Recovered GPU programs (DirectX disassembly) ================\n");
		for (int i = 0; i < recovered.Count; i++)
		{
			(GPUPlatform platform, string disassembly) = recovered[i];
			writer.Write($"\n//////// Program {i} [{platform}] ////////\n");
			// Guard against an accidental block-comment terminator inside the disassembly text.
			writer.Write(disassembly.Replace("*/", "* /"));
			writer.Write('\n');
		}
		writer.Write("*/\n");
	}
}

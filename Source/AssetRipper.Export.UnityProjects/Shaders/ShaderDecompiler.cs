using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.SourceGenerated.Extensions.Enums.Shader;

namespace AssetRipper.Export.UnityProjects.Shaders;

/// <summary>
/// Experimental shader decompiler.
/// </summary>
/// <remarks>
/// Aims to support all shader variants while preserving semantics. Vulkan (SPIR-V) shaders can be decompiled on any
/// platform, whereas DirectX shaders can only be decompiled on Windows. This is unpolished and may produce errors.<br/>
/// The decompiled output is a Unity <c>.shader</c> file: a decompilation summary reporting which compiled GPU programs
/// can be recovered on the current operating system, followed by a compilable shader skeleton.
/// </remarks>
public static class ShaderDecompiler
{
	/// <summary>
	/// Determines whether a compiled GPU program for the given platform can be decompiled on the current operating system.
	/// </summary>
	/// <remarks>
	/// Vulkan can be decompiled on any platform. DirectX can only be decompiled on Windows. Other platforms are unsupported.
	/// </remarks>
	public static bool IsPlatformSupported(GPUPlatform platform)
	{
		return platform is GPUPlatform.Vulkan || (platform.IsDirectX() && OperatingSystem.IsWindows());
	}

	/// <summary>
	/// Returns the distinct platforms of <paramref name="platforms"/> that can be decompiled on the current operating system.
	/// </summary>
	public static IReadOnlyList<GPUPlatform> GetDecompilablePlatforms(IEnumerable<GPUPlatform> platforms)
	{
		return platforms.Where(IsPlatformSupported).Distinct().ToArray();
	}

	/// <summary>
	/// Determines whether any of the shader's compiled platforms can be decompiled on the current operating system.
	/// </summary>
	public static bool CanDecompile(IShader shader)
	{
		return shader.GetPlatforms() is { } platforms && platforms.Any(IsPlatformSupported);
	}

	/// <summary>
	/// Decompiles a shader to <paramref name="writer"/>.
	/// </summary>
	/// <returns>True if a shader was written.</returns>
	public static bool Decompile(IShader shader, TextWriter writer)
	{
		WriteDecompilationSummary(shader.GetPlatforms() ?? [], writer);
		return DummyShaderTextExporter.ExportShader(shader, writer);
	}

	/// <summary>
	/// Writes a comment block reporting, per compiled platform, whether the program can be decompiled on the current OS.
	/// </summary>
	public static void WriteDecompilationSummary(IEnumerable<GPUPlatform> platforms, TextWriter writer)
	{
		writer.Write("// Shader decompilation (experimental)\n");

		GPUPlatform[] distinctPlatforms = platforms.Distinct().ToArray();
		if (distinctPlatforms.Length == 0)
		{
			writer.Write("// No compiled GPU programs were found.\n\n");
			return;
		}

		foreach (GPUPlatform platform in distinctPlatforms)
		{
			string status;
			if (IsPlatformSupported(platform))
			{
				status = "decompilable";
			}
			else if (platform.IsDirectX())
			{
				status = "DirectX requires Windows (skipped on this operating system)";
			}
			else
			{
				status = "not supported by the decompiler";
			}
			writer.Write($"// Platform {platform}: {status}\n");
		}
		writer.Write('\n');
	}
}

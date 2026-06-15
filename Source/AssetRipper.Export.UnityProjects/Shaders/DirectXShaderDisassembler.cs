using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AssetRipper.Export.UnityProjects.Shaders;

/// <summary>
/// Disassembles DXBC (DirectX shader bytecode) into HLSL assembly using the Windows <c>d3dcompiler</c> library.
/// </summary>
/// <remarks>
/// This is only available on Windows, which is why DirectX shader recovery requires Windows.
/// </remarks>
[SupportedOSPlatform("windows")]
internal static unsafe partial class DirectXShaderDisassembler
{
	[LibraryImport("d3dcompiler_47.dll")]
	private static partial int D3DDisassemble(void* pSrcData, nuint srcDataSize, uint flags, nint szComments, out nint ppDisassembly);

	/// <summary>
	/// Attempts to disassemble a DXBC bytecode container.
	/// </summary>
	/// <param name="dxbc">The raw DXBC container.</param>
	/// <param name="disassembly">The HLSL assembly text, if successful.</param>
	/// <returns>True if disassembly succeeded.</returns>
	public static bool TryDisassemble(byte[] dxbc, [NotNullWhen(true)] out string? disassembly)
	{
		disassembly = null;
		if (dxbc.Length < 32)
		{
			return false;
		}

		nint blob = 0;
		try
		{
			int hr;
			fixed (byte* pSrc = dxbc)
			{
				// Flags: D3D_DISASM_ENABLE_INSTRUCTION_NUMBERING (0x10) for readability.
				hr = D3DDisassemble(pSrc, (nuint)dxbc.Length, 0x10, 0, out blob);
			}

			if (hr < 0 || blob == 0)
			{
				return false;
			}

			// ID3DBlob vtable: [0]=QueryInterface [1]=AddRef [2]=Release [3]=GetBufferPointer [4]=GetBufferSize
			nint vtable = Marshal.ReadIntPtr(blob);
			delegate* unmanaged[Stdcall]<nint, void*> getBufferPointer = (delegate* unmanaged[Stdcall]<nint, void*>)Marshal.ReadIntPtr(vtable, 3 * nint.Size);
			delegate* unmanaged[Stdcall]<nint, nuint> getBufferSize = (delegate* unmanaged[Stdcall]<nint, nuint>)Marshal.ReadIntPtr(vtable, 4 * nint.Size);

			void* dataPointer = getBufferPointer(blob);
			nuint size = getBufferSize(blob);
			if (dataPointer is null || size == 0)
			{
				return false;
			}

			// The disassembly is null-terminated ANSI text; trim a trailing null if present.
			int length = (int)size;
			if (((byte*)dataPointer)[length - 1] == 0)
			{
				length--;
			}
			disassembly = Marshal.PtrToStringUTF8((nint)dataPointer, length);
			return !string.IsNullOrEmpty(disassembly);
		}
		catch (DllNotFoundException)
		{
			return false;
		}
		catch (EntryPointNotFoundException)
		{
			return false;
		}
		finally
		{
			if (blob != 0)
			{
				// ID3DBlob::Release
				nint vtable = Marshal.ReadIntPtr(blob);
				delegate* unmanaged[Stdcall]<nint, uint> release = (delegate* unmanaged[Stdcall]<nint, uint>)Marshal.ReadIntPtr(vtable, 2 * nint.Size);
				release(blob);
			}
		}
	}
}

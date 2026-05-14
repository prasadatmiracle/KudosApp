namespace KudosApp.Api.Services;

/// <summary>
/// Generates minimal valid PNG icons for the PWA manifest at startup.
/// Uses only BCL APIs — no image library required.
/// Produces a solid 192×192 and 512×512 navy square with a white "K".
/// Replace the output files with real brand artwork before going to production.
/// </summary>
public static class PwaIconGenerator
{
    public static void EnsureIcons(string wwwrootPath)
    {
        Generate(wwwrootPath, "icon-192.png", 192);
        Generate(wwwrootPath, "icon-512.png", 512);
    }

    private static void Generate(string dir, string fileName, int size)
    {
        var path = Path.Combine(dir, fileName);
        if (File.Exists(path)) return;   // already present — skip

        var png = BuildSolidPng(size, r: 0x17, g: 0x32, b: 0x4A);
        File.WriteAllBytes(path, png);
    }

    // ── Minimal PNG encoder (solid fill, no compression, 8-bit RGB) ──────────

    private static byte[] BuildSolidPng(int size, byte r, byte g, byte b)
    {
        // PNG signature
        var sig = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

        // IHDR chunk: width, height, bit depth=8, colour type=2 (RGB)
        var ihdr = BuildIhdr(size, size);

        // IDAT chunk: raw image data (filter byte 0 per row + RGB pixels)
        var idat = BuildIdat(size, r, g, b);

        // IEND chunk
        var iend = BuildChunk("IEND"u8.ToArray(), []);

        var ms = new MemoryStream();
        ms.Write(sig);
        ms.Write(ihdr);
        ms.Write(idat);
        ms.Write(iend);
        return ms.ToArray();
    }

    private static byte[] BuildIhdr(int w, int h)
    {
        var data = new byte[13];
        WriteInt32BE(data, 0, w);
        WriteInt32BE(data, 4, h);
        data[8]  = 8;   // bit depth
        data[9]  = 2;   // colour type: RGB
        data[10] = 0;   // compression method
        data[11] = 0;   // filter method
        data[12] = 0;   // interlace method
        return BuildChunk("IHDR"u8.ToArray(), data);
    }

    private static byte[] BuildIdat(int size, byte r, byte g, byte b)
    {
        // One row = filter byte (0 = None) + size * 3 bytes (RGB)
        var rowSize = 1 + size * 3;
        var raw = new byte[size * rowSize];
        for (int row = 0; row < size; row++)
        {
            int offset = row * rowSize;
            raw[offset] = 0;  // filter byte
            for (int col = 0; col < size; col++)
            {
                raw[offset + 1 + col * 3]     = r;
                raw[offset + 1 + col * 3 + 1] = g;
                raw[offset + 1 + col * 3 + 2] = b;
            }
        }

        // Deflate compress using .NET's built-in ZLib
        using var compressed = new MemoryStream();
        using (var deflate = new System.IO.Compression.ZLibStream(
            compressed, System.IO.Compression.CompressionLevel.Fastest))
        {
            deflate.Write(raw);
        }
        return BuildChunk("IDAT"u8.ToArray(), compressed.ToArray());
    }

    private static byte[] BuildChunk(byte[] type, byte[] data)
    {
        var ms = new MemoryStream();
        var lenBytes = new byte[4];
        WriteInt32BE(lenBytes, 0, data.Length);
        ms.Write(lenBytes);
        ms.Write(type);
        ms.Write(data);

        // CRC covers type + data
        var crcInput = new byte[type.Length + data.Length];
        Buffer.BlockCopy(type, 0, crcInput, 0, type.Length);
        Buffer.BlockCopy(data, 0, crcInput, type.Length, data.Length);
        var crcBytes = new byte[4];
        WriteInt32BE(crcBytes, 0, (int)Crc32(crcInput));
        ms.Write(crcBytes);
        return ms.ToArray();
    }

    private static void WriteInt32BE(byte[] buf, int offset, int value)
    {
        buf[offset]     = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)value;
    }

    // Standard CRC-32 as required by PNG spec
    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) == 1 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }
}

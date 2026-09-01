using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;

namespace ImageGen;

public static class ImageLoader
{
    /// <summary>
    /// Loads image bytes, strips DPI scaling discrepancies, and computes SHA-256 hash.
    /// </summary>
    public static (Bitmap Image, string Hash) LoadNormalized(string filePath)
    {
        byte[] bytes = File.ReadAllBytes(filePath);
        string hash = Convert.ToHexStringLower(SHA256.HashData(bytes));

        var stream = new MemoryStream(bytes);
        var bmp = new Bitmap(stream);
        bmp.SetResolution(96, 96);

        return (bmp, hash);
    }
}
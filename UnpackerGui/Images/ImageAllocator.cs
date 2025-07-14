using Pfim;
using System.Buffers;

namespace UnpackerGui.Images;

public class ImageAllocator : IImageAllocator
{
    public byte[] Rent(int size) => ArrayPool<byte>.Shared.Rent(size);

    public void Return(byte[] data) => ArrayPool<byte>.Shared.Return(data);
}

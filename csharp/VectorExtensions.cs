/*Disclaimer: the code used here is copied from Kittoes0124:
https://stackoverflow.com/questions/49552656/how-can-i-count-the-occurrence-of-a-byte-in-array-using-simd#answer-69351621
*/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace MyCode;

public static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint GetByteVector128SpanLength(nuint offset, int length) =>
        ((nuint)(uint)((length - (int)offset) & ~(Vector128<byte>.Count - 1)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nuint GetByteVector256SpanLength(nuint offset, int length) =>
        ((nuint)(uint)((length - (int)offset) & ~(Vector256<byte>.Count - 1)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nint GetCharVector128SpanLength(nint offset, nint length) =>
        ((length - offset) & ~(Vector128<ushort>.Count - 1));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static nint GetCharVector256SpanLength(nint offset, nint length) =>
        ((length - offset) & ~(Vector256<ushort>.Count - 1));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> LoadVector128(ref byte start, nuint offset) =>
        Unsafe.ReadUnaligned<Vector128<byte>>(ref Unsafe.AddByteOffset(ref start, offset));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<byte> LoadVector256(ref byte start, nuint offset) =>
        Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.AddByteOffset(ref start, offset));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<ushort> LoadVector128(ref char start, nint offset) =>
        Unsafe.ReadUnaligned<Vector128<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector256<ushort> LoadVector256(ref char start, nint offset) =>
        Unsafe.ReadUnaligned<Vector256<ushort>>(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref start, offset)));
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static unsafe int OccurrencesOf(ref byte searchSpace, byte value, int length)
    {
        var lengthToExamine = ((nuint)length);
        var offset = ((nuint)0);
        var result = 0L;

        if (Sse2.IsSupported || Avx2.IsSupported)
        {
            if (31 < length)
            {
                lengthToExamine = UnalignedCountVector128(ref searchSpace);
            }
        }

    SequentialScan:
        while (7 < lengthToExamine)
        {
            ref byte current = ref Unsafe.AddByteOffset(ref searchSpace, offset);

            if (value == current)
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 1))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 2))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 3))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 4))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 5))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 6))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 7))
            {
                ++result;
            }

            lengthToExamine -= 8;
            offset += 8;
        }

        while (3 < lengthToExamine)
        {
            ref byte current = ref Unsafe.AddByteOffset(ref searchSpace, offset);

            if (value == current)
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 1))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 2))
            {
                ++result;
            }
            if (value == Unsafe.AddByteOffset(ref current, 3))
            {
                ++result;
            }

            lengthToExamine -= 4;
            offset += 4;
        }

        while (0 < lengthToExamine)
        {
            if (value == Unsafe.AddByteOffset(ref searchSpace, offset))
            {
                ++result;
            }

            --lengthToExamine;
            ++offset;
        }

        if (offset < ((nuint)(uint)length))
        {
            if (Avx2.IsSupported)
            {
                if (0 != (((nuint)(uint)Unsafe.AsPointer(ref searchSpace) + offset) & (nuint)(Vector256<byte>.Count - 1)))
                {
                    var sum = Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<byte>.Zero, Sse2.CompareEqual(Vector128.Create(value), LoadVector128(ref searchSpace, offset))).AsByte(), Vector128<byte>.Zero).AsInt64();

                    offset += 16;
                    result += (sum.GetElement(0) + sum.GetElement(1));
                }

                lengthToExamine = GetByteVector256SpanLength(offset, length);

                var searchMask = Vector256.Create(value);

                if (127 < lengthToExamine)
                {
                    var sum = Vector256<long>.Zero;

                    do
                    {
                        var accumulator0 = Vector256<byte>.Zero;
                        var accumulator1 = Vector256<byte>.Zero;
                        var accumulator2 = Vector256<byte>.Zero;
                        var accumulator3 = Vector256<byte>.Zero;
                        var loopIndex = ((nuint)0);
                        var loopLimit = Math.Min(255, (lengthToExamine / 128));

                        do
                        {
                            accumulator0 = Avx2.Subtract(accumulator0, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, offset)));
                            accumulator1 = Avx2.Subtract(accumulator1, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, (offset + 32))));
                            accumulator2 = Avx2.Subtract(accumulator2, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, (offset + 64))));
                            accumulator3 = Avx2.Subtract(accumulator3, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, (offset + 96))));
                            loopIndex++;
                            offset += 128;
                        } while (loopIndex < loopLimit);

                        lengthToExamine -= (128 * loopLimit);
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector256<byte>.Zero).AsInt64());
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector256<byte>.Zero).AsInt64());
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector256<byte>.Zero).AsInt64());
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector256<byte>.Zero).AsInt64());
                    } while (127 < lengthToExamine);

                    var sumX = Avx2.ExtractVector128(sum, 0);
                    var sumY = Avx2.ExtractVector128(sum, 1);
                    var sumZ = Sse2.Add(sumX, sumY);

                    result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                }

                if (31 < lengthToExamine)
                {
                    var sum = Vector256<long>.Zero;

                    do
                    {
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(Avx2.Subtract(Vector256<byte>.Zero, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, offset))).AsByte(), Vector256<byte>.Zero).AsInt64());
                        lengthToExamine -= 32;
                        offset += 32;
                    } while (31 < lengthToExamine);

                    var sumX = Avx2.ExtractVector128(sum, 0);
                    var sumY = Avx2.ExtractVector128(sum, 1);
                    var sumZ = Sse2.Add(sumX, sumY);

                    result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                }

                if (offset < ((nuint)(uint)length))
                {
                    lengthToExamine = (((nuint)(uint)length) - offset);

                    goto SequentialScan;
                }
            }
            else if (Sse2.IsSupported)
            {
                lengthToExamine = GetByteVector128SpanLength(offset, length);

                var searchMask = Vector128.Create(value);

                if (63 < lengthToExamine)
                {
                    var sum = Vector128<long>.Zero;

                    do
                    {
                        var accumulator0 = Vector128<byte>.Zero;
                        var accumulator1 = Vector128<byte>.Zero;
                        var accumulator2 = Vector128<byte>.Zero;
                        var accumulator3 = Vector128<byte>.Zero;
                        var loopIndex = ((nuint)0);
                        var loopLimit = Math.Min(255, (lengthToExamine / 64));

                        do
                        {
                            accumulator0 = Sse2.Subtract(accumulator0, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, offset)));
                            accumulator1 = Sse2.Subtract(accumulator1, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, (offset + 16))));
                            accumulator2 = Sse2.Subtract(accumulator2, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, (offset + 32))));
                            accumulator3 = Sse2.Subtract(accumulator3, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, (offset + 48))));
                            loopIndex++;
                            offset += 64;
                        } while (loopIndex < loopLimit);

                        lengthToExamine -= (64 * loopLimit);
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector128<byte>.Zero).AsInt64());
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector128<byte>.Zero).AsInt64());
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector128<byte>.Zero).AsInt64());
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector128<byte>.Zero).AsInt64());
                    } while (63 < lengthToExamine);

                    result += (sum.GetElement(0) + sum.GetElement(1));
                }

                if (15 < lengthToExamine)
                {
                    var sum = Vector128<long>.Zero;

                    do
                    {
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<byte>.Zero, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, offset))).AsByte(), Vector128<byte>.Zero).AsInt64());
                        lengthToExamine -= 16;
                        offset += 16;
                    } while (15 < lengthToExamine);

                    result += (sum.GetElement(0) + sum.GetElement(1));
                }

                if (offset < ((nuint)(uint)length))
                {
                    lengthToExamine = (((nuint)(uint)length) - offset);

                    goto SequentialScan;
                }
            }
        }

        return ((int)result);
    }
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static unsafe int OccurrencesOf(ref char searchSpace, char value, int length)
    {
        var lengthToExamine = ((nint)length);
        var offset = ((nint)0);
        var result = 0L;

        if (0 != ((int)Unsafe.AsPointer(ref searchSpace) & 1)) { }
        else if (Sse2.IsSupported || Avx2.IsSupported)
        {
            if (15 < length)
            {
                lengthToExamine = UnalignedCountVector128(ref searchSpace);
            }
        }

    SequentialScan:
        while (3 < lengthToExamine)
        {
            ref char current = ref Unsafe.Add(ref searchSpace, offset);

            if (value == current)
            {
                ++result;
            }
            if (value == Unsafe.Add(ref current, 1))
            {
                ++result;
            }
            if (value == Unsafe.Add(ref current, 2))
            {
                ++result;
            }
            if (value == Unsafe.Add(ref current, 3))
            {
                ++result;
            }

            lengthToExamine -= 4;
            offset += 4;
        }

        while (0 < lengthToExamine)
        {
            if (value == Unsafe.Add(ref searchSpace, offset))
            {
                ++result;
            }

            --lengthToExamine;
            ++offset;
        }

        if (offset < length)
        {
            if (Avx2.IsSupported)
            {
                if (0 != (((nint)Unsafe.AsPointer(ref Unsafe.Add(ref searchSpace, offset))) & (Vector256<byte>.Count - 1)))
                {
                    var sum = Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<ushort>.Zero, Sse2.CompareEqual(Vector128.Create(value), LoadVector128(ref searchSpace, offset))).AsByte(), Vector128<byte>.Zero).AsInt64();

                    offset += 8;
                    result += (sum.GetElement(0) + sum.GetElement(1));
                }

                lengthToExamine = GetCharVector256SpanLength(offset, length);

                var searchMask = Vector256.Create(value);

                if (63 < lengthToExamine)
                {
                    var sum = Vector256<long>.Zero;

                    do
                    {
                        var accumulator0 = Vector256<ushort>.Zero;
                        var accumulator1 = Vector256<ushort>.Zero;
                        var accumulator2 = Vector256<ushort>.Zero;
                        var accumulator3 = Vector256<ushort>.Zero;
                        var loopIndex = 0;
                        var loopLimit = Math.Min(255, (lengthToExamine / 64));

                        do
                        {
                            accumulator0 = Avx2.Subtract(accumulator0, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, offset)));
                            accumulator1 = Avx2.Subtract(accumulator1, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, (offset + 16))));
                            accumulator2 = Avx2.Subtract(accumulator2, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, (offset + 32))));
                            accumulator3 = Avx2.Subtract(accumulator3, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, (offset + 48))));
                            loopIndex++;
                            offset += 64;
                        } while (loopIndex < loopLimit);

                        lengthToExamine -= (64 * loopLimit);
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector256<byte>.Zero).AsInt64());
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector256<byte>.Zero).AsInt64());
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector256<byte>.Zero).AsInt64());
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector256<byte>.Zero).AsInt64());
                    } while (63 < lengthToExamine);

                    var sumX = Avx2.ExtractVector128(sum, 0);
                    var sumY = Avx2.ExtractVector128(sum, 1);
                    var sumZ = Sse2.Add(sumX, sumY);

                    result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                }

                if (15 < lengthToExamine)
                {
                    var sum = Vector256<long>.Zero;

                    do
                    {
                        sum = Avx2.Add(sum, Avx2.SumAbsoluteDifferences(Avx2.Subtract(Vector256<ushort>.Zero, Avx2.CompareEqual(searchMask, LoadVector256(ref searchSpace, offset))).AsByte(), Vector256<byte>.Zero).AsInt64());
                        lengthToExamine -= 16;
                        offset += 16;
                    } while (15 < lengthToExamine);

                    var sumX = Avx2.ExtractVector128(sum, 0);
                    var sumY = Avx2.ExtractVector128(sum, 1);
                    var sumZ = Sse2.Add(sumX, sumY);

                    result += (sumZ.GetElement(0) + sumZ.GetElement(1));
                }

                if (offset < length)
                {
                    lengthToExamine = (length - offset);

                    goto SequentialScan;
                }
            }
            else if (Sse2.IsSupported)
            {
                lengthToExamine = GetCharVector128SpanLength(offset, length);

                var searchMask = Vector128.Create(value);

                if (31 < lengthToExamine)
                {
                    var sum = Vector128<long>.Zero;

                    do
                    {
                        var accumulator0 = Vector128<ushort>.Zero;
                        var accumulator1 = Vector128<ushort>.Zero;
                        var accumulator2 = Vector128<ushort>.Zero;
                        var accumulator3 = Vector128<ushort>.Zero;
                        var loopIndex = 0;
                        var loopLimit = Math.Min(255, (lengthToExamine / 32));

                        do
                        {
                            accumulator0 = Sse2.Subtract(accumulator0, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, offset)));
                            accumulator1 = Sse2.Subtract(accumulator1, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, (offset + 8))));
                            accumulator2 = Sse2.Subtract(accumulator2, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, (offset + 16))));
                            accumulator3 = Sse2.Subtract(accumulator3, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, (offset + 24))));
                            loopIndex++;
                            offset += 32;
                        } while (loopIndex < loopLimit);

                        lengthToExamine -= (32 * loopLimit);
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator0.AsByte(), Vector128<byte>.Zero).AsInt64());
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator1.AsByte(), Vector128<byte>.Zero).AsInt64());
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator2.AsByte(), Vector128<byte>.Zero).AsInt64());
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(accumulator3.AsByte(), Vector128<byte>.Zero).AsInt64());
                    } while (31 < lengthToExamine);

                    result += (sum.GetElement(0) + sum.GetElement(1));
                }

                if (7 < lengthToExamine)
                {
                    var sum = Vector128<long>.Zero;

                    do
                    {
                        sum = Sse2.Add(sum, Sse2.SumAbsoluteDifferences(Sse2.Subtract(Vector128<ushort>.Zero, Sse2.CompareEqual(searchMask, LoadVector128(ref searchSpace, offset))).AsByte(), Vector128<byte>.Zero).AsInt64());
                        lengthToExamine -= 8;
                        offset += 8;
                    } while (7 < lengthToExamine);

                    result += (sum.GetElement(0) + sum.GetElement(1));
                }

                if (offset < length)
                {
                    lengthToExamine = (length - offset);

                    goto SequentialScan;
                }
            }
        }

        return ((int)result);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe nuint UnalignedCountVector128(ref byte searchSpace)
    {
        nint unaligned = ((nint)Unsafe.AsPointer(ref searchSpace) & (Vector128<byte>.Count - 1));

        return ((nuint)(uint)((Vector128<byte>.Count - unaligned) & (Vector128<byte>.Count - 1)));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe nint UnalignedCountVector128(ref char searchSpace)
    {
        const int ElementsPerByte = (sizeof(ushort) / sizeof(byte));

        return ((nint)(uint)(-(int)Unsafe.AsPointer(ref searchSpace) / ElementsPerByte) & (Vector128<ushort>.Count - 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int OccurrencesOf(this ReadOnlySpan<byte> span, byte value) =>
        OccurrencesOf(
            length: span.Length,
            searchSpace: ref MemoryMarshal.GetReference(span),
            value: value
        );
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int OccurrencesOf(this Span<byte> span, byte value) =>
        ((ReadOnlySpan<byte>)span).OccurrencesOf(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int OccurrencesOf(this ReadOnlySpan<char> span, char value) =>
        OccurrencesOf(
            length: span.Length,
            searchSpace: ref MemoryMarshal.GetReference(span),
            value: value
        );
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int OccurrencesOf(this Span<char> span, char value) =>
        ((ReadOnlySpan<char>)span).OccurrencesOf(value);
}

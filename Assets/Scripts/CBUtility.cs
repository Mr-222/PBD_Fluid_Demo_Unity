using System;
using System.Collections.Generic;
using UnityEngine;

public static class CBUtility
{
    public static void Release(ComputeBuffer buffer)
    {
        buffer?.Release();
    }

    public static void Release(IList<ComputeBuffer> buffers)
    {
        if (buffers == null)
            return;
        
        int count = buffers.Count;
        for (int i = 0; i < count; i++)
        {
            Release(buffers[i]);
            buffers[i] = null;
        }
    }

    public static void Swap(ComputeBuffer[] buffers)
    {
        if (buffers.Length != 2)
            throw new ArgumentException("Swap method requires exactly 2 buffers.");

        (buffers[0], buffers[1]) = (buffers[1], buffers[0]);
    }
}
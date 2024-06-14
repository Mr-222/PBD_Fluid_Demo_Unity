using System;
using System.Collections.Generic;
using UnityEngine;

public static class CBUtility
{
    public static void Release(ComputeBuffer buffer)
    {
        if (buffer == null)
            return;
        buffer.Release();
    }

    public static void Release(IList<ComputeBuffer> buffers)
    {
        if (buffers == null)
            return;
        
        int count = buffers.Count;
        for (int i = 0; i < count; i++)
        {
            if (buffers[i] == null) 
                continue;
            buffers[i].Release();
            buffers[i] = null;
        }
    }

    public static void Swap(ComputeBuffer[] buffers)
    {
        if (buffers.Length != 2)
            throw new ArgumentException("Swap method requires exactly 2 buffers.");
        
        ComputeBuffer tmp = buffers[0];
        buffers[0] = buffers[1];
        buffers[1] = tmp;
    }
}
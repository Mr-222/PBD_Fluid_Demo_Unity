// https://forum.unity.com/threads/parallel-prefix-sum-computeshader.518397/
// Example : 
// [SerializeField] ComputeShader scanOperations;
// ScanHelper mScanHelper;
//  
// mScanHelper.InclusiveScan(N, scanOperations, inputs, outputs);

using System.Collections.Generic;
using UnityEngine;

struct ScanHelper
{
    private const int threadsPerGroup = 512;   // THREADS_PER_GROUP in ScanOperations.compute
    private int size;
    private List<ComputeBuffer> group_buffer;
    private List<int> work_size;

    private int NUM_GROUPS(int num, int threadsInOneGroup)
    {
        return Mathf.CeilToInt(num / (float)threadsInOneGroup);
    }
 
    public void InclusiveScan(int num, ComputeShader scanOperations, ComputeBuffer inputs, ComputeBuffer outputs)
    {
        RequireBuffer(num);
 
        // 1. Per group scan
        int kernelScan = scanOperations.FindKernel("ScanInGroupsInclusive");
        scanOperations.SetInt("N", num);
        scanOperations.SetBuffer(kernelScan, "InputBufR", inputs);
        scanOperations.SetBuffer(kernelScan, "OutputBufW", outputs);
        scanOperations.Dispatch(kernelScan, NUM_GROUPS(num, threadsPerGroup), 1, 1);
 
        if (num < threadsPerGroup)
            return;
 
        int kernelScanSums = scanOperations.FindKernel("ScanSums");
        int kernelAdd = scanOperations.FindKernel("AddScannedSums");
 
        // 2. Scan per group sum
        scanOperations.SetInt("N", num);
        scanOperations.SetBuffer(kernelScanSums, "InputBufR", outputs);
        scanOperations.SetBuffer(kernelScanSums, "OutputBufW", this.group_buffer[0]);
        scanOperations.Dispatch(kernelScanSums, NUM_GROUPS(this.work_size[0], threadsPerGroup), 1, 1);
 
        // Continue down the pyramid
        for (int l = 0; l < this.group_buffer.Count - 1; ++l)
        {
            int work_sz = this.work_size[l];
            // 2. Scan per group sum
            scanOperations.SetInt("N", work_sz);
            scanOperations.SetBuffer(kernelScanSums, "InputBufR", this.group_buffer[l]);
            scanOperations.SetBuffer(kernelScanSums, "OutputBufW", this.group_buffer[l+1]);
            scanOperations.Dispatch(kernelScanSums, NUM_GROUPS(this.work_size[l+1], threadsPerGroup), 1, 1);
        }
 
        for (int l = this.group_buffer.Count - 1; l > 0; --l)
        {
            int work_sz = this.work_size[l - 1];
            // 3. Add scanned group sum
            scanOperations.SetInt("N", work_sz);
            scanOperations.SetBuffer(kernelAdd, "InputBufR", this.group_buffer[l]);
            scanOperations.SetBuffer(kernelAdd, "OutputBufW", this.group_buffer[l - 1]);
            scanOperations.Dispatch(kernelAdd, NUM_GROUPS(work_sz, threadsPerGroup), 1, 1);
        }
 
        // 3. Add scanned group sum
        scanOperations.SetInt("N", num);
        scanOperations.SetBuffer(kernelAdd, "InputBufR", this.group_buffer[0]);
        scanOperations.SetBuffer(kernelAdd, "OutputBufW", outputs);
        scanOperations.Dispatch(kernelAdd, this.work_size[0], 1, 1);
    }
 
    private void RequireBuffer(int alloc_sz)
    {
        if (this.size < alloc_sz)
        {
            this.Release();
            this.size = (int)(alloc_sz * 1.5);
            this.group_buffer = new List<ComputeBuffer>();
            this.work_size = new List<int>();
 
            int work_sz = this.size;
            while (work_sz > threadsPerGroup)
            {
                work_sz = NUM_GROUPS(work_sz, threadsPerGroup);
                this.group_buffer.Add(new ComputeBuffer(work_sz, sizeof(uint)));
                this.work_size.Add(work_sz);
            }
        }
    }
 
    public void Release()
    {
        if (group_buffer != null)
        {
            foreach (ComputeBuffer buffer in group_buffer)
                if (buffer != null)
                    buffer.Dispose();
            group_buffer = null;
        }
    }
}
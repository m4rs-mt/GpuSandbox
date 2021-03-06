﻿using System.Diagnostics;
using Alea;
using Alea.CSharp;

#if DOUBLE_PRECISION
    using Real = System.Double;
#else
    using Real = System.Single;
#endif

namespace AleaSandbox.Benchmarks
{
    internal static class AddVector
    {
        public static void Initialise(Real[] matrix, Real[] vector, int m, int n)
        {
            var counter = 0;

            for (int i = 0; i != m; ++i)
                for (int j = 0; j != n; ++j)
                    matrix[i * n + j] = counter++;

            for (int j = 0; j != n; ++j)
                vector[j] = j;
        }

        public static void Managed(Real[] matrix, Real[] vector, int m, int n)
        {
            var timer = Stopwatch.StartNew();

            for (int i = 0; i != m; ++i)
                for (int j = 0; j != n; ++j)
                    matrix[i * n + j] += vector[j];

            Util.PrintPerformance(timer, "AddVector.Managed", 3, m, n);
        }

        public static void Cuda(Real[] matrix, Real[] vector, int m, int n)
        {
            var gpu = Gpu.Default;

            using (var cudaMatrix = gpu.AllocateDevice(matrix))
            using (var cudaVector = gpu.AllocateDevice(vector))
            {
                var timer = Stopwatch.StartNew();

                var gridSizeX = Util.DivUp(n, 32);
                var gridSizeY = Util.DivUp(m, 8);
                var lp = new LaunchParam(new dim3(gridSizeX, gridSizeY), new dim3(32, 8));

                gpu.Launch(CudaKernel, lp, cudaMatrix.Ptr, cudaVector.Ptr, m, n);

                gpu.Synchronize();
                Util.PrintPerformance(timer, "AddVector.Cuda", 3, m, n);

                Gpu.Copy(cudaMatrix, matrix);
            }
        }

        private static void CudaKernel(deviceptr<Real> matrix, deviceptr<Real> vector, int m, int n)
        {
            var i = blockIdx.y * blockDim.y + threadIdx.y;
            var j = blockIdx.x * blockDim.x + threadIdx.x;

            if (i < m && j < n)
            {
                matrix[i * n + j] += vector[j];
            }
        }
    }
}

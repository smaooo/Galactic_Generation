//using System;
//using System.Diagnostics;
//using System.Runtime.InteropServices;
//using System.Threading;
//using Unity.Burst;
//using Unity.Collections;
//using Unity.Collections.LowLevel.Unsafe;
//using Unity.Jobs;
//using ProceduralMeshes;

//namespace CustomNativeContainer
//{
//    [NativeContainerSupportsMinMaxWriteRestriction]
//    [NativeContainerSupportsDeallocateOnJobCompletion]
//    [NativeContainer]
//    // Ensure our memory layout is the same as the order of our variables.
//    [StructLayout(LayoutKind.Sequential)]
//    public unsafe struct NativeNoiseLayers : IDisposable
//    {
//        [NativeDisableUnsafePtrRestriction]
//        internal void* mBuffer;
//        internal int mLength;


//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//        internal int m_MinIndex;
//        internal int m_MaxIndex;
//        internal AtomicSafetyHandle mSaftey;

//        [NativeSetClassTypeToNullOnSchedule]
//        internal DisposeSentinel mDisposeSentinel;
//        static int s_staticSafetyId;

//        [BurstDiscard]
//        static void AssignStaticSafteyId(ref AtomicSafetyHandle safetyHandle)
//        {
//            if (s_staticSafetyId == 0)
//            {
//                s_staticSafetyId = AtomicSafetyHandle.NewStaticSafetyId<NativeNoiseLayers>();
//                //AtomicSafetyHandle.SetCustomErrorMessage(s_staticSafetyId, AtomicSafetyErrorType.DeallocatedFromJob,
//                //"The {5} has been deallocated before being passed into a job. For NativeCustomArrays, this usually means <type-specific guidance here>");
//            }
//            AtomicSafetyHandle.SetStaticSafetyId(ref safetyHandle, s_staticSafetyId);

//        }
//#endif

//        internal Allocator mAllocatorLabel;

//        public NativeNoiseLayers(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
//        {
//            Allocate(length, allocator, out this);

//            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
//            {
//                UnsafeUtility.MemClear(mBuffer, (long)length * UnsafeUtility.SizeOf<NoiseLayer>());
//            }
        
//        }

//        public unsafe JobHandle Dispose(JobHandle inputDeps)
//        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//            DisposeSentinel.Clear(ref mDisposeSentinel);

//#endif

//            NativeCustomArrayDisposeJob disposeJob = new NativeCustomArrayDisposeJob()
//            {
//                Data = new NativeCustomArrayDispose()
//                {
//                    mBuffer = mBuffer,
//                    mAllocatorLabel = mAllocatorLabel
//                }
//            };
//            JobHandle result = disposeJob.Schedule(inputDeps);

//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            AtomicSafetyHandle.Release(mSaftey);

//#endif

//            mBuffer = null;
//            mLength = 0;

//            return result;

//        }

//        [NativeContainer]
//        internal unsafe struct NativeCustomArrayDispose
//        {
//            [NativeDisableUnsafePtrRestriction]
//            internal void* mBuffer;
//            internal Allocator mAllocatorLabel;

//            public void Dispose()
//            {
//                UnsafeUtility.Free(mBuffer, mAllocatorLabel);
//            }
//        }

//        [BurstCompile]
//        internal struct NativeCustomArrayDisposeJob : IJob
//        {
//            internal NativeCustomArrayDispose Data;
//            public void Execute()
//            {
//                Data.Dispose();
//            }
//        }



//        static void Allocate(int length, Allocator allocator, out NativeNoiseLayers layers)
//        {
//            long size = UnsafeUtility.SizeOf<NoiseLayer>() * (long)length;

//#if ENABLE_UNITY_COLLECTIONS_CHECKS
//            if (allocator <= Allocator.None)
//                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));

//            if (length < 0)
//                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");

//            if (size > int.MaxValue)
//                throw new ArgumentOutOfRangeException(nameof(length), $"Length * sizeof(NoiseLayer) cannot exceed {(object)int.MaxValue} bytes");

//#endif
//            layers = default(NativeNoiseLayers);

//            layers.mBuffer = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<NoiseLayer>(), allocator);
//            layers.mLength = length;
//            layers.mAllocatorLabel = allocator;

//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//            layers.m_MinIndex = 0;
//            layers.m_MaxIndex = length - 1;

//            DisposeSentinel.Create(out layers.mSaftey, out layers.mDisposeSentinel, 1, allocator);

//#endif
//        }


//        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
//        private void CheckRangeAccess(int index)
//        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//            if (index < m_MinIndex || index > m_MaxIndex)
//            {
//                if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
//                    throw new IndexOutOfRangeException(string.Format("Index {0} is out of restricted IJobParallelFor range [{1}...{2}] in ReadWriteBuffer.\n" +
//                    "ReadWriteBuffers are restricted to only read & write the element at the job index. " +
//                    "You can use double buffering strategies to avoid race conditions due to " +
//                    "reading & writing in parallel to the same elements from a job.",
//                    index, m_MinIndex, m_MaxIndex));

                
//                throw new IndexOutOfRangeException(string.Format("Index {0} is out of range of '{1}' Length.", index, Length));
//            }
//#endif

//        }

//        [NativeContainerIsAtomicWriteOnly]
//        [NativeContainer]
//        public unsafe struct ParallelWrite
//        {
//            [NativeDisableUnsafePtrRestriction]
//            internal void* mBuffer;
//            internal int mLength;

//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//            internal AtomicSafetyHandle mSaftey;
//#endif
//            public int Length => mLength;
//        }

//        public ParallelWrite AsParallelWriter()
//        {
//            ParallelWrite writer;

//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//            AtomicSafetyHandle.CheckWriteAndThrow(mSaftey);
//            writer.mSaftey = mSaftey;
//            AtomicSafetyHandle.UseSecondaryVersion(ref writer.mSaftey);
//#endif

//            writer.mBuffer = mBuffer;
//            writer.mLength = mLength;
//            return writer;
//        }
//        public NoiseLayer this[int index]
//        {
//            get
//            {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//                AtomicSafetyHandle.CheckReadAndThrow(mSaftey);
//#endif
//                CheckRangeAccess(index);
//                return UnsafeUtility.ReadArrayElement<NoiseLayer>(mBuffer, index);

//            }

//            [WriteAccessRequired]
//            set
//            {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//                AtomicSafetyHandle.CheckWriteAndThrow(mSaftey);
//#endif
//                CheckRangeAccess(index);
//                UnsafeUtility.WriteArrayElement(mBuffer, index, value);
//            }
//        }

//        public int Length => mLength;

//        public void Dispose()
//        {
//#if ENABLE_UNITY_COLLECTIONS_CHECKS

//            if (!UnsafeUtility.IsValidAllocator(mAllocatorLabel))
//                throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");

//            DisposeSentinel.Dispose(ref mSaftey, ref mDisposeSentinel);

//#endif

//            UnsafeUtility.Free(mBuffer, mAllocatorLabel);
//            mBuffer = null;
//            mLength = 0;
//        }
//    }
//}
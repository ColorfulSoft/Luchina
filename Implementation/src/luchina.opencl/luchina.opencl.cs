﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2025. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public static unsafe partial class luchina
{

    public static unsafe partial class opencl
    {

        private static bool _initialized = false;

        private static CLPlatformID[] _platforms;

        private static Dictionary<CLPlatformID, CLDeviceID[]> _devices;

        private static Dictionary<CLDeviceID, CLContext> _contexts;

        private static Dictionary<CLDeviceID, CLCommandQueue> _queues;

        private static luchina.Device _current_device;

        /// <summary>
        /// Return a bool indicating if CUDA is currently available.
        /// </summary>
        public static bool is_available
        {

            get;

            private set;

        }

        /// <summary>
        /// Gets or sets a currently selected opencl device.
        /// </summary>
        public static luchina.Device current_device
        {

            get
            {
                if(!luchina.opencl.is_available)
                {
                    throw new NotSupportedException("OpenCL is not available.");
                }
                return luchina.opencl._current_device;
            }

            set
            {
                if(!luchina.opencl.is_available)
                {
                    throw new NotSupportedException("OpenCL is not available.");
                }
                if(value.type != "opencl")
                {
                    throw new ArgumentException("The device must be \"opencl\".");
                }
                luchina.opencl._current_device = value;
            }

        }

        /// <summary>
        /// Initialize Luchina’s OpenCL state. Ordinary users should not need this, as all of Luchina’s OpenCL
        /// methods automatically initialize OpenCL state on-demand. Does nothing if the OpenCL state is already
        /// initialized.
        /// </summary>
        public static void init()
        {
            if(!luchina.opencl._initialized)
            {
                luchina.opencl._initialized = true;
                //--------------------------------------------------
                // Get a list of available platforms.
                //--------------------------------------------------
                uint num_platforms = 0;
                CLError err;
                // If OpenCL is unavailable (the dll is missing),
                // the first call to the OpenCL API function will throw a DllNotFoundException.
                try
                {
                    // Get the number of available platforms.
                    err = OpenCL.clGetPlatformIDs(0, IntPtr.Zero, ref num_platforms);
                    // If something went wrong, set is_available to false and complete initialization.
                    if(err != CLError.Success)
                    {
                        luchina.opencl.is_available = false;
                        return;
                    }
                }
                catch
                {
                    luchina.opencl.is_available = false;
                    return;
                }
                // Get the actual platforms once we know their amount.
                luchina.opencl._platforms = new CLPlatformID[num_platforms];
                err = OpenCL.clGetPlatformIDs(num_platforms, luchina.opencl._platforms, ref num_platforms);
                if(err != CLError.Success)
                {
                    luchina.opencl.is_available = false;
                    luchina.opencl._platforms = null;
                    return;
                }
                //--------------------------------------------------
                // Get a list of available devices for each found platform.
                //--------------------------------------------------
                luchina.opencl._devices = new Dictionary<CLPlatformID, CLDeviceID[]>();
                for(uint i = 0; i < num_platforms; ++i)
                {
                    // Get the number of available devices.
                    uint num_devices = 0;
                    CLPlatformID platform = luchina.opencl._platforms[i];
                    err = OpenCL.clGetDeviceIDs(platform, CLDeviceType.All, 0, IntPtr.Zero, ref num_devices);
                    if((err != CLError.Success) || (num_devices < 1))
                    {
                        luchina.opencl._devices.Add(platform, new CLDeviceID[0]);
                        continue;
                    }
                    // Get the actual devices once we know their amount.
                    CLDeviceID[] devices = new CLDeviceID[num_devices];
                    err = OpenCL.clGetDeviceIDs(platform, CLDeviceType.All, num_devices, devices, ref num_devices);
                    luchina.opencl._devices.Add(platform, devices);
                }
                //--------------------------------------------------
                // Create objects to store contexts and command queues for each device.
                //--------------------------------------------------
                luchina.opencl._contexts = new Dictionary<CLDeviceID, CLContext>();
                luchina.opencl._queues = new Dictionary<CLDeviceID, CLCommandQueue>();
                // If we got here, then we consider opencl to be available.
                luchina.opencl.is_available = true;
                // Setting the default values
                luchina.opencl._current_device = luchina.device("opencl:0:0");
            }
        }

        /// <summary>
        /// Returns requested information about a device.
        /// </summary>
        /// <param name="device">Device ID to query.</param>
        /// <param name="info">Requested information.</param>
        /// <returns>Value which depends on the type of information requested.</returns>
        private static object _get_device_info(CLDeviceID device, CLDeviceInfo info)
        {
            CLError err = CLError.Success;
            // Define variables to store native information.
            SizeT param_value_size_ret = 0;
            IntPtr ptr = IntPtr.Zero;
            object result = null;
            // Get initial size of buffer to allocate.
            err = OpenCL.clGetDeviceInfo(device, info, 0, IntPtr.Zero, ref param_value_size_ret);
            if(err != CLError.Success)
            {
                throw new luchina.opencl.OpenCLException(err);
            }
            if(param_value_size_ret < 1)
            {
                return result;
            }
            // Allocate native memory to store value.
            ptr = Marshal.AllocHGlobal(param_value_size_ret);
            // Protect following statements with try-finally in case something 
            // goes wrong.
            try
            {
                // Get actual value.
                err = OpenCL.clGetDeviceInfo(device, info,
                param_value_size_ret, ptr, ref param_value_size_ret);
                switch(info)
                {
                    case CLDeviceInfo.Type:
                    {
                        result = (CLDeviceType)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.VendorID:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxComputeUnits:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxWorkItemDimensions:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxWorkGroupSize:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.MaxWorkItemSizes:
                    {
                        uint dims = (uint)_get_device_info(device, CLDeviceInfo.MaxWorkItemDimensions);
                        SizeT[] sizes = new SizeT[dims];
                        for(int i = 0; i < dims; i++)
                        {
                            sizes[i] = new SizeT(Marshal.ReadIntPtr(ptr, i * IntPtr.Size).ToInt64());
                        }
                        result = sizes;
                        break;
                    }
                    case CLDeviceInfo.PreferredVectorWidthChar:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.PreferredVectorWidthShort:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.PreferredVectorWidthInt:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.PreferredVectorWidthLong:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.PreferredVectorWidthFloat:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.PreferredVectorWidthDouble:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxClockFrequency:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.AddressBits:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxReadImageArgs:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxWriteImageArgs:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxMemAllocSize:
                    {
                        result = (ulong)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.Image2DMaxWidth:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.Image2DMaxHeight:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.Image3DMaxWidth:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.Image3DMaxHeight:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.Image3DMaxDepth:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.ImageSupport:
                    {
                        result = (CLBool)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxParameterSize:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.MaxSamplers:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MemBaseAddrAlign:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.MinDataTypeAlignSize:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.SingleFPConfig:
                    {
                        result = (CLDeviceFPConfig)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.GlobalMemCacheType:
                    {
                        result = (CLDeviceMemCacheType)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.GlobalMemCacheLineSize:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.GlobalMemCacheSize:
                    {
                        result = (ulong)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.GlobalMemSize:
                    {
                        result = (ulong)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxConstantBufferSize:
                    {
                        result = (ulong)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.MaxConstantArgs:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.LocalMemType:
                    {
                        result = (CLDeviceLocalMemType)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.LocalMemSize:
                    {
                        result = (ulong)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.ErrorCorrectionSupport:
                    {
                        result = (CLBool)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.ProfilingTimerResolution:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(SizeT));
                        break;
                    }
                    case CLDeviceInfo.EndianLittle:
                    {
                        result = (CLBool)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.Available:
                    {
                        result = (CLBool)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.CompilerAvailable:
                    {
                        result = (CLBool)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.ExecutionCapabilities:
                    {
                        result = (CLDeviceExecCapabilities)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.QueueProperties:
                    {
                        result = (CLCommandQueueProperties)Marshal.ReadInt64(ptr);
                        break;
                    }
                    case CLDeviceInfo.Name:
                    {
                        result = Marshal.PtrToStringAnsi(ptr, param_value_size_ret);
                        break;
                    }
                    case CLDeviceInfo.Vendor:
                    {
                        result = Marshal.PtrToStringAnsi(ptr, param_value_size_ret);
                        break;
                    }
                    case CLDeviceInfo.DriverVersion:
                    {
                        result = Marshal.PtrToStringAnsi(ptr, param_value_size_ret);
                        break;
                    }
                    case CLDeviceInfo.Profile:
                    {
                        result = Marshal.PtrToStringAnsi(ptr, param_value_size_ret);
                        break;
                    }
                    case CLDeviceInfo.Version:
                    {
                        result = Marshal.PtrToStringAnsi(ptr, param_value_size_ret);
                        break;
                    }
                    case CLDeviceInfo.Extensions:
                    {
                        result = Marshal.PtrToStringAnsi(ptr, param_value_size_ret);
                        break;
                    }
                    case CLDeviceInfo.Platform:
                    {
                        result = Marshal.PtrToStructure(ptr, typeof(CLPlatformID));
                        break;
                    }
                    case CLDeviceInfo.PreferredVectorWidthHalf:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.HostUnifiedMemory:
                    {
                        result = (CLBool)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.NativeVectorWidthChar:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.NativeVectorWidthShort:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.NativeVectorWidthInt:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.NativeVectorWidthLong:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.NativeVectorWidthFloat:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.NativeVectorWidthDouble:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.NativeVectorWidthHalf:
                    {
                        result = (uint)Marshal.ReadInt32(ptr);
                        break;
                    }
                    case CLDeviceInfo.OpenCLCVersion:
                    {
                        result = Marshal.PtrToStringAnsi(ptr, param_value_size_ret);
                        break;
                    }
                }
            }
            finally
            {
                // Free native buffer.
                Marshal.FreeHGlobal(ptr);
            }
            return result;
        }

        /// <summary>
        /// Get the name of a device.
        /// </summary>
        /// <param name="device">
        /// luchina.device or str, optional – device for which to return the name.
        /// It uses the current device, given by current_device, if device is null (default).</param>
        /// <returns>The name of a device.</returns>
        public static string get_device_name(object device = null)
        {
            if(device == null)
            {
                device = luchina.opencl.current_device;
            }
            if(device is string)
            {
                device = luchina.device((string)device);
            }
            if(device is luchina.Device)
            {
                luchina.Device dev = (luchina.Device)device;
                if(dev.type != "opencl")
                {
                    throw new ArgumentException("device should be opencl", "device");
                }
                if(dev.platform >= luchina.opencl._platforms.Length)
                {
                    throw new ArgumentException("Platform index is out of available range", "device");
                }
                CLPlatformID platform = luchina.opencl._platforms[dev.platform];
                if(dev.index >= luchina.opencl._devices[platform].Length)
                {
                    throw new ArgumentException("Device index is out of available range", "device");
                }
                CLDeviceID device_id = luchina.opencl._devices[platform][dev.index];
                return (string)_get_device_info(device_id, CLDeviceInfo.Name);
            }
            throw new ArgumentException("device should be of type luchina.Device or string", "device");
        }

        /// <summary>
        /// Get the OpenCL capability of a device.
        /// </summary>
        /// <param name="device">
        /// luchina.device or str, optional – device for which to return the device capability.
        /// It uses the current device, given by current_device, if device is null (default).</param>
        /// <returns>The OpenCL version.</returns>
        public static string get_device_capability(object device = null)
        {
            if(device == null)
            {
                device = luchina.opencl.current_device;
            }
            if(device is string)
            {
                device = luchina.device((string)device);
            }
            if(device is luchina.Device)
            {
                luchina.Device dev = (luchina.Device)device;
                if(dev.type != "opencl")
                {
                    throw new ArgumentException("device should be opencl", "device");
                }
                if(dev.platform >= luchina.opencl._platforms.Length)
                {
                    throw new ArgumentException("Platform index is out of available range", "device");
                }
                CLPlatformID platform = luchina.opencl._platforms[dev.platform];
                if(dev.index >= luchina.opencl._devices[platform].Length)
                {
                    throw new ArgumentException("Device index is out of available range", "device");
                }
                CLDeviceID device_id = luchina.opencl._devices[platform][dev.index];
                return (string)_get_device_info(device_id, CLDeviceInfo.Version);
            }
            throw new ArgumentException("device should be of type luchina.Device or string", "device");
        }

        /// <summary>
        /// Performs automatic initialization of luchina.opencl.
        /// </summary>
        static opencl()
        {
            luchina.opencl.init();
        }

    }

}
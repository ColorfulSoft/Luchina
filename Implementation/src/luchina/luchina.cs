﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2025. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

using System;
using System.Reflection;

public static unsafe partial class luchina
{

    #region Luchina

    /// <summary>
    /// Current luchina version.
    /// </summary>
    public static Version version
    {

        get
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

    }

    /// <summary>
    /// Performs initialization of the luchina module.
    /// </summary>
    static luchina()
    {
        default_dtype = float32;
    }

    #endregion

    #region Device

    /// <summary>
    /// Returns a Device object containing information about the computing device provided in the device string.
    /// The string device has the &lt;type&gt;:&lt;platform&gt;:&lt;index&gt; format, where type is the device
    /// type ("clr" or "opencl"), platform is an eight-bit unsigned integer representing the platform number,
    /// index is an eight-bit unsigned integer representing the device number within the platform. The platform and
    /// index parameters are optional (default values are 0), explicit indication of these values for the "clr"
    /// device is not allowed. "clr" - a managed backend is used (the clr is responsible for calculations, in most
    /// cases calculations are performed on the CPU), "opencl" - an OpenCL-compatible device (CPU or GPU) is used for
    /// calculations.
    /// </summary>
    /// <param name="device">Identifier of the computing device in the string representation.</param>
    /// <returns>Device object containing information about the computing device provided in the device string.</returns>
    /// <exception cref="System.FormatException">Incorrect format of the device string.</exception>
    /// <example>
    /// <code>
    /// var device = (luchina.opencl.is_available()) ? luchina.device("opencl:0:1") : luchina.device("clr")
    /// </code>
    /// </example>
    public static Device device(string device)
    {
        return new Device(device);
    }

    #endregion

    #region DType

    /// <summary>
    /// 32-bit floating point. Alias for luchina.float.
    /// </summary>
    public const DType float32 = DType.float32;

    /// <summary>
    /// 32-bit floating point. Alias for luchina.float32.
    /// </summary>
    public const DType @float = DType.float32;

    /// <summary>
    /// 32-bit signed integer. Alias for luchina.int.
    /// </summary>
    public const DType int32 = DType.int32;

    /// <summary>
    /// 32-bit signed integer. Alias for luchina.int32.
    /// </summary>
    public const DType @int = DType.int32;

    /// <summary>
    /// Boolean.
    /// </summary>
    public const DType @bool = DType.@bool;

    /// <summary>
    /// Gets or sets the default data type for tensors.
    /// </summary>
    public static DType default_dtype
    {

        get;

        set;

    }

    /// <summary>
    /// Returns the size of the data type value in bytes.
    /// </summary>
    /// <param name="dtype">Data type.</param>
    /// <returns>Size in bytes.</returns>
    public static int element_size(this DType dtype)
    {
        switch(dtype)
        {
            case @bool:
            {
                return 1;
            }
            case float32:
            case int32:
            {
                return 4;
            }
            default:
            {
                throw new TypeAccessException(string.Format("Unknown data type with code {0}.", (byte)dtype));
            }
        }
    }

    /// <summary>
    /// Checks whether the specified data type is a boolean.
    /// </summary>
    /// <param name="dtype">Data type.</param>
    /// <returns>true, if dtype is boolean.</returns>
    public static bool is_boolean(this DType dtype)
    {
        return dtype == @bool;
    }

    /// <summary>
    /// Checks whether the specified data type is a floating point.
    /// </summary>
    /// <param name="dtype">Data type.</param>
    /// <returns>true, if dtype is floating point.</returns>
    public static bool is_floating_point(this DType dtype)
    {
        return dtype == float32;
    }

    /// <summary>
    /// Checks whether the specified data type is an integer.
    /// </summary>
    /// <param name="dtype">Data type.</param>
    /// <returns>true, if dtype is integer.</returns>
    public static bool is_integer(this DType dtype)
    {
        return dtype == int32;
    }

    #endregion

}
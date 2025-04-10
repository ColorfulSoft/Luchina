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

}
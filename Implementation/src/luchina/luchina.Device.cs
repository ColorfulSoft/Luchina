﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2025. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

using System;

public static unsafe partial class luchina
{

    /// <summary>
    /// Identifier of the computing device.
    /// </summary>
    public sealed class Device
    {

        /// <summary>
        /// The type of computing device ('clr' or 'opencl').
        /// </summary>
        public string type
        {

            get;

            private set;

        }

        /// <summary>
        /// The platform index (for opencl only).
        /// </summary>
        public byte platform
        {

            get;

            private set;

        }

        /// <summary>
        /// The device's index within the platform (opencl only).
        /// </summary>
        public byte index
        {

            get;

            private set;

        }

        /// <summary>
        /// Initializes the Device object using the string representation of the device in the
        /// &lt;type&gt;:&lt;platform&gt;:&lt;index&gt; format. The platform and index fields are set as an
        /// unsigned 8-bit integer and are optional. Specifying platform and index is allowed only with
        /// type=="opencl". To create a device identifier object, use the luchina.device(string) method
        /// instead of explicitly calling this constructor.
        /// </summary>
        /// <param name="device">Identifier of the computing device in the string representation.</param>
        /// <exception cref="System.FormatException">Incorrect format of the device string.</exception>
        /// <example>
        /// <code>
        /// var device = (luchina.opencl.is_available()) ? new luchina.Device("opencl:0:1") : new luchina.Device("clr")
        /// </code>
        /// </example>
        public Device(string device)
        {
            string[] parts = device.Split(':');
            if((parts[0] != "clr") && (parts[0] != "opencl"))
            {
                throw new FormatException("Expected one of clr, opencl device type at start of device string: '" + parts[0] + "'");
            }
            this.type = parts[0];
            if(parts.Length > 1)
            {
                if((parts.Length != 3) || (this.type == "clr"))
                {
                    throw new FormatException("Invalid device string: '" + device + "'");
                }
                byte platform;
                if(!byte.TryParse(parts[1], out platform))
                {
                    throw new FormatException("Invalid device string: '" + device + "'");
                }
                this.platform = platform;
                byte index;
                if(!byte.TryParse(parts[2], out index))
                {
                    throw new FormatException("Invalid device string: '" + device + "'");
                }
                this.index = index;
            }
        }

        /// <summary>
        /// Compares two objects of the Device class, returning true if they are identical and false otherwise.
        /// </summary>
        /// <param name="device1">The object to the left of the operator.</param>
        /// <param name="device2">The object to the right of the operator.</param>
        /// <returns>The value is true if the operands are equal, false otherwise.</returns>
        public static bool operator ==(Device device1, Device device2)
        {
            return (device1.type == device2.type) &&
                   (device1.platform == device2.platform) &&
                   (device1.index == device2.index);
        }

        /// <summary>
        /// Compares two objects of the Device class, returning false if they are identical and true otherwise.
        /// </summary>
        /// <param name="device1">The object to the left of the operator.</param>
        /// <param name="device2">The object to the right of the operator.</param>
        /// <returns>The value is false if the operands are equal, true otherwise.</returns>
        public static bool operator !=(Device device1, Device device2)
        {
            return (device1.type != device2.type) ||
                   (device1.platform != device2.platform) ||
                   (device1.index != device2.index);
        }

        /// <summary>
        /// Compares the current object of the Device class with the obj object.
        /// If obj is an object of the Device class equal to the current one, it returns
        /// true, otherwise it returns false.
        /// </summary>
        /// <param name="obj">The object to be compared with.</param>
        /// <returns>The value is true if obj is an instance of the Device class equal to the current one, otherwise false.</returns>
        public override bool Equals(object obj)
        {
            Device other = obj as Device;
            if(other == null)
            {
                return false;
            }
            return (this.type == other.type) && (this.platform == other.platform) && (this.index == other.index);
        }

        /// <summary>
        /// Returns the hash code for the current object of the Device class.
        /// </summary>
        /// <returns>Hash code.</returns>
        public override int GetHashCode()
        {
            return (((this.type == "clr") ? 0 : 1) << 16) |
                   (this.platform << 8) |
                   (this.index << 0);
        }

        /// <summary>
        /// Converts the current object of the Device class to a string representation.
        /// </summary>
        /// <returns>A string containing information about the current object.</returns>
        public override string ToString()
        {
            return string.Format("device(type='{0}', platform={1}, index={2})", this.type, this.platform, this.index);
        }

    }

}
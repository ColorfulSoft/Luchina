﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2025. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

using System;

public static unsafe partial class luchina
{

    public static unsafe partial class opencl
    {

        /// <summary>
        /// Represents an OpenCL exception that occured in one of the API functions.
        /// </summary>
        public sealed class OpenCLException : Exception
        {

            /// <summary>
            /// OpenCL error code.
            /// </summary>
            public int ErrorCode
            {

                get;

                private set;

            }

            /// <summary>
            /// Gets OpenCL error string and code.
            /// </summary>
            public override string Message
            {

                get
                {
                    return string.Format("OpenCL error {0} (Code: {1})", (CLError)this.ErrorCode, this.ErrorCode);
                }

            }

            /// <summary>
            /// Creates a new exception instance with given error code.
            /// </summary>
            /// <param name="error">OpenCL error code.</param>
            internal OpenCLException(CLError error)
            {
                this.ErrorCode = (int)error;
            }

        }

    }

}
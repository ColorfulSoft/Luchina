﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2023. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

namespace System.AI.Experimental
{

    public static partial class torchlite
    {

        /// <summary>
        /// Represents the data type supported by torchlite.
        /// </summary>
        public enum DType : byte
        {

            /// <summary>
            /// 32-bit floating point. Alias for float.
            /// </summary>
            float32 = 0,

            /// <summary>
            /// 32-bit floating point. Alias for float32.
            /// </summary>
            @float = 0,

            /// <summary>
            /// 32-bit signed integer. Alias for int.
            /// </summary>
            int32 = 1,

            /// <summary>
            /// 32-bit signed integer. Alias for int32.
            /// </summary>
            @int = 1,

            /// <summary>
            /// Boolean.
            /// </summary>
            @bool = 2

        }

    }

}
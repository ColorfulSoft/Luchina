﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2023. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

namespace System.AI.Experimental
{

    public static partial class torchlite
    {

        /// <summary>
        /// Alias for torch.acos().
        /// </summary>
        /// <param name="input">Input tensor.</param>
        /// <returns>Tensor.</returns>
        public static Tensor arccos(this Tensor input)
        {
            return input.acos();
        }

    }

}
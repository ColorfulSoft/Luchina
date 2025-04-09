﻿//***************************************************************************************************
//* (C) ColorfulSoft corp., 2019-2025. All rights reserved.
//* The code is available under the Apache-2.0 license. Read the License for details.
//***************************************************************************************************

using System;
using System.Reflection;

public static unsafe partial class luchina
{

    public static Version version
    {

        get
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

    }

}
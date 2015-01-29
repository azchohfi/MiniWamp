﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DapperWare
{
    /// <summary>
    /// Gets the type of message sent by the client
    /// </summary>
    public enum MessageType
    {
        WELCOME = 0,
        PREFIX = 1,
        CALL = 2,
        CALLRESULT = 3,
        CALLERROR = 4,
        SUBSCRIBE = 5,
        UNSUBSCRIBE = 6,
        PUBLISH = 7,
        EVENT = 8
    }
}

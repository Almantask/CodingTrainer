﻿using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CodingTrainer.CodingTrainerWeb.Hubs.Helpers
{
    interface IRequiresContext
    {
        HubCallerContext Context { get; set; }
    }
}
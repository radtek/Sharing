﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Sharing.Core.Models
{
    public class SharedContext : ISharedContext
    {
        public long Id { get; set; }

        public string OpenId { get; set; }

        public long? InvitedBy { get; set; }
    }
}
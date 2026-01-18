using System;
using System.Collections.Generic;

namespace Maestro.Models
{
    public class CommunityManifest
    {
        public int Version { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<CommunitySong> Songs { get; set; } = new List<CommunitySong>();
    }
}

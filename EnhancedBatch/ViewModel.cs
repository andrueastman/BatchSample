using System.IO;
using Microsoft.Graph;

namespace EnhancedBatch
{
    public class ViewModel
    {
        // one could probable extend this class and add more properties to it
        public User Me { get; set; }
        public User Manager { get; set; }
        public Stream Photo { get; set; }
    }
}
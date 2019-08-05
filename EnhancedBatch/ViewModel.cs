using Microsoft.Graph;

namespace EnhancedBatch
{
    /// <summary>
    /// Model class for holding data that comes back as a response.
    /// One could probable extend this class and add more properties to it.
    /// </summary>
    public class ViewModel
    {
        public User Me { get; set; }
        public Calendar Calendar { get; set; }
        public Drive Drive { get; set; }
    }
}
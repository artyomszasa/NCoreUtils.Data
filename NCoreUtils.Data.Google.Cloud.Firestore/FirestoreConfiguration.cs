using System.Collections.Generic;
using Google.Cloud.Firestore;

namespace NCoreUtils.Data
{
    public class FirestoreConfiguration
    {
        public string? ProjectId { get; set; }

        public List<object> CustomConverters { get; set; } = new List<object>();
    }
}
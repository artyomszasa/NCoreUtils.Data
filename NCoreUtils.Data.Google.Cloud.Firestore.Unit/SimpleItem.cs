using System;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class SimpleItem : IHasId<string>
    {
        public string Id { get; }

        public string StringValue { get; }

        public int NumValue { get; }

        public double FloatValue { get; }

        public bool BooleanValue { get; }

        public DateTimeOffset DateValue { get; }

        public SimpleItem(string id, string stringValue, int numValue, double floatValue, bool booleanValue, DateTimeOffset dateValue)
        {
            Id = id;
            StringValue = stringValue;
            NumValue = numValue;
            FloatValue = floatValue;
            BooleanValue = booleanValue;
            DateValue = dateValue;
        }
    }
}
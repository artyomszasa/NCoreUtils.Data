using System.Threading.Tasks;
using Xunit;

namespace NCoreUtils.Data.Google.Cloud.Firestore.Unit
{
    public class FirestoreEnumAsSingleNumberTest : FirestoreEnumTestBase
    {
        public FirestoreEnumAsSingleNumberTest() : base(FirestoreEnumHandling.AsSingleNumber, BuildModel, ConfigureFirestore(FirestoreEnumHandling.AsSingleNumber)) { }

        [Fact]
        public override Task StoreAndRead() => base.StoreAndRead();
    }

    public class FirestoreEnumAsNumberOrNumberArrayTest : FirestoreEnumTestBase
    {
        public FirestoreEnumAsNumberOrNumberArrayTest() : base(FirestoreEnumHandling.AsNumberOrNumberArray, BuildModel, ConfigureFirestore(FirestoreEnumHandling.AsNumberOrNumberArray)) { }

        [Fact]
        public override Task StoreAndRead() => base.StoreAndRead();
    }

    public class FirestoreEnumAlwaysAsStringTest : FirestoreEnumTestBase
    {
        public FirestoreEnumAlwaysAsStringTest() : base(FirestoreEnumHandling.AlwaysAsString, BuildModel, ConfigureFirestore(FirestoreEnumHandling.AlwaysAsString)) { }

        [Fact]
        public override Task StoreAndRead() => base.StoreAndRead();
    }

    public class FirestoreEnumAsStringOrStringArrayTest : FirestoreEnumTestBase
    {
        public FirestoreEnumAsStringOrStringArrayTest() : base(FirestoreEnumHandling.AsStringOrStringArray, BuildModel, ConfigureFirestore(FirestoreEnumHandling.AsStringOrStringArray)) { }

        [Fact]
        public override Task StoreAndRead() => base.StoreAndRead();
    }
}
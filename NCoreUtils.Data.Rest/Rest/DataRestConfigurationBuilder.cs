using System;
using System.Collections.Generic;
using NCoreUtils.Rest;

namespace NCoreUtils.Data.Rest
{
    public class DataRestConfigurationBuilder : List<(Type EntityType, Type IdType, IRestClientConfiguration Configuration)>
    {

    }
}
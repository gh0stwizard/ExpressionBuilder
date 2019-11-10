using System;
using System.Collections.Generic;

namespace ExpressionBuilder.Test.Models
{
    public class SimpleList
    {
        public IList<string> Strings { get; set; }
        public IList<int> Integers { get; set; }
        public IList<bool> Booleans { get; set; }
    }
}

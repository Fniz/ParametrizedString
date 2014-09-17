using System.Collections.Generic;

namespace Fniz.ParametrizedString
{
    public class DelimiterIndices
    {
        public List<int> StartDelimitersIndices { get; set; }
        public List<int> EndDelimitersIndices { get; set; }
        public List<int> EscapedDelimitersIndices { get; set; }
    }
}

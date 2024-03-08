using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simulysis.Helpers.SignalSearch
{
    public class SignalSearcher
    {
        /**
         * How to use ?
         * List<SearchResult> = new SignalSearcher(new FromGotoType()).Search(input);
         *
         */
        
        SignalSearchStrategy searchStrategy;
        public SignalSearcher(SignalSearchStrategy searchStrategy)
        {
            this.searchStrategy = searchStrategy;
        }
        public SignalSearcher()
        {
            
        }
        public List<Signal> Search(SearchInput input)
        {
            return searchStrategy.Search(input);
        }
    }
}
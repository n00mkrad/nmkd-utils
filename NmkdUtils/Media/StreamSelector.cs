using System;
using System.Collections.Generic;
using System.Linq;
using NmkdUtils.Media;
using static NmkdUtils.Media.MediaData;
using static NmkdUtils.Media.Stream;

namespace NmkdUtils.Media
{
    public class StreamSelector
    {
        public enum SelectionType { All, FirstN }
        public SelectionType Type { get; set; }
        public bool Blacklist { get; set; } = false;


        public StreamSelector(SelectionType type, bool blacklist = false)
        {
            Type = type;
            Blacklist = blacklist;
        }

        public List<Stream> Apply(List<Stream> streams, List<StreamSelector> selectors)
        {
            foreach (var selector in selectors)
            {
                streams = Apply(streams, selector);
            }

            return streams;
        }

        public List<Stream> Apply(List<Stream> streams, StreamSelector selector)
        {
            if (Type == SelectionType.All)
                return streams;



            return streams;
        }
    }
}

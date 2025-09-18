
namespace NmkdUtils;

public class Enums
{
    public enum ExceptionHandling { Throw, Log, Suppress }
    public enum Sort { None, AToZ, ZToA, Newest, Oldest, Biggest, Smallest, Longest, Shortest, Deepest, Shallowest }
    public enum Position { TopLft = 7, TopCtr = 8, TopRgt = 9, MidLft = 4, MidCtr = 5, MidRgt = 6, BotLft = 1, BotCtr = 2, BotRgt = 3 }
    public enum PathKind { Unknown, Missing, File, Directory }

}

using System.Collections.Generic;

namespace TeamHitori.Mulplay.shared.storage.documents
{
    public record ImageBinaryString(string imageStr, Dictionary<string, string> meta);
}

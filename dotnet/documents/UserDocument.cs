using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeamHitori.Mulplay.shared.storage.documents
{
    public record UserDocument(
                string id,
                string appOwner,
                string userPrincipleId,
                string primaryName,
                string type,
                string content, // null means document has been deleted
                int version, // null means, hasn't been persisted to DB yet
                string lastModified,
                string _etag
            );
}

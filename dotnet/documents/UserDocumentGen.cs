using System;

namespace TeamHitori.Mulplay.shared.storage.documents
{
    public record UserDocumentGen<T>(
                String id,
                String appOwner,
                String userPrincipleId,
                String primaryName,
                String contents, // null means document has been deleted
                int version, // null means, hasn't been persisted to DB yet
                String lastModified,
                String etag
            );

    public static class UserDocumentGenExtensions
    {
        public static UserDocument GetUserDocument<T>(this UserDocumentGen<T> docIn) where T : class
        {
            return new (
                docIn.id,
                docIn.appOwner,
                docIn.userPrincipleId,
                docIn.primaryName,
                typeof(T).FullName,
                docIn.contents,
                (docIn?.version ?? 0) + 1,
                docIn.lastModified,
                docIn.etag);
        }

        public static UserDocumentGen<T> GetUserDocument<T>(this UserDocument docIn) where T : class
        {
            return new (
                docIn.id,
                docIn.appOwner,
                docIn.userPrincipleId,
                docIn.primaryName,
                docIn.content.GetObject<T>() != null? docIn.content : null,
                (docIn?.version ?? 0) + 1,
                docIn.lastModified,
                docIn._etag);
        }

        public static String GetPrimaryKey<T>(this UserDocumentGen<T> docIn) where T : class
        {
            var primaryName = $"[{docIn.userPrincipleId}][{docIn.primaryName}][{typeof(T).FullName}]";
            return primaryName;
        }

    }
}

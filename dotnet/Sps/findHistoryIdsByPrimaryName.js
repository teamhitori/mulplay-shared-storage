// findHistoryIdsByPrimaryName
function findDocumentByPrimaryName(primaryName, type, userPrincipleId) {
    var collection = getContext().getCollection();

    console.log("primaryName: " + primaryName + ", ")
    console.log("IN type: " + type + ", ")
    console.log("userPrincipleId: " + userPrincipleId)

    var i = 0;
    var result = __.chain()
        .filter(function (doc) {
            return doc.primaryName.toLowerCase() == primaryName.toLowerCase() &&
                doc.type.toLowerCase() == type.toLowerCase() &&
                doc.userPrincipleId == userPrincipleId;
        })
        .sortByDescending(function (doc) { return doc.version; })
        .map(function (doc) { return { id: doc.id, contents: doc.contents, version: doc.version }; })
        .value();


    if (!result.isAccepted) throw new Error('The query was not accepted by the server.');
}
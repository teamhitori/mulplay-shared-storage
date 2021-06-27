// deleteDocumentById
function findDocumentByPrimaryName(id) {
    var collection = getContext().getCollection();

    console.log("id: " + id)

    var result = __.filter(function (doc) {
        return doc.id == id
    }, function (err, feed, options) {
        if (err) throw err;
        if (!__.deleteDocument(feed[0]._self)) throw new Error("deleteDocument was not accepted");
    });



    if (!result.isAccepted) throw new Error('The query was not accepted by the server.');
}
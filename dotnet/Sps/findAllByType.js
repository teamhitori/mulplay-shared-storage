// findAllByType
function findAllByType(type, userPrincipleId) {
    var collection = getContext().getCollection();
    var response = getContext().getResponse();

    console.log("IN type: " + type + ", ")
    console.log("userPrincipleId: " + userPrincipleId)

    var i = 0;
    var dict = {};

    var result = __.chain()
        .filter(function (doc) {
            return doc.type.toLowerCase() == type.toLowerCase() &&
                doc.userPrincipleId == userPrincipleId;
        })
        .sortBy(function (doc) { return doc.version; })
        .value({ pageSize: -1 }, function (err, feed, options) {

            var res = [];
            console.log("Type: [" + typeof (feed) + "]")
            for (item in feed) {
                console.log(item)
                //console.log(feed[item])
                console.log("[INSERTING: " + feed[item].primaryName + ", VERSION" + feed[item].version + "]      ")
                dict[feed[item].primaryName] = feed[item]

            }

            for (item in dict) {
                res.push(dict[item]);
            }

            response.setBody(res);

            //
        });

    if (!result.isAccepted) throw new Error('The query was not accepted by the server.');
}

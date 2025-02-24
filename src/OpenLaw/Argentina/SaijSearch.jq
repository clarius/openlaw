{
    total: .searchResults.totalSearchResults,
    skip: .queryObjectData.offset,
    take: .queryObjectData.pageSize,
    docs: [.searchResults.documentResultList[] | {
        id: .uuid,
        abstract: .documentAbstract
    }]
}
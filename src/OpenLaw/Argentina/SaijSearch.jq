{
    total: .searchResults.categoriesResultList[].facetChildren.[] | 
            select(.facetName == "total") | .facetHits,
    skip: .queryObjectData.offset,
    take: .queryObjectData.pageSize,
    docs: [.searchResults.documentResultList[] | {
        id: .uuid,
        abstract: .documentAbstract
    }]
}
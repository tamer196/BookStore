using Acme.BookStore.Authors;
using Acme.BookStore.Books;
using Acme.BookStore.Permissions;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

public class BookAppService :
    CrudAppService<
        Book, // The book entity
        BookDto, // Used to show books
        Guid, // Primary key of the book entity
        PagedAndSortedResultRequestDto, // Used for paging/sorting
        CreateUpdateBookDto>, // Used to create/update a book
    IBookAppService // Implement the IBookAppService
{
    private readonly IAuthorRepository _authorRepository;

    public BookAppService(
        IRepository<Book, Guid> repository,
        IAuthorRepository authorRepository)
        : base(repository)
    {
        _authorRepository = authorRepository;
    }

    public override async Task<BookDto> GetAsync(Guid id)
    {
        await CheckGetPolicyAsync();

        // Get the book by its ID
        var book = await Repository.GetAsync(id);

        if (book == null)
        {
            throw new EntityNotFoundException(typeof(Book), id);
        }

        // Get the author by the book's AuthorId
        var author = await _authorRepository.GetAsync(book.AuthorId);

        // Map the book to BookDto
        var bookDto = ObjectMapper.Map<Book, BookDto>(book);

        // Set the author name in the BookDto
        bookDto.AuthorName = author?.Name;

        return bookDto;
    }

    public override async Task<PagedResultDto<BookDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        await CheckGetListPolicyAsync();

        // Get the list of books with paging
        var books = await Repository.GetPagedListAsync(input.SkipCount, input.MaxResultCount, input.Sorting);

        // Get the total count of books
        var totalCount = await Repository.GetCountAsync();

        // Get the list of all authors whose IDs are in the book list
        var authorIds = books.Select(b => b.AuthorId).Distinct().ToList();
        var authors = await _authorRepository.GetListAsync(a => authorIds.Contains(a.Id));

        // Map books to BookDto and join with authors in memory
        var bookDtos = books.Select(book =>
        {
            var author = authors.FirstOrDefault(a => a.Id == book.AuthorId);
            var bookDto = ObjectMapper.Map<Book, BookDto>(book);
            bookDto.AuthorName = author?.Name;
            return bookDto;
        }).ToList();

        return new PagedResultDto<BookDto>(totalCount, bookDtos);
    }

    public async Task<ListResultDto<AuthorLookupDto>> GetAuthorLookupAsync()
    {

        var authors = await _authorRepository.GetListAsync();
        return new ListResultDto<AuthorLookupDto>(
            ObjectMapper.Map<List<Author>, List<AuthorLookupDto>>(authors)
        );


    }

}

using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PracticalWork.Library.Abstractions.Services;
using PracticalWork.Library.Contracts.v1.Books.Request;
using PracticalWork.Library.Contracts.v1.Books.Response;
using PracticalWork.Library.Controllers.Mappers.v1;
using PracticalWork.Library.Models.BaseModels;

namespace PracticalWork.Library.Controllers.Api.v1;

[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/books")]
public class BooksController(IBookService bookService) : Controller
{
    /// <summary> Создание новой книги</summary>
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreateBookResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<Guid> CreateBookAsync(CreateBookRequest request)
    {
        var result = await bookService.CreateBook(request.ToBook());

        return result;
    }

    /// <summary>
    /// Редактирование книги
    /// </summary>
    /// <param name="id">Идентификатор книги</param>
    /// <param name="request">Запрос</param>
    [HttpPut("{id:guid}")]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> UpdateBookAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateBookRequest request)
    {
        await bookService.UpdateBook(request.ToBook(), id);
        
        return Ok();
    }
    
    /// <summary>
    /// Перевод книги в архив
    /// </summary>
    /// <param name="id">Идентификатор книги</param>
    [HttpPost("{id:guid}/archive")]
    [Produces("application/json")]
    [ProducesResponseType( 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ArchiveBookAsync(Guid id)
    {
        await bookService.ArchiveBook(id);

        return Ok();
    }

    /// <summary>
    /// Получение списка книг с филтрами
    /// </summary>
    /// <param name="request"></param>
    /// <returns>Список книг</returns>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<Contracts.v1.Abstracts.PaginationResponseBase<BookResponse>> GetBooksAsync([FromQuery] GetBooksRequest request)
    {
        return (await bookService.GetBooks(request.ToRequestBookModel())).ToResponseBookContract();
    }
    
    /// <summary>
    /// Добавление деталей книги: описание и обложка
    /// </summary>
    [HttpPost("{id:guid}/details")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> AddBookDetailsAsync(
        [FromRoute] Guid id,
        [FromForm] PostBookDetailsRequest request,
        IFormFile coverFile)
    {
        await bookService.UpdateBookDetails(
            id,
            request.Description,
            new UploadedFile(
                coverFile?.FileName,
                coverFile?.ContentType,
                coverFile?.OpenReadStream()
                ));

        return Ok();
    }
}
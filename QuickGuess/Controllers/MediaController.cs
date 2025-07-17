using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickGuess.Data;

namespace QuickGuess.Controllers
{
    [ApiController]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly HttpClient _http;

        public MediaController(ApplicationDbContext db, IHttpClientFactory clientFactory)
        {
            _db = db;
            _http = clientFactory.CreateClient();
        }

        [HttpGet("song-audio/{id:guid}")]
        public async Task<IActionResult> GetSongAudio(Guid id)
        {
            var song = await _db.Songs.FindAsync(id);
            if (song == null) return NotFound();

            var stream = await _http.GetStreamAsync(song.AudioUrl);
            return File(stream, "audio/mpeg");
        }

        [HttpGet("movie-image/{id:guid}")]
        public async Task<IActionResult> GetMovieImage(Guid id)
        {
            var movie = await _db.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            var stream = await _http.GetStreamAsync(movie.ImageUrl);
            return File(stream, "image/jpeg");
        }
    }
}

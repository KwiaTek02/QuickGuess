using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using QuickGuess.Data;

namespace QuickGuess.Controllers
{
    [ApiController]
    [EnableCors]
    [Route("api/media")]
    public class MediaController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public MediaController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("song-audio/{id:guid}")]
        public async Task<IActionResult> GetSongAudio(Guid id)
        {
            var song = await _db.Songs.FindAsync(id);
            if (song == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", song.AudioUrl.TrimStart('/'));

            Console.WriteLine(">>> Szukam pliku audio: " + filePath);
            if (!System.IO.File.Exists(filePath))
            {
                Console.WriteLine(">>> Nie znaleziono pliku: " + filePath);
                return NotFound();
            }

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            Response.Headers["Content-Disposition"] = "inline";


            Response.Headers["Access-Control-Allow-Origin"] = "https://localhost:7003"; 
            Response.Headers["Access-Control-Allow-Credentials"] = "true"; 
            Response.Headers["Access-Control-Allow-Methods"] = "GET";
            Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin";

            return File(stream, "audio/mpeg", enableRangeProcessing: true); 

        }

        [HttpGet("movie-image/{id:guid}")]
        public async Task<IActionResult> GetMovieImage(Guid id)
        {
            var movie = await _db.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", movie.ImageUrl.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return File(stream, "image/jpeg");
        }
    }
}